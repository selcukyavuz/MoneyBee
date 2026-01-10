FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["MoneyBee.sln", "."]

# Copy project files
COPY ["src/Services/MoneyBee.Customer.Service/MoneyBee.Customer.Service.csproj", "src/Services/MoneyBee.Customer.Service/"]
COPY ["src/Shared/MoneyBee.Common/MoneyBee.Common.csproj", "src/Shared/MoneyBee.Common/"]

# Restore dependencies
RUN dotnet restore "src/Services/MoneyBee.Customer.Service/MoneyBee.Customer.Service.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/MoneyBee.Customer.Service"
RUN dotnet build "MoneyBee.Customer.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MoneyBee.Customer.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MoneyBee.Customer.Service.dll"]
