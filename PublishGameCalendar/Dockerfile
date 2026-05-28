FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PublishGameCalendar/AquaOs.Calendar.csproj", "PublishGameCalendar/"]
RUN dotnet restore "PublishGameCalendar/AquaOs.Calendar.csproj"
COPY PublishGameCalendar/ PublishGameCalendar/
RUN dotnet publish "PublishGameCalendar/AquaOs.Calendar.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AquaOs.Calendar.dll"]