FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["BarraldevDownloader/BarraldevDownloader.csproj", "BarraldevDownloader/"]
RUN dotnet restore "BarraldevDownloader/BarraldevDownloader.csproj"
COPY . .
WORKDIR "/src/BarraldevDownloader"
RUN dotnet build "BarraldevDownloader.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BarraldevDownloader.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BarraldevDownloader.dll"]
