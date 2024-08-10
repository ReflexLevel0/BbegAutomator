FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BbegAutomator/BbegAutomator.csproj", "BbegAutomator/"]
RUN dotnet restore "BbegAutomator/BbegAutomator.csproj"
COPY . .
WORKDIR "/src/BbegAutomator"
RUN dotnet build "BbegAutomator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BbegAutomator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BbegAutomator.dll"]
