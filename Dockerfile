FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BlazorSample.csproj", "./"]
RUN dotnet restore "BlazorSample.csproj"

COPY . .
RUN dotnet publish "BlazorSample.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlazorSample.dll"]
