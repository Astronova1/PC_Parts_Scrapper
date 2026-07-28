# Stage 1: Build using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["PC_Parts_Scrapper/PC_Parts_Scrapper.csproj", "PC_Parts_Scrapper/"]
RUN dotnet restore "PC_Parts_Scrapper/PC_Parts_Scrapper.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/PC_Parts_Scrapper"
RUN dotnet build "PC_Parts_Scrapper.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "PC_Parts_Scrapper.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: ASP.NET Core 10 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Prevent interactive prompts during apt package installation
ENV DEBIAN_FRONTEND=noninteractive

# Copy published application
COPY --from=publish /app/publish .

# Install PowerShell and Playwright browser + OS dependencies automatically
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    gnupg \
    powershell \
    && rm -rf /var/lib/apt/lists/* \
    && pwsh ./playwright.ps1 install --with-deps chromium

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PC_Parts_Scrapper.dll"]