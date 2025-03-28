FROM mcr.microsoft.com/dotnet/aspnet:9.0

ARG COMMIT_ID
ENV COMMIT_ID=${COMMIT_ID}

WORKDIR /app
COPY bin/Release/net9.0/publish/ ./
COPY source/ ./source/
COPY appsettings.json .

ENTRYPOINT ["dotnet", "YaeBlog.dll", "serve"]
