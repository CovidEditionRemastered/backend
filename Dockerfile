FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

COPY SoapyBackend.csproj src/
RUN dotnet restore src/"SoapyBackend.csproj"
COPY . src/.
WORKDIR src
RUN dotnet build "SoapyBackend.csproj" -c Release 

FROM build AS publish
RUN dotnet publish "SoapyBackend.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS final
RUN apk add curl
WORKDIR app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SoapyBackend.dll"]
