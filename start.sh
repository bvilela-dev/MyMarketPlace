#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

readonly NAMESPACE="marketplace"
readonly PORT_FORWARD_LOCAL_PORT="5000"
readonly PORT_FORWARD_REMOTE_PORT="8080"

images=(
  "mymarketplace/identity-api:latest|Identity/Identity.API/Dockerfile"
  "mymarketplace/catalog-api:latest|Catalog/Catalog.API/Dockerfile"
  "mymarketplace/cart-api:latest|Cart/Cart.API/Dockerfile"
  "mymarketplace/order-api:latest|Order/Order.API/Dockerfile"
  "mymarketplace/payment-api:latest|Payment/Payment.API/Dockerfile"
  "mymarketplace/inventory-api:latest|Inventory/Inventory.API/Dockerfile"
  "mymarketplace/notification-api:latest|Notification/Notification.API/Dockerfile"
  "mymarketplace/api-gateway:latest|ApiGateway/ApiGateway.API/Dockerfile"
)

require_command() {
  local command_name="$1"

  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "Missing required command: $command_name" >&2
    exit 1
  fi
}

start_k3s_if_possible() {
  if command -v systemctl >/dev/null 2>&1 && systemctl list-unit-files | grep -q '^k3s\.service'; then
    echo "Starting k3s service if needed..."
    sudo systemctl start k3s
  fi
}

build_images() {
  echo "Building container images..."

  local entry image dockerfile
  for entry in "${images[@]}"; do
    image="${entry%%|*}"
    dockerfile="${entry##*|}"

    echo "  -> $image"
    docker build -t "$image" -f "$dockerfile" .
  done
}

import_images_into_k3s() {
  echo "Importing images into k3s containerd..."

  local entry image
  for entry in "${images[@]}"; do
    image="${entry%%|*}"

    echo "  -> $image"
    docker save "$image" | sudo k3s ctr images import -
  done
}

deploy_manifests() {
  echo "Applying Kubernetes manifests..."
  sudo k3s kubectl apply -f k8s/platform.yaml
  sudo k3s kubectl apply -f k8s/apps.yaml
}

wait_for_workloads() {
  echo "Waiting for deployments to become available..."
  sudo k3s kubectl wait --for=condition=Available deployment --all -n "$NAMESPACE" --timeout=600s

  echo "Current pod status:"
  sudo k3s kubectl get pods -n "$NAMESPACE"
}

port_forward_gateway() {
  echo "Gateway will be available at http://localhost:${PORT_FORWARD_LOCAL_PORT}"
  echo "Press Ctrl+C to stop port-forwarding."
  exec sudo k3s kubectl port-forward -n "$NAMESPACE" svc/api-gateway "${PORT_FORWARD_LOCAL_PORT}:${PORT_FORWARD_REMOTE_PORT}"
}

main() {
  require_command docker
  require_command sudo

  start_k3s_if_possible

  echo "Checking k3s cluster access..."
  sudo k3s kubectl get nodes

  build_images
  import_images_into_k3s
  deploy_manifests
  wait_for_workloads
  port_forward_gateway
}

main "$@"