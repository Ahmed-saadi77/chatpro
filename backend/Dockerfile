# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Build image with SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Add wait script
COPY wait-for-it.sh .
RUN chmod +x wait-for-it.sh

# Use wait-for-it before starting the app
ENTRYPOINT ["./wait-for-it.sh", "postgres", "5432", "--", "dotnet", "chatpro.dll"]
