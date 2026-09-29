FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/Candidatos.Api/Candidatos.Api.csproj backend/src/Candidatos.Api/
RUN dotnet restore backend/src/Candidatos.Api/Candidatos.Api.csproj
COPY backend/src/Candidatos.Api/ backend/src/Candidatos.Api/
RUN dotnet publish backend/src/Candidatos.Api/Candidatos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Candidatos.Api.dll"]
