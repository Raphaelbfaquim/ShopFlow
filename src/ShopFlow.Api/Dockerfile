FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY ShopFlow.sln ./
COPY src/ShopFlow.BuildingBlocks/ShopFlow.BuildingBlocks.csproj src/ShopFlow.BuildingBlocks/
COPY src/ShopFlow.Domain/ShopFlow.Domain.csproj src/ShopFlow.Domain/
COPY src/ShopFlow.Application/ShopFlow.Application.csproj src/ShopFlow.Application/
COPY src/ShopFlow.Infrastructure/ShopFlow.Infrastructure.csproj src/ShopFlow.Infrastructure/
COPY src/ShopFlow.Api/ShopFlow.Api.csproj src/ShopFlow.Api/
RUN dotnet restore src/ShopFlow.Api/ShopFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/ShopFlow.Api/ShopFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ShopFlow.Api.dll"]
