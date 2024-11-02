# Use imagens mais leves se necessário
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar os arquivos do projeto e restaurar as dependências
COPY . .
RUN dotnet restore

# Publicar o projeto em modo Release
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Expor a porta 80
EXPOSE 80

# Definir variáveis de ambiente
ENV MUSICAS_DIRECTORY=/downloads/musicas
ENV VIDEOS_DIRECTORY=/downloads/videos

ENTRYPOINT ["dotnet", "BarraldevDownloader.dll"]
