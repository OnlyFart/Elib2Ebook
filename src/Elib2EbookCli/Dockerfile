FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

COPY . .
RUN dotnet restore src/Elib2EbookCli/Elib2EbookCli.csproj
RUN dotnet publish src/Elib2EbookCli/Elib2EbookCli.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "Elib2EbookCli.dll"]
