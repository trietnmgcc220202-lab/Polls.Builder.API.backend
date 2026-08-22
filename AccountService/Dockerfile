# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj và restore trước để tận dụng cache layer
COPY ["AccountService/AccountService.csproj", "AccountService/"]
RUN dotnet restore "AccountService/AccountService.csproj"

# Copy toàn bộ source còn lại và build
COPY AccountService/. AccountService/
WORKDIR /src/AccountService
RUN dotnet build "AccountService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---- Publish Stage ----
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AccountService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---- Final Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]