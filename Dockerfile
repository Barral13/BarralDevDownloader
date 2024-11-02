# Usar uma imagem do .NET SDK 8.0 para a fase de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar os arquivos do projeto e restaurar as dependências
COPY . .
RUN dotnet restore

# Publicar o projeto em modo Release para a pasta /app/publish
RUN dotnet publish -c Release -o /app/publish

# Usar uma imagem do .NET ASP.NET Core Runtime 8.0 para a fase final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Criar diretórios para downloads, músicas e vídeos
RUN mkdir -p /downloads/musicas /downloads/videos

COPY --from=build /app/publish .

# Expor a porta 80 para acesso externo
EXPOSE 80

# Definir o ponto de entrada
ENTRYPOINT ["dotnet", "BarraldevDownloader.dll"]
