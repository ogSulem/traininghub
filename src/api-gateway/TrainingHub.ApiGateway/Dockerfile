FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy full source and restore specific project (including its referenced projects)
COPY . .
RUN dotnet restore api-gateway/TrainingHub.ApiGateway/TrainingHub.ApiGateway.csproj
RUN dotnet publish api-gateway/TrainingHub.ApiGateway/TrainingHub.ApiGateway.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=5000
ENTRYPOINT ["dotnet", "TrainingHub.ApiGateway.dll"]
