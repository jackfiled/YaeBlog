FROM mcr.azure.cn/dotnet/aspnet:10.0

ARG COMMIT_ID
ENV COMMIT_ID=${COMMIT_ID}

WORKDIR /app
COPY out/ ./
COPY source/ ./source/
COPY src/YaeBlog/appsettings.json .

ENV BLOG__ROOT="./source"

ENTRYPOINT ["dotnet", "YaeBlog.dll", "serve"]
