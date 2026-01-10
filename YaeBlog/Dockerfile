FROM mcr.azure.cn/dotnet/aspnet:10.0

ARG COMMIT_ID
ENV COMMIT_ID=${COMMIT_ID}

WORKDIR /app
COPY bin/Release/net10.0/publish/ ./
COPY source/ ./source/
COPY appsettings.json .

ENTRYPOINT ["dotnet", "YaeBlog.dll", "serve"]
