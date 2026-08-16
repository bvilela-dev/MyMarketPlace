#!/usr/bin/env bash
# =============================================================================
# Derruba o MyMarketPlace do cluster k3s.
#
#   ./stop.sh              remove os workloads, PRESERVA os dados
#   ./stop.sh --purgar     remove tambem os volumes (bancos zerados)
#
# Por padrao os PersistentVolumeClaims sao mantidos: apagar `kind: StatefulSet`
# NAO apaga o volume, justamente para nao perder dados por engano. Zerar tudo
# precisa ser um pedido explicito.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

readonly NAMESPACE="marketplace"
PURGAR=false

for argumento in "$@"; do
  case "$argumento" in
    --purgar|--purge) PURGAR=true ;;
    *) printf 'Argumento desconhecido: %s\n' "$argumento" >&2; exit 1 ;;
  esac
done

info() { printf '\033[1;34m==>\033[0m %s\n' "$1"; }

main() {
  if ! command -v sudo >/dev/null 2>&1; then
    printf 'ERRO: comando obrigatorio nao encontrado: sudo\n' >&2
    exit 1
  fi

  info "Removendo os microsservicos..."
  sudo k3s kubectl delete -f k8s/apps.yaml --ignore-not-found=true

  info "Removendo a plataforma..."
  sudo k3s kubectl delete -f k8s/platform.yaml --ignore-not-found=true

  if [ "$PURGAR" = true ]; then
    info "Removendo os volumes persistentes (dados serao perdidos)..."
    sudo k3s kubectl delete pvc --all -n "$NAMESPACE" --ignore-not-found=true
  else
    info "Volumes preservados. Use './stop.sh --purgar' para apagar os dados."
  fi

  info "Recursos restantes no namespace ${NAMESPACE}:"
  sudo k3s kubectl get all -n "$NAMESPACE" 2>/dev/null || true
}

main "$@"
