# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY QuickBooksAPI.sln ./
COPY QuickBooksAPI/QuickBooksAPI.csproj QuickBooksAPI/
COPY QuickBooksService/QuickBooksService.csproj QuickBooksService/

# Restore (API pulls in QuickBooksService)
RUN dotnet restore QuickBooksAPI/QuickBooksAPI.csproj

# Copy source
COPY QuickBooksAPI/ QuickBooksAPI/
COPY QuickBooksService/ QuickBooksService/

# Build release
RUN dotnet build QuickBooksAPI/QuickBooksAPI.csproj -c Release --no-restore -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish QuickBooksAPI/QuickBooksAPI.csproj -c Release --no-build -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user (optional, good for Coolify)
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

# Listen on port 8080 (Coolify/reverse proxy will map this)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "QuickBooksAPI.dll"]
