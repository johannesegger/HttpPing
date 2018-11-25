FROM microsoft/dotnet:2.1.4-runtime-stretch-slim-arm32v7
COPY /deploy /src/app
WORKDIR /src/app
ENTRYPOINT [ "dotnet", "HttpPing.dll" ]