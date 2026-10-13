FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["src/Core.Domain/Core.Domain.csproj", "src/Core.Domain/"]
COPY ["src/Core.Application/Core.Application.csproj", "src/Core.Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Presentation.API/Presentation.API.csproj", "src/Presentation.API/"]
RUN case "$(uname -m)" in x86_64) printf linux-musl-x64 > /tmp/runtime-id ;; aarch64) printf linux-musl-arm64 > /tmp/runtime-id ;; *) exit 1 ;; esac \
    && dotnet restore src/Presentation.API/Presentation.API.csproj -r "$(cat /tmp/runtime-id)" -p:ProductionContainer=true
COPY src/ ./src/
RUN dotnet publish src/Presentation.API/Presentation.API.csproj -c Release -r "$(cat /tmp/runtime-id)" --self-contained true --no-restore -p:ProductionContainer=true -p:DebugType=None -p:DebugSymbols=false -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=false \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["./Presentation.API"]
