FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY *.sln .
COPY ./ApiGateway.csproj ./ApiGateway.csproj
RUN dotnet restore

COPY . .
WORKDIR /source
RUN dotnet publish -c release -o /app 

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT [ "dotnet", "ApiGateway.dll" ]

