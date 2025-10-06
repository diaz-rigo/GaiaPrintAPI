# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copia csproj y restaura para aprovechar cache
COPY *.sln .
COPY GaiaPrintAPI/*.csproj ./GaiaPrintAPI/
RUN dotnet restore

# copia todo y publica
COPY . .
WORKDIR /src/GaiaPrintAPI
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Ejecutar en el puerto que asigna la plataforma (lo manejamos en Program.cs)
ENTRYPOINT ["dotnet", "GaiaPrintAPI.dll"]
