#!/usr/bin/env bash
# Stop on errors, unset vars, or failed piped commands.
set -euo pipefail

# Optional args:
#   1 = deploy directory on the server
#   2 = compose file name/path inside that directory
DEPLOY_PATH="${1:-/var/www/uncannyprompt}"
COMPOSE_FILE="${2:-docker-compose.prod.yml}"

# Run docker compose from the directory that contains .env and the real prod compose file.
cd "$DEPLOY_PATH"

# Difference vs bootstrap.sh:
# this script is for routine application releases only.
# It updates just the published app image and leaves SQL Server or any other
# backing service untouched.
docker compose -f "$COMPOSE_FILE" pull webapp

# Recreate only the application container with the freshly pulled image,
# leaving backing services untouched and removing stale orphaned containers.
# Use bootstrap.sh instead for the first bootstrap or when you intentionally
# want to refresh the whole stack.
docker compose -f "$COMPOSE_FILE" up -d --no-deps --remove-orphans webapp
