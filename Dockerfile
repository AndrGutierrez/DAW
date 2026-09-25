FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Core.Domain/Core.Domain.csproj", "src/Core.Domain/"]
COPY ["src/Core.Application/Core.Application.csproj", "src/Core.Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Presentation.API/Presentation.API.csproj", "src/Presentation.API/"]
RUN dotnet restore "src/Presentation.API/Presentation.API.csproj"

COPY . .
RUN dotnet publish "src/Presentation.API/Presentation.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Presentation.API.dll"]
