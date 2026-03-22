    # See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

    # This stage is used when running from VS in fast mode (Default for Debug configuration)
    FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
    WORKDIR /app
    EXPOSE 5000
    ENV ASPNETCORE_URLS=http://+:5000


    # This stage is used to build the service project
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
    WORKDIR /src
    COPY Project_Group3.csproj .
    RUN dotnet restore ./Project_Group3.csproj
    COPY . .
    RUN dotnet build ./Project_Group3.csproj -c Release -o ./build
    RUN dotnet publish ./Project_Group3.csproj -c Release -o ./publish /p:UseAppHost=false

    # This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
    FROM base AS final
    WORKDIR /app
    COPY --from=build /src/publish .
    ENTRYPOINT ["dotnet", "Project_Group3.dll"]