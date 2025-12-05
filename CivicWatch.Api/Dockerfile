# Estágio 1: Build - Atualizado para SDK 9.0
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar o ficheiro de projeto e restaurar as dependências
COPY ["CivicWatch.Api.csproj", "./"]
RUN dotnet restore "CivicWatch.Api.csproj"

# Copiar todo o código fonte
COPY . .
WORKDIR "/src/."

# Compilar a aplicação
RUN dotnet build "CivicWatch.Api.csproj" -c Release -o /app/build

# Publicar a aplicação
FROM build AS publish
RUN dotnet publish "CivicWatch.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 2: Runtime - Atualizado para ASP.NET 9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CivicWatch.Api.dll"]