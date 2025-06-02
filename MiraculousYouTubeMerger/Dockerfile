FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
ENV GENERAL_TMDDAPIKEY=""
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MiraculousYouTubeMerger/MiraculousYouTubeMerger.csproj", "MiraculousYouTubeMerger/"]
RUN dotnet restore "MiraculousYouTubeMerger/MiraculousYouTubeMerger.csproj"
COPY . .
WORKDIR "/src/MiraculousYouTubeMerger"
RUN dotnet build "./MiraculousYouTubeMerger.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MiraculousYouTubeMerger.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MiraculousYouTubeMerger.dll"]
