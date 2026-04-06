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

## Observações

- Identity e Order usam outbox persistido em banco com publicador em background.
- Order chama Identity e Catalog via gRPC com políticas de retry e circuit breaker usando Polly.
- Todos os serviços emitem traces com OpenTelemetry por meio do coletor OTLP.
