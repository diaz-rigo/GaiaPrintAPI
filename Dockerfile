# Dockerfile (colocar en la raíz del repo, donde está la .sln)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos la solución y proyecto explícitamente (JSON form avoids shell expansion)
COPY ["GaiaPrintAPI.sln", "./"]
COPY ["GaiaPrintAPI/GaiaPrintAPI.csproj", "GaiaPrintAPI/"]

RUN dotnet restore "GaiaPrintAPI/GaiaPrintAPI.csproj"

# Copiamos todo y publicamos
COPY . .
WORKDIR /src/GaiaPrintAPI
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "GaiaPrintAPI.dll"]
