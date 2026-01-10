FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/MoneyBee.Transfer.Service/MoneyBee.Transfer.Service.csproj", "Services/MoneyBee.Transfer.Service/"]
COPY ["src/Shared/MoneyBee.Common/MoneyBee.Common.csproj", "Shared/MoneyBee.Common/"]
RUN dotnet restore "Services/MoneyBee.Transfer.Service/MoneyBee.Transfer.Service.csproj"
COPY src/ .
WORKDIR "/src/Services/MoneyBee.Transfer.Service"
RUN dotnet build "MoneyBee.Transfer.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MoneyBee.Transfer.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MoneyBee.Transfer.Service.dll"]
