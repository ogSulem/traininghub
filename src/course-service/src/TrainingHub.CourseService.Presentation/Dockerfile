FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy full source and restore specific project (including its referenced projects)
COPY . .
RUN dotnet restore course-service/src/TrainingHub.CourseService.Presentation/TrainingHub.CourseService.Presentation.csproj
RUN dotnet publish course-service/src/TrainingHub.CourseService.Presentation/TrainingHub.CourseService.Presentation.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "TrainingHub.CourseService.Presentation.dll"]
