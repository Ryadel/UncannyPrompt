using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Domain;

namespace UncannyPrompt.Infrastructure;

public sealed class UncannyPromptDbContext(DbContextOptions<UncannyPromptDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AuthIdentity> AuthIdentities => Set<AuthIdentity>();
    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Prompt> Prompts => Set<Prompt>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PromptTag> PromptTags => Set<PromptTag>();
    public DbSet<VersionLabel> VersionLabels => Set<VersionLabel>();
    public DbSet<PromptNote> PromptNotes => Set<PromptNote>();
    public DbSet<VariableDefinition> VariableDefinitions => Set<VariableDefinition>();
    public DbSet<VariableValue> VariableValues => Set<VariableValue>();
    public DbSet<ShareGrant> ShareGrants => Set<ShareGrant>();
    public DbSet<PublicShareLink> PublicShareLinks => Set<PublicShareLink>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Role).HasDefaultValue(UserRole.Standard).HasSentinel(UserRole.Standard);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<AuthIdentity>(entity =>
        {
            entity.Property(x => x.Provider).HasMaxLength(64);
            entity.Property(x => x.ProviderSubjectId).HasMaxLength(256);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.HasIndex(x => new { x.Provider, x.ProviderSubjectId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.AuthIdentities).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserApiKey>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Prefix).HasMaxLength(24);
            entity.Property(x => x.KeyHash).HasMaxLength(512);
            entity.HasIndex(x => x.Prefix);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.HasIndex(x => new { x.OwnerUserId, x.Kind });
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.GrantedByUserId });
            entity.HasOne(x => x.Tenant).WithMany(x => x.Memberships).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.TenantMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.GrantedByUser).WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.TenantId);
            entity.HasOne(x => x.Tenant).WithMany(x => x.Workspaces).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.WorkspaceId);
            entity.HasOne(x => x.Workspace).WithMany(x => x.Projects).HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.HasIndex(x => new { x.ProjectId, x.ParentFolderId });
            entity.HasOne(x => x.Project).WithMany(x => x.Folders).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ParentFolder).WithMany(x => x.Children).HasForeignKey(x => x.ParentFolderId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Prompt>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(220);
            entity.Property(x => x.Summary).HasMaxLength(500);
            entity.Property(x => x.Content).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.Ignore(x => x.ShareGrants);
            entity.HasIndex(x => new { x.ProjectId, x.FolderId, x.Status, x.AuthorUserId });
            entity.HasIndex(x => x.UpdatedAt);
            entity.HasOne(x => x.Project).WithMany(x => x.Prompts).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Folder).WithMany(x => x.Prompts).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PromptVersion>(entity =>
        {
            entity.Property(x => x.Content).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Label).HasMaxLength(120);
            entity.Property(x => x.Changelog).HasMaxLength(2000);
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.PromptId, x.VersionNumber }).IsUnique();
            entity.HasOne(x => x.Prompt).WithMany(x => x.Versions).HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.HasIndex(x => new { x.Scope, x.Name, x.UserId, x.TenantId, x.WorkspaceId }).IsUnique();
        });

        modelBuilder.Entity<PromptTag>(entity =>
        {
            entity.HasKey(x => new { x.PromptId, x.TagId });
            entity.HasOne(x => x.Prompt).WithMany(x => x.PromptTags).HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Tag).WithMany(x => x.PromptTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VersionLabel>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.HasIndex(x => new { x.PromptVersionId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<PromptNote>(entity =>
        {
            entity.Property(x => x.Body).HasColumnType("nvarchar(max)");
            entity.HasOne(x => x.Prompt).WithMany(x => x.PromptNotes).HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<VariableDefinition>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DefaultValue).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.Scope, x.Name, x.UserId, x.ProjectId });
        });

        modelBuilder.Entity<VariableValue>(entity =>
        {
            entity.Property(x => x.Value).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.VariableDefinitionId, x.UserId, x.ProjectId, x.PromptId });
        });

        modelBuilder.Entity<ShareGrant>(entity =>
        {
            entity.HasIndex(x => new { x.TargetType, x.TargetId, x.UserId, x.TenantId, x.WorkspaceId });
        });

        modelBuilder.Entity<PublicShareLink>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(512);
            entity.Property(x => x.TokenLookupHash).HasMaxLength(512);
            entity.HasIndex(x => x.PromptId);
            entity.HasIndex(x => x.TokenLookupHash);
            entity.HasOne(x => x.Prompt).WithMany(x => x.PublicShareLinks).HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(x => x.EventType).HasMaxLength(120);
            entity.Property(x => x.EntityType).HasMaxLength(120);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.Property(x => x.DataJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
            entity.HasIndex(x => new { x.ActorUserId, x.CreatedAt });
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.PromptId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.Favorites).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Prompt).WithMany(x => x.Favorites).HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.Property(x => x.Key).HasMaxLength(120);
            entity.Property(x => x.Value).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.Scope, x.Key }).IsUnique().HasFilter("[TenantId] IS NULL");
            entity.HasIndex(x => new { x.Scope, x.TenantId, x.Key }).IsUnique().HasFilter("[TenantId] IS NOT NULL");
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt ??= now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
