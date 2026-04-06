# MyMarketPlace

English | [Português (Brasil)](#portugues-brasil)

## English

Distributed marketplace reference system built on .NET 10 and C# 13 with Clean Architecture, MassTransit, PostgreSQL, Redis, RabbitMQ, gRPC, and OpenTelemetry.

## Services

- ApiGateway: YARP entry point
- Identity: JWT auth, refresh tokens, addresses, outbox, user-created events
- Catalog: PostgreSQL catalog with Redis cache-aside reads and gRPC product lookup
- Cart: Redis-backed whole-cart storage
- Order: PostgreSQL orders, address snapshot, gRPC validation, outbox, order-created events
- Payment: simulated payment approval/failure consumer
- Inventory: PostgreSQL stock reservation consumer
- Notification: one consumer per integration event

## Run Locally

1. `docker compose up -d postgres-identity postgres-catalog postgres-order postgres-inventory redis rabbitmq otel-collector prometheus grafana`
2. `dotnet restore MyMarketPlace.slnx`
3. Run each API project or use the container builds in `docker-compose.yml`

## Run With Kubernetes (k3s)

1. Start your k3s cluster and confirm access:
	- `sudo k3s kubectl get nodes`
2. Build the service images with the names expected by the manifests:
	- `docker build -t mymarketplace/identity-api:latest -f Identity/Identity.API/Dockerfile .`
	- `docker build -t mymarketplace/catalog-api:latest -f Catalog/Catalog.API/Dockerfile .`
	- `docker build -t mymarketplace/cart-api:latest -f Cart/Cart.API/Dockerfile .`
	- `docker build -t mymarketplace/order-api:latest -f Order/Order.API/Dockerfile .`
	- `docker build -t mymarketplace/payment-api:latest -f Payment/Payment.API/Dockerfile .`
	- `docker build -t mymarketplace/inventory-api:latest -f Inventory/Inventory.API/Dockerfile .`
	- `docker build -t mymarketplace/notification-api:latest -f Notification/Notification.API/Dockerfile .`
	- `docker build -t mymarketplace/api-gateway:latest -f ApiGateway/ApiGateway.API/Dockerfile .`
3. Import the images into k3s containerd:
	- `docker save mymarketplace/identity-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/catalog-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/cart-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/order-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/payment-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/inventory-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/notification-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/api-gateway:latest | sudo k3s ctr images import -`
4. Apply the shared platform dependencies:
	- `sudo k3s kubectl apply -f k8s/platform.yaml`
5. Deploy the application workloads:
	- `sudo k3s kubectl apply -f k8s/apps.yaml`
6. Wait until the pods are ready:
	- `sudo k3s kubectl get pods -n marketplace -w`
7. Expose the gateway locally:
	- `sudo k3s kubectl port-forward -n marketplace svc/api-gateway 5000:8080`
8. Access the system through the gateway:
	- `http://localhost:5000`

### Kubernetes Notes

- The manifests create the `marketplace` namespace and the base services declared in `k8s/platform.yaml`.
- The gateway routes requests to `identity-api`, `catalog-api`, `cart-api`, and `order-api` using the config in `k8s/apps.yaml`.
- The current manifests use ClusterIP services, so local access is done with `port-forward`.
- HPAs in `k8s/apps.yaml` require `metrics-server` in the cluster.
- The OpenTelemetry Collector is deployed by `k8s/platform.yaml` as `otel-collector` and listens on `4317` for OTLP gRPC.
- The collector also exposes Prometheus-compatible metrics on port `8889` inside the cluster.

## Notes

- Identity and Order use a database-backed outbox plus background publisher.
- Order calls Identity and Catalog over gRPC with Polly retry and circuit-breaker handlers.
- All services emit OpenTelemetry traces through the OTLP collector.

---

## Portugues (Brasil)

Sistema de marketplace distribuído de referência construído com .NET 10 e C# 13, usando Clean Architecture, MassTransit, PostgreSQL, Redis, RabbitMQ, gRPC e OpenTelemetry.

## Serviços

- ApiGateway: ponto de entrada com YARP
- Identity: autenticação JWT, refresh tokens, endereços, outbox e eventos de criação de usuário
- Catalog: catálogo em PostgreSQL com leitura em cache via Redis no padrão cache-aside e consulta de produtos por gRPC
- Cart: armazenamento completo do carrinho em Redis
- Order: pedidos em PostgreSQL, snapshot de endereço, validação via gRPC, outbox e eventos de criação de pedido
- Payment: consumidor com simulação de aprovação ou falha de pagamento
- Inventory: consumidor para reserva de estoque em PostgreSQL
- Notification: um consumidor por evento de integração

## Execução local

1. `docker compose up -d postgres-identity postgres-catalog postgres-order postgres-inventory redis rabbitmq otel-collector prometheus grafana`
2. `dotnet restore MyMarketPlace.slnx`
3. Execute cada projeto API individualmente ou use os builds de contêiner definidos em `docker-compose.yml`

## Execução com Kubernetes (k3s)

1. Inicie o cluster k3s e confirme o acesso:
	- `sudo k3s kubectl get nodes`
2. Gere as imagens com os nomes esperados pelos manifests:
	- `docker build -t mymarketplace/identity-api:latest -f Identity/Identity.API/Dockerfile .`
	- `docker build -t mymarketplace/catalog-api:latest -f Catalog/Catalog.API/Dockerfile .`
	- `docker build -t mymarketplace/cart-api:latest -f Cart/Cart.API/Dockerfile .`
	- `docker build -t mymarketplace/order-api:latest -f Order/Order.API/Dockerfile .`
	- `docker build -t mymarketplace/payment-api:latest -f Payment/Payment.API/Dockerfile .`
	- `docker build -t mymarketplace/inventory-api:latest -f Inventory/Inventory.API/Dockerfile .`
	- `docker build -t mymarketplace/notification-api:latest -f Notification/Notification.API/Dockerfile .`
	- `docker build -t mymarketplace/api-gateway:latest -f ApiGateway/ApiGateway.API/Dockerfile .`
3. Importe as imagens para o containerd do k3s:
	- `docker save mymarketplace/identity-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/catalog-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/cart-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/order-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/payment-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/inventory-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/notification-api:latest | sudo k3s ctr images import -`
	- `docker save mymarketplace/api-gateway:latest | sudo k3s ctr images import -`
4. Aplique as dependências compartilhadas da plataforma:
	- `sudo k3s kubectl apply -f k8s/platform.yaml`
5. Faça o deploy das aplicações:
	- `sudo k3s kubectl apply -f k8s/apps.yaml`
6. Aguarde os pods ficarem prontos:
	- `sudo k3s kubectl get pods -n marketplace -w`
7. Exponha o gateway localmente:
	- `sudo k3s kubectl port-forward -n marketplace svc/api-gateway 5000:8080`
8. Acesse o sistema pelo gateway:
	- `http://localhost:5000`

### Observações sobre Kubernetes

- Os manifests criam o namespace `marketplace` e os serviços-base definidos em `k8s/platform.yaml`.
- O gateway encaminha requisições para `identity-api`, `catalog-api`, `cart-api` e `order-api` com a configuração de `k8s/apps.yaml`.
- Os manifests atuais usam serviços ClusterIP, então o acesso local é feito com `port-forward`.
- Os HPAs definidos em `k8s/apps.yaml` dependem de `metrics-server` no cluster.
- O OpenTelemetry Collector é publicado por `k8s/platform.yaml` como `otel-collector` e escuta OTLP gRPC na porta `4317`.
- O collector também expõe métricas compatíveis com Prometheus na porta `8889` dentro do cluster.

## Observações

- Identity e Order usam outbox persistido em banco com publicador em background.
- Order chama Identity e Catalog via gRPC com políticas de retry e circuit breaker usando Polly.
- Todos os serviços emitem traces com OpenTelemetry por meio do coletor OTLP.
