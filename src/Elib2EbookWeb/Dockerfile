FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY . .
RUN dotnet restore src/Elib2EbookWeb/Elib2EbookWeb.csproj
RUN dotnet publish src/Elib2EbookWeb/Elib2EbookWeb.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "Elib2EbookWeb.dll"]
