#!/usr/bin/env bash
# =============================================================================
# Sobe o MyMarketPlace num cluster k3s local.
#
#   ./start.sh
#
# O que o script faz:
#   1. verifica pre-requisitos e acesso ao cluster;
#   2. gera as oito imagens Docker;
#   3. importa as imagens para o containerd do k3s (nao ha registry);
#   4. aplica os manifests de plataforma e de aplicacao;
#   5. espera tudo ficar pronto;
#   6. expoe o gateway em http://localhost:5000.
#
# Para o ambiente em Docker Compose (mais rapido para demonstrar), use:
#   docker compose up -d --build
# =============================================================================

# -e: aborta no primeiro erro | -u: variavel nao definida e erro
# -o pipefail: um erro no meio de um pipe nao passa despercebido
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

readonly NAMESPACE="marketplace"
readonly PORTA_LOCAL="5000"
readonly PORTA_REMOTA="8080"

readonly IMAGENS=(
  "mymarketplace/identity-api:latest|Identity/Identity.API/Dockerfile"
  "mymarketplace/catalog-api:latest|Catalog/Catalog.API/Dockerfile"
  "mymarketplace/cart-api:latest|Cart/Cart.API/Dockerfile"
  "mymarketplace/order-api:latest|Order/Order.API/Dockerfile"
  "mymarketplace/payment-api:latest|Payment/Payment.API/Dockerfile"
  "mymarketplace/inventory-api:latest|Inventory/Inventory.API/Dockerfile"
  "mymarketplace/notification-api:latest|Notification/Notification.API/Dockerfile"
  "mymarketplace/api-gateway:latest|ApiGateway/ApiGateway.API/Dockerfile"
)

info() { printf '\033[1;34m==>\033[0m %s\n' "$1"; }
erro() { printf '\033[1;31mERRO:\033[0m %s\n' "$1" >&2; }

exigir_comando() {
  if ! command -v "$1" >/dev/null 2>&1; then
    erro "comando obrigatorio nao encontrado: $1"
    exit 1
  fi
}

iniciar_k3s_se_possivel() {
  if command -v systemctl >/dev/null 2>&1 && systemctl list-unit-files | grep -q '^k3s\.service'; then
    info "Garantindo que o servico k3s esta ativo..."
    sudo systemctl start k3s
  fi
}

gerar_imagens() {
  info "Gerando as imagens (build paralelo pode consumir bastante CPU)..."

  local entrada imagem dockerfile
  for entrada in "${IMAGENS[@]}"; do
    imagem="${entrada%%|*}"
    dockerfile="${entrada##*|}"
    printf '    -> %s\n' "$imagem"
    docker build --quiet -t "$imagem" -f "$dockerfile" . >/dev/null
  done
}

importar_imagens() {
  # O k3s usa containerd proprio e nao enxerga as imagens do Docker local.
  # Sem um registry, o caminho e exportar e importar manualmente — motivo pelo
  # qual os manifests usam imagePullPolicy: IfNotPresent.
  info "Importando as imagens para o containerd do k3s..."

  local entrada imagem
  for entrada in "${IMAGENS[@]}"; do
    imagem="${entrada%%|*}"
    printf '    -> %s\n' "$imagem"
    docker save "$imagem" | sudo k3s ctr images import - >/dev/null
  done
}

aplicar_manifests() {
  info "Aplicando a plataforma (bancos, RabbitMQ, Redis, observabilidade)..."
  sudo k3s kubectl apply -f k8s/platform.yaml

  info "Aguardando as dependencias ficarem prontas..."
  sudo k3s kubectl wait --for=condition=Ready pod -l tier=database   -n "$NAMESPACE" --timeout=300s || true
  sudo k3s kubectl wait --for=condition=Ready pod -l tier=messaging  -n "$NAMESPACE" --timeout=300s || true
  sudo k3s kubectl wait --for=condition=Ready pod -l tier=cache      -n "$NAMESPACE" --timeout=300s || true

  info "Aplicando os microsservicos..."
  sudo k3s kubectl apply -f k8s/apps.yaml
}

aguardar_aplicacao() {
  info "Aguardando os deployments ficarem disponiveis..."
  sudo k3s kubectl wait --for=condition=Available deployment --all -n "$NAMESPACE" --timeout=600s

  info "Situacao atual dos pods:"
  sudo k3s kubectl get pods -n "$NAMESPACE"
}

expor_gateway() {
  cat <<TEXTO

$(info "Ambiente pronto.")

    Gateway .......... http://localhost:${PORTA_LOCAL}
    Catalogo ......... http://localhost:${PORTA_LOCAL}/catalog/api/products
    Cadastro ......... POST http://localhost:${PORTA_LOCAL}/identity/api/auth/register

    Painel do RabbitMQ e Grafana exigem port-forward proprio:
      sudo k3s kubectl port-forward -n ${NAMESPACE} svc/rabbitmq 15672:15672
      sudo k3s kubectl port-forward -n ${NAMESPACE} svc/grafana 3000:3000

    Ctrl+C encerra o redirecionamento de porta (os pods continuam de pe).

TEXTO
  exec sudo k3s kubectl port-forward -n "$NAMESPACE" svc/api-gateway "${PORTA_LOCAL}:${PORTA_REMOTA}"
}

main() {
  exigir_comando docker
  exigir_comando sudo

  iniciar_k3s_se_possivel

  info "Verificando acesso ao cluster..."
  sudo k3s kubectl get nodes

  gerar_imagens
  importar_imagens
  aplicar_manifests
  aguardar_aplicacao
  expor_gateway
}

main "$@"
