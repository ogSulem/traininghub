FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy full source and publish the Blazor WebAssembly app
COPY . .
RUN dotnet restore blazor-client/TrainingHub.BlazorClient/TrainingHub.BlazorClient.csproj
RUN dotnet publish blazor-client/TrainingHub.BlazorClient/TrainingHub.BlazorClient.csproj -c Release -o /app/publish

# final image: use nginx to serve the static Blazor WebAssembly files
FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
EXPOSE 80
