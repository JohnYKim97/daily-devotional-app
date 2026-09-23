FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DailyDevotional.Api/DailyDevotional.Api.csproj DailyDevotional.Api/
RUN dotnet restore DailyDevotional.Api/DailyDevotional.Api.csproj

COPY . .
RUN dotnet publish DailyDevotional.Api/DailyDevotional.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DailyDevotional.Api.dll"]