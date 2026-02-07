# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY Calendar.Api/Calendar.Api.csproj Calendar.Api/
RUN dotnet restore Calendar.Api/Calendar.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/Calendar.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "Calendar.Api.dll"]
