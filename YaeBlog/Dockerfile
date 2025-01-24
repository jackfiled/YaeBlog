FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY bin/Release/net9.0/publish/ ./
COPY source/ ./source/
COPY appsettings.json .

ENTRYPOINT ["dotnet", "YaeBlog.dll", "serve"]
