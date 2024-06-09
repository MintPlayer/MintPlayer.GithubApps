#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 80:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MintPlayer.GithubApps/MintPlayer.GithubApps.csproj", "MintPlayer.GithubApps/"]
COPY ["MintPlayer.AspNetCore.BotFramework/MintPlayer.AspNetCore.BotFramework.csproj", "MintPlayer.AspNetCore.BotFramework/"]
COPY ["MintPlayer.AspNetCore.BotFramework.Abstractions/MintPlayer.AspNetCore.BotFramework.Abstractions.csproj", "MintPlayer.AspNetCore.BotFramework.Abstractions/"]
RUN dotnet restore "./MintPlayer.GithubApps/MintPlayer.GithubApps.csproj"
COPY . .
WORKDIR "/src/MintPlayer.GithubApps"
RUN dotnet build "./MintPlayer.GithubApps.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MintPlayer.GithubApps.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MintPlayer.GithubApps.dll"]