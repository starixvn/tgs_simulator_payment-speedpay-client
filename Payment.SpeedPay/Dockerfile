FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Payment.SpeedPay/Payment.SpeedPay.csproj", "Payment.SpeedPay/"]
COPY ["SpeedPay.Core/SpeedPay.Core.csproj", "SpeedPay.Core/"]
COPY ["SpeedPay.Client/SpeedPay.Client.csproj", "SpeedPay.Client/"]
RUN dotnet restore "./Payment.SpeedPay/./Payment.SpeedPay.csproj"
COPY . .
WORKDIR "/src/Payment.SpeedPay"
RUN dotnet build "./Payment.SpeedPay.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Payment.SpeedPay.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.SpeedPay.dll"]