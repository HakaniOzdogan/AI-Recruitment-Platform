FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IkOtomasyon.Api.csproj ./
RUN dotnet restore IkOtomasyon.Api.csproj

COPY . ./
RUN dotnet publish IkOtomasyon.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid appgroup --create-home --shell /usr/sbin/nologin appuser

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CvStorage__RootPath=/app/storage

EXPOSE 8080
VOLUME ["/app/storage"]

COPY --from=build /app/publish ./
RUN mkdir -p /app/storage && chown -R appuser:appgroup /app

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl --fail http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "IkOtomasyon.Api.dll"]
