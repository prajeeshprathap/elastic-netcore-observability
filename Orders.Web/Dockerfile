#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Orders.Web/Orders.Web.csproj", "Orders.Web/"]
RUN dotnet restore "Orders.Web/Orders.Web.csproj"
COPY . .
WORKDIR "/src/Orders.Web"
RUN dotnet build "Orders.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Orders.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orders.Web.dll"]