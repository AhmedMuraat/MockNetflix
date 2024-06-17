FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./Subscribe.csproj", "Subscribe/"]
RUN dotnet restore "./Subscribe/Subscribe.csproj"
WORKDIR /src/Subscribe
COPY . .
RUN dotnet build "./Subscribe.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Subscribe.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Generate and copy the certificate
FROM build AS certs
WORKDIR /certs
RUN dotnet dev-certs https -ep certhttps.pfx -p Password123

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=certs /certs/certhttps.pfx /app/
ENTRYPOINT ["dotnet", "Subscribe.dll"]