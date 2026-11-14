# Build context = repository root (see docker-compose.yml)
# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet restore backend/src/Host/SwiftDrop.Host/SwiftDrop.Host.csproj
RUN dotnet publish backend/src/Host/SwiftDrop.Host/SwiftDrop.Host.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Folder for uploaded delivery-proof images (mounted as a volume)
RUN mkdir -p /app/uploads && chown -R app:app /app/uploads

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "SwiftDrop.Host.dll"]
