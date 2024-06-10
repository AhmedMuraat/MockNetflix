# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Subscription/Subscription.csproj", "Subscription/"]
RUN dotnet restore "Subscription/Subscription.csproj"
WORKDIR /src/Subscription
COPY . .
RUN dotnet build "Subscription.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Subscription.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Generate and copy the certificate
FROM build AS certs
WORKDIR /certs
RUN dotnet dev-certs https -ep certhttps.pfx -p Password123

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=certs /certs/certhttps.pfx /app/
ENTRYPOINT ["dotnet", "Subscription.dll"]
