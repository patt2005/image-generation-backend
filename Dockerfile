FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["PhotoAiBackend.csproj", "./"]

RUN dotnet restore "PhotoAiBackend.csproj"

COPY . .
WORKDIR "/src/"
RUN dotnet build "PhotoAiBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish

RUN dotnet publish "PhotoAiBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PhotoAiBackend.dll"]
