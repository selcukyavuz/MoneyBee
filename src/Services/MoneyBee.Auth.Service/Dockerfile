FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["MoneyBee.sln", "."]

# Copy project files
COPY ["src/Services/MoneyBee.Auth.Service/MoneyBee.Auth.Service.csproj", "src/Services/MoneyBee.Auth.Service/"]
COPY ["src/Shared/MoneyBee.Common/MoneyBee.Common.csproj", "src/Shared/MoneyBee.Common/"]

# Restore dependencies
RUN dotnet restore "src/Services/MoneyBee.Auth.Service/MoneyBee.Auth.Service.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/MoneyBee.Auth.Service"
RUN dotnet build "MoneyBee.Auth.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MoneyBee.Auth.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MoneyBee.Auth.Service.dll"]
