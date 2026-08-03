# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Revoa.Api/Revoa.Api.csproj", "Revoa.Api/"]
COPY ["shared/Revoa.Abstractions/Revoa.Abstractions.csproj", "shared/Revoa.Abstractions/"]
COPY ["shared/Revoa.IntegrationContracts/Revoa.IntegrationContracts.csproj", "shared/Revoa.IntegrationContracts/"]
COPY ["shared/Revoa.Infrastructure/Revoa.Infrastructure.csproj", "shared/Revoa.Infrastructure/"]
COPY ["modules/identity/Revoa.Identity.Domain/Revoa.Identity.Domain.csproj", "modules/identity/Revoa.Identity.Domain/"]
COPY ["modules/identity/Revoa.Identity.Application/Revoa.Identity.Application.csproj", "modules/identity/Revoa.Identity.Application/"]
COPY ["modules/identity/Revoa.Identity.Infrastructure/Revoa.Identity.Infrastructure.csproj", "modules/identity/Revoa.Identity.Infrastructure/"]

RUN dotnet restore "Revoa.Api/Revoa.Api.csproj"

COPY . .
WORKDIR /src/Revoa.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8000
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8000
ENTRYPOINT ["dotnet", "Revoa.Api.dll"]
