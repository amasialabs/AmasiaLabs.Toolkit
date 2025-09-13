# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT=src/AmasiaLabs.Toolkit.FlowflakeId.Grpc.Host/AmasiaLabs.Toolkit.FlowflakeId.Grpc.Host.csproj
WORKDIR /src
COPY . ./
RUN dotnet restore "$PROJECT"
RUN dotnet publish "$PROJECT" -c Release -o /out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "AmasiaLabs.Toolkit.FlowflakeId.Grpc.Host.dll"]

