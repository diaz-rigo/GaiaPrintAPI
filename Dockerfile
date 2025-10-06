# Dockerfile (usar cuando GaiaPrintAPI.csproj está en la misma carpeta que el Dockerfile)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos la solución y el proyecto (ajusta el nombre .sln si es distinto)
COPY ["GaiaPrintAPI.sln", "./"]
COPY ["GaiaPrintAPI.csproj", "./"]

# Restaurar (ahora el path es directo)
RUN dotnet restore "GaiaPrintAPI.csproj"

# Copiamos todo y publicamos
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "GaiaPrintAPI.dll"]
