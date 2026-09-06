# syntax=docker/dockerfile:1

# ── SPA (React + Vite + TS) ──────────────────────────────────────────────
# Build isolado da UI; o dist/ vira o wwwroot da API (única origem, sem CORS).
FROM node:22-alpine AS spa
WORKDIR /ui
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ── API (build) ──────────────────────────────────────────────────────────
# Context = raiz do repo (compose: context "."). Todos os csprojs copiados
# ANTES do fonte para o restore ficar em layer cacheável.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY ["src/modules/account/Revoa.Account.Application/Revoa.Account.Application.csproj", "src/modules/account/Revoa.Account.Application/Revoa.Account.Application.csproj"]
COPY ["src/modules/account/Revoa.Account.Domain/Revoa.Account.Domain.csproj", "src/modules/account/Revoa.Account.Domain/Revoa.Account.Domain.csproj"]
COPY ["src/modules/account/Revoa.Account.Infrastructure/Revoa.Account.Infrastructure.csproj", "src/modules/account/Revoa.Account.Infrastructure/Revoa.Account.Infrastructure.csproj"]
COPY ["src/modules/admin/Revoa.Admin.Application/Revoa.Admin.Application.csproj", "src/modules/admin/Revoa.Admin.Application/Revoa.Admin.Application.csproj"]
COPY ["src/modules/admin/Revoa.Admin.Domain/Revoa.Admin.Domain.csproj", "src/modules/admin/Revoa.Admin.Domain/Revoa.Admin.Domain.csproj"]
COPY ["src/modules/admin/Revoa.Admin.Infrastructure/Revoa.Admin.Infrastructure.csproj", "src/modules/admin/Revoa.Admin.Infrastructure/Revoa.Admin.Infrastructure.csproj"]
COPY ["src/modules/catalog/Revoa.Catalog.Application/Revoa.Catalog.Application.csproj", "src/modules/catalog/Revoa.Catalog.Application/Revoa.Catalog.Application.csproj"]
COPY ["src/modules/catalog/Revoa.Catalog.Domain/Revoa.Catalog.Domain.csproj", "src/modules/catalog/Revoa.Catalog.Domain/Revoa.Catalog.Domain.csproj"]
COPY ["src/modules/catalog/Revoa.Catalog.Infrastructure/Revoa.Catalog.Infrastructure.csproj", "src/modules/catalog/Revoa.Catalog.Infrastructure/Revoa.Catalog.Infrastructure.csproj"]
COPY ["src/modules/community/Revoa.Community.Application/Revoa.Community.Application.csproj", "src/modules/community/Revoa.Community.Application/Revoa.Community.Application.csproj"]
COPY ["src/modules/community/Revoa.Community.Domain/Revoa.Community.Domain.csproj", "src/modules/community/Revoa.Community.Domain/Revoa.Community.Domain.csproj"]
COPY ["src/modules/community/Revoa.Community.Infrastructure/Revoa.Community.Infrastructure.csproj", "src/modules/community/Revoa.Community.Infrastructure/Revoa.Community.Infrastructure.csproj"]
COPY ["src/modules/coupon/Revoa.Coupon.Application/Revoa.Coupon.Application.csproj", "src/modules/coupon/Revoa.Coupon.Application/Revoa.Coupon.Application.csproj"]
COPY ["src/modules/coupon/Revoa.Coupon.Domain/Revoa.Coupon.Domain.csproj", "src/modules/coupon/Revoa.Coupon.Domain/Revoa.Coupon.Domain.csproj"]
COPY ["src/modules/coupon/Revoa.Coupon.Infrastructure/Revoa.Coupon.Infrastructure.csproj", "src/modules/coupon/Revoa.Coupon.Infrastructure/Revoa.Coupon.Infrastructure.csproj"]
COPY ["src/modules/demurrage/Revoa.Demurrage.Application/Revoa.Demurrage.Application.csproj", "src/modules/demurrage/Revoa.Demurrage.Application/Revoa.Demurrage.Application.csproj"]
COPY ["src/modules/demurrage/Revoa.Demurrage.Domain/Revoa.Demurrage.Domain.csproj", "src/modules/demurrage/Revoa.Demurrage.Domain/Revoa.Demurrage.Domain.csproj"]
COPY ["src/modules/demurrage/Revoa.Demurrage.Infrastructure/Revoa.Demurrage.Infrastructure.csproj", "src/modules/demurrage/Revoa.Demurrage.Infrastructure/Revoa.Demurrage.Infrastructure.csproj"]
COPY ["src/modules/exchange/Revoa.Exchange.Application/Revoa.Exchange.Application.csproj", "src/modules/exchange/Revoa.Exchange.Application/Revoa.Exchange.Application.csproj"]
COPY ["src/modules/exchange/Revoa.Exchange.Domain/Revoa.Exchange.Domain.csproj", "src/modules/exchange/Revoa.Exchange.Domain/Revoa.Exchange.Domain.csproj"]
COPY ["src/modules/exchange/Revoa.Exchange.Infrastructure/Revoa.Exchange.Infrastructure.csproj", "src/modules/exchange/Revoa.Exchange.Infrastructure/Revoa.Exchange.Infrastructure.csproj"]
COPY ["src/modules/identity/Revoa.Identity.Application/Revoa.Identity.Application.csproj", "src/modules/identity/Revoa.Identity.Application/Revoa.Identity.Application.csproj"]
COPY ["src/modules/identity/Revoa.Identity.Domain/Revoa.Identity.Domain.csproj", "src/modules/identity/Revoa.Identity.Domain/Revoa.Identity.Domain.csproj"]
COPY ["src/modules/identity/Revoa.Identity.Infrastructure/Revoa.Identity.Infrastructure.csproj", "src/modules/identity/Revoa.Identity.Infrastructure/Revoa.Identity.Infrastructure.csproj"]
COPY ["src/modules/moderation/Revoa.Moderation.Application/Revoa.Moderation.Application.csproj", "src/modules/moderation/Revoa.Moderation.Application/Revoa.Moderation.Application.csproj"]
COPY ["src/modules/moderation/Revoa.Moderation.Domain/Revoa.Moderation.Domain.csproj", "src/modules/moderation/Revoa.Moderation.Domain/Revoa.Moderation.Domain.csproj"]
COPY ["src/modules/moderation/Revoa.Moderation.Infrastructure/Revoa.Moderation.Infrastructure.csproj", "src/modules/moderation/Revoa.Moderation.Infrastructure/Revoa.Moderation.Infrastructure.csproj"]
COPY ["src/modules/notifications/Revoa.Notifications.Application/Revoa.Notifications.Application.csproj", "src/modules/notifications/Revoa.Notifications.Application/Revoa.Notifications.Application.csproj"]
COPY ["src/modules/notifications/Revoa.Notifications.Domain/Revoa.Notifications.Domain.csproj", "src/modules/notifications/Revoa.Notifications.Domain/Revoa.Notifications.Domain.csproj"]
COPY ["src/modules/notifications/Revoa.Notifications.Infrastructure/Revoa.Notifications.Infrastructure.csproj", "src/modules/notifications/Revoa.Notifications.Infrastructure/Revoa.Notifications.Infrastructure.csproj"]
COPY ["src/modules/pricing/Revoa.Pricing.Application/Revoa.Pricing.Application.csproj", "src/modules/pricing/Revoa.Pricing.Application/Revoa.Pricing.Application.csproj"]
COPY ["src/modules/pricing/Revoa.Pricing.Domain/Revoa.Pricing.Domain.csproj", "src/modules/pricing/Revoa.Pricing.Domain/Revoa.Pricing.Domain.csproj"]
COPY ["src/modules/pricing/Revoa.Pricing.Infrastructure/Revoa.Pricing.Infrastructure.csproj", "src/modules/pricing/Revoa.Pricing.Infrastructure/Revoa.Pricing.Infrastructure.csproj"]
COPY ["src/modules/reputation/Revoa.Reputation.Application/Revoa.Reputation.Application.csproj", "src/modules/reputation/Revoa.Reputation.Application/Revoa.Reputation.Application.csproj"]
COPY ["src/modules/reputation/Revoa.Reputation.Domain/Revoa.Reputation.Domain.csproj", "src/modules/reputation/Revoa.Reputation.Domain/Revoa.Reputation.Domain.csproj"]
COPY ["src/modules/reputation/Revoa.Reputation.Infrastructure/Revoa.Reputation.Infrastructure.csproj", "src/modules/reputation/Revoa.Reputation.Infrastructure/Revoa.Reputation.Infrastructure.csproj"]
COPY ["src/modules/token/Revoa.Token.Application/Revoa.Token.Application.csproj", "src/modules/token/Revoa.Token.Application/Revoa.Token.Application.csproj"]
COPY ["src/modules/token/Revoa.Token.Domain/Revoa.Token.Domain.csproj", "src/modules/token/Revoa.Token.Domain/Revoa.Token.Domain.csproj"]
COPY ["src/modules/token/Revoa.Token.Infrastructure/Revoa.Token.Infrastructure.csproj", "src/modules/token/Revoa.Token.Infrastructure/Revoa.Token.Infrastructure.csproj"]
COPY ["src/Revoa.Api/Revoa.Api.csproj", "src/Revoa.Api/Revoa.Api.csproj"]
COPY ["src/shared/Revoa.Abstractions/Revoa.Abstractions.csproj", "src/shared/Revoa.Abstractions/Revoa.Abstractions.csproj"]
COPY ["src/shared/Revoa.Application/Revoa.Application.csproj", "src/shared/Revoa.Application/Revoa.Application.csproj"]
COPY ["src/shared/Revoa.Infrastructure/Revoa.Infrastructure.csproj", "src/shared/Revoa.Infrastructure/Revoa.Infrastructure.csproj"]
COPY ["src/shared/Revoa.IntegrationContracts/Revoa.IntegrationContracts.csproj", "src/shared/Revoa.IntegrationContracts/Revoa.IntegrationContracts.csproj"]
RUN dotnet restore "src/Revoa.Api/Revoa.Api.csproj"

COPY src/ /src/
WORKDIR /src/Revoa.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# SPA → wwwroot do publish (servida pela própria API em Produção)
COPY --from=spa /ui/dist /app/publish/wwwroot

# ── Runtime ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8000
COPY --from=build /app/publish .
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://0.0.0.0:8000
ENTRYPOINT ["dotnet", "Revoa.Api.dll"]
