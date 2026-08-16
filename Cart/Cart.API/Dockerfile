# =============================================================================
# Cart.API — build multi-estagio.
#
# Por que multi-estagio: a imagem do SDK do .NET tem ~800 MB (compilador, NuGet,
# ferramentas); a de runtime, ~200 MB. Compilar num estagio e copiar apenas o
# resultado para o outro reduz a imagem final e, principalmente, tira o
# compilador e o codigo-fonte da imagem que vai para producao.
#
# O contexto de build e a RAIZ do repositorio (nao a pasta do servico), porque
# este projeto referencia os building blocks compartilhados:
#   docker build -f Cart/Cart.API/Dockerfile .
# =============================================================================

# ------------------------------- Estagio 1: restore + build ------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# ---------------------------------------------------------------------------
# Copia PRIMEIRO apenas os arquivos de projeto e roda o restore.
#
# Isso existe por causa do cache de camadas do Docker: dependencias NuGet mudam
# raramente, codigo muda a cada commit. Com os passos separados, alterar um .cs
# reaproveita a camada de restore em vez de baixar tudo de novo — a diferenca
# entre um build de segundos e um de minutos.
#
# Os COPY sao explicitos (e nao um curinga esperto) porque a estrutura de pastas
# nao segue um padrao unico: os building blocks ficam em BuildingBlocks/, os
# servicos em pastas com o proprio nome. Verboso, porem correto e obvio de ler.
# ---------------------------------------------------------------------------
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["ApiGateway/ApiGateway.API/ApiGateway.API.csproj", "ApiGateway/ApiGateway.API/"]
COPY ["BuildingBlocks/Marketplace.Application/Marketplace.Application.csproj", "BuildingBlocks/Marketplace.Application/"]
COPY ["BuildingBlocks/Marketplace.Contracts/Marketplace.Contracts.csproj", "BuildingBlocks/Marketplace.Contracts/"]
COPY ["BuildingBlocks/Marketplace.Infrastructure/Marketplace.Infrastructure.csproj", "BuildingBlocks/Marketplace.Infrastructure/"]
COPY ["BuildingBlocks/Marketplace.SharedKernel/Marketplace.SharedKernel.csproj", "BuildingBlocks/Marketplace.SharedKernel/"]
COPY ["Cart/Cart.API/Cart.API.csproj", "Cart/Cart.API/"]
COPY ["Cart/Cart.Application/Cart.Application.csproj", "Cart/Cart.Application/"]
COPY ["Cart/Cart.Domain/Cart.Domain.csproj", "Cart/Cart.Domain/"]
COPY ["Cart/Cart.Infrastructure/Cart.Infrastructure.csproj", "Cart/Cart.Infrastructure/"]
COPY ["Catalog/Catalog.API/Catalog.API.csproj", "Catalog/Catalog.API/"]
COPY ["Catalog/Catalog.Application/Catalog.Application.csproj", "Catalog/Catalog.Application/"]
COPY ["Catalog/Catalog.Domain/Catalog.Domain.csproj", "Catalog/Catalog.Domain/"]
COPY ["Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj", "Catalog/Catalog.Infrastructure/"]
COPY ["Identity/Identity.API/Identity.API.csproj", "Identity/Identity.API/"]
COPY ["Identity/Identity.Application/Identity.Application.csproj", "Identity/Identity.Application/"]
COPY ["Identity/Identity.Domain/Identity.Domain.csproj", "Identity/Identity.Domain/"]
COPY ["Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "Identity/Identity.Infrastructure/"]
COPY ["Inventory/Inventory.API/Inventory.API.csproj", "Inventory/Inventory.API/"]
COPY ["Inventory/Inventory.Application/Inventory.Application.csproj", "Inventory/Inventory.Application/"]
COPY ["Inventory/Inventory.Domain/Inventory.Domain.csproj", "Inventory/Inventory.Domain/"]
COPY ["Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "Inventory/Inventory.Infrastructure/"]
COPY ["Notification/Notification.API/Notification.API.csproj", "Notification/Notification.API/"]
COPY ["Notification/Notification.Application/Notification.Application.csproj", "Notification/Notification.Application/"]
COPY ["Notification/Notification.Infrastructure/Notification.Infrastructure.csproj", "Notification/Notification.Infrastructure/"]
COPY ["Order/Order.API/Order.API.csproj", "Order/Order.API/"]
COPY ["Order/Order.Application/Order.Application.csproj", "Order/Order.Application/"]
COPY ["Order/Order.Domain/Order.Domain.csproj", "Order/Order.Domain/"]
COPY ["Order/Order.Infrastructure/Order.Infrastructure.csproj", "Order/Order.Infrastructure/"]
COPY ["Payment/Payment.API/Payment.API.csproj", "Payment/Payment.API/"]
COPY ["Payment/Payment.Application/Payment.Application.csproj", "Payment/Payment.Application/"]
COPY ["Payment/Payment.Infrastructure/Payment.Infrastructure.csproj", "Payment/Payment.Infrastructure/"]

RUN dotnet restore "Cart/Cart.API/Cart.API.csproj"

# Agora sim o codigo-fonte completo.
COPY . .

RUN dotnet publish "Cart/Cart.API/Cart.API.csproj" \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false

# ------------------------------- Estagio 2: runtime --------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Rodar sem privilegios: se a aplicacao for comprometida, o atacante nao ganha
# root dentro do container. A imagem oficial ja traz o usuario "app" pronto.
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Cart.API.dll"]
