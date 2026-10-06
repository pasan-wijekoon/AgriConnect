# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY backend/backend.csproj ./backend/
RUN dotnet restore ./backend/backend.csproj

# Copy the entire backend source code and build
COPY backend/ ./backend/
WORKDIR /src/backend
RUN dotnet publish backend.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Ensure static file uploads folder exists and is writable by non-root user
RUN mkdir -p /app/wwwroot/uploads && chown -R $APP_UID:$APP_UID /app/wwwroot

# Non-root user for security
USER $APP_UID

# Configure ASP.NET Core environment
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5000

# Copy build artifacts
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "backend.dll"]
