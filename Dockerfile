# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY CatStealer.sln ./

# Copy project files
COPY CatStealer.Domain/CatStealer.Domain.csproj CatStealer.Domain/
COPY CatStealer.Application/CatStealer.Application.csproj CatStealer.Application/
COPY CatStealer.Api/CatStealer.Api.csproj CatStealer.Api/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR /src/CatStealer.Api
RUN dotnet build -c Debug -o /app/build

# Publish the application
RUN dotnet publish -c Debug -o /app/publish

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create directory for cat images
RUN mkdir -p /app/CatImages

# Copy the published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "CatStealer.Api.dll"]