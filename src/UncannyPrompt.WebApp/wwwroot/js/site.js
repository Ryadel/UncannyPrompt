document.addEventListener("alpine:init", () => {
  const stored = localStorage.getItem("uncanny:sidebar:collapsed");
  Alpine.store("shell", {
    collapsed: stored === "true",
    toggle() {
      this.collapsed = !this.collapsed;
      localStorage.setItem("uncanny:sidebar:collapsed", String(this.collapsed));
    }
  });
});

document.addEventListener("keydown", (event) => {
  if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
    const search = document.querySelector("[data-global-search]");
    if (search && !search.disabled) {
      event.preventDefault();
      search.focus();
      search.select();
    }
  }
});

document.addEventListener("DOMContentLoaded", () => {
  initializeScopeSwitchers();
});

window.promptBoard = function promptBoard(defaultProjectId) {
  return {
    loading: false,
    showVariables: false,
    prompts: [],
    selected: null,
    versions: [],
    placeholders: [],
    values: {},
    missing: [],
    copyMessage: "",
    filters: {
      query: "",
      projectId: defaultProjectId,
      folderId: "",
      tag: "",
      favoritesOnly: false,
      sort: "updated"
    },
    async init() {
      await this.loadPrompts();
    },
    async selectProject(projectId) {
      this.filters.projectId = projectId;
      this.filters.folderId = "";
      this.selected = null;
      await this.loadPrompts();
    },
    async loadPrompts() {
      this.loading = true;
      const params = new URLSearchParams();
      for (const [key, value] of Object.entries(this.filters)) {
        if (value !== "" && value !== false && value !== null) params.set(key, value);
      }
      const response = await fetch(`/api/prompts?${params.toString()}`);
      this.prompts = response.ok ? await response.json() : [];
      if (this.selected) {
        this.selected = this.prompts.find((prompt) => prompt.id === this.selected.id) || null;
      }
      this.loading = false;
    },
    selectPrompt(prompt) {
      this.selected = prompt;
      this.versions = [];
      this.copyMessage = "";
      this.missing = [];
      this.placeholders = [...new Set([...prompt.content.matchAll(/\{\{\s*([a-zA-Z][a-zA-Z0-9_]*)\s*\}\}/g)].map((match) => match[1]))].sort();
      this.values = Object.fromEntries(this.placeholders.map((name) => [name, this.values[name] || ""]));
    },
    async toggleFavorite(prompt) {
      const response = await fetch(`/api/prompts/${prompt.id}/favorite`, jsonRequest("POST", {}));
      if (response.ok) {
        Object.assign(prompt, await response.json());
      }
    },
    async togglePinned(prompt) {
      const response = await fetch(`/api/prompts/${prompt.id}/pinned`, jsonRequest("POST", {}));
      if (response.ok) {
        Object.assign(prompt, await response.json());
      }
    },
    async copyRaw() {
      if (!this.selected) return;
      await navigator.clipboard.writeText(this.selected.content);
      await fetch(`/api/prompts/${this.selected.id}/copy-log`, jsonRequest("POST", { resolved: false }));
      this.copyMessage = "Copied raw prompt.";
    },
    async copyResolved() {
      if (!this.selected) return;
      const response = await fetch(`/api/prompts/${this.selected.id}/resolve`, jsonRequest("POST", { values: this.values }));
      if (!response.ok) return;
      const result = await response.json();
      this.missing = result.missingVariables || [];
      await navigator.clipboard.writeText(result.resolvedContent);
      await fetch(`/api/prompts/${this.selected.id}/copy-log`, jsonRequest("POST", { resolved: true }));
      this.copyMessage = this.missing.length ? "Copied with placeholders still open." : "Copied resolved prompt.";
    },
    async loadVersions() {
      if (!this.selected) return;
      const response = await fetch(`/api/prompts/${this.selected.id}/versions`);
      this.versions = response.ok ? await response.json() : [];
    },
    async restoreVersion(version) {
      if (!this.selected) return;
      const response = await fetch(`/api/prompts/${this.selected.id}/restore`, jsonRequest("POST", {
        versionId: version.id,
        changelog: `Restored version ${version.versionNumber}`
      }));
      if (response.ok) {
        const restored = await response.json();
        await this.loadPrompts();
        this.selectPrompt(restored);
        await this.loadVersions();
      }
    }
  };
};

function jsonRequest(method, body) {
  return {
    method,
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": document.querySelector("meta[name='request-verification-token']")?.content || ""
    },
    body: JSON.stringify(body)
  };
}

function initializeScopeSwitchers() {
  for (const root of document.querySelectorAll("[data-scope-switcher]")) {
    if (root.dataset.scopeSwitcherBound === "1") {
      continue;
    }
    root.dataset.scopeSwitcherBound = "1";

    root.addEventListener("click", async (event) => {
      const option = event.target.closest("[data-scope-option]");
      if (!option || !root.contains(option)) {
        return;
      }

      event.preventDefault();

      const type = option.dataset.scopeType;
      const id = option.dataset.scopeId;
      if (!type || !id) {
        return;
      }

      const currentTenant = root.dataset.currentTenant || null;
      const currentWorkspace = root.dataset.currentWorkspace || null;

      let payload;
      if (type === "tenant") {
        if (id === currentTenant) {
          return;
        }
        payload = { tenantId: id, workspaceId: null, projectId: null };
      } else if (type === "workspace") {
        if (id === root.dataset.currentWorkspace) {
          return;
        }
        payload = { tenantId: currentTenant, workspaceId: id, projectId: null };
      } else if (type === "project") {
        if (id === root.dataset.currentProject) {
          return;
        }
        payload = { tenantId: currentTenant, workspaceId: currentWorkspace, projectId: id };
      } else {
        return;
      }

      option.setAttribute("aria-busy", "true");

      const response = await fetch("/api/scope", jsonRequest("POST", payload));
      if (!response.ok) {
        option.removeAttribute("aria-busy");
        return;
      }

      window.location.assign(root.dataset.reloadUrl || window.location.href);
    });
  }
}
