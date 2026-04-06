#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

readonly NAMESPACE="marketplace"

require_command() {
  local command_name="$1"

  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "Missing required command: $command_name" >&2
    exit 1
  fi
}

main() {
  require_command sudo

  echo "Deleting application workloads..."
  sudo k3s kubectl delete -f k8s/apps.yaml --ignore-not-found=true

  echo "Deleting shared platform resources..."
  sudo k3s kubectl delete -f k8s/platform.yaml --ignore-not-found=true

  echo "Remaining resources in namespace ${NAMESPACE}:"
  sudo k3s kubectl get all -n "$NAMESPACE" || true
}

main "$@"