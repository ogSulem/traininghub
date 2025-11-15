FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy full source and restore the worker project (with referenced projects)
COPY . .
RUN dotnet restore course-service/src/TrainingHub.CourseService.Worker.ML/TrainingHub.CourseService.Worker.ML.csproj
RUN dotnet publish course-service/src/TrainingHub.CourseService.Worker.ML/TrainingHub.CourseService.Worker.ML.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "TrainingHub.CourseService.Worker.ML.dll"]
