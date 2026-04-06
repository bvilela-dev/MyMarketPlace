# MyMarketPlace

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
