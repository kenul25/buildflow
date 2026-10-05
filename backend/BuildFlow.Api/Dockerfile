FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY BuildFlow.Api.csproj ./
RUN dotnet restore BuildFlow.Api.csproj
COPY . ./
RUN dotnet publish BuildFlow.Api.csproj --configuration Release --no-restore --output /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
COPY --from=build /out ./
COPY docker-start.sh /usr/local/bin/buildflow-start.sh
RUN mkdir -p /app/uploads
EXPOSE 10000
ENTRYPOINT ["sh", "/usr/local/bin/buildflow-start.sh"]
