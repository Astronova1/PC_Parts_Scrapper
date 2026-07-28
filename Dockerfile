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

# Stage 3: Install Playwright & Browsers inside SDK environment
# We install PowerShell and run Playwright install-deps to fetch Chromium + OS dependencies
RUN pwsh /app/publish/playwright.ps1 install --with-deps chromium

# Stage 4: ASP.NET Core 10 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy the compiled app from publish
COPY --from=publish /app/publish .

# Copy the downloaded Chromium binaries from root cache to runtime container
COPY --from=publish /root/.cache/ms-playwright /root/.cache/ms-playwright

# Install required Linux shared libraries for Chromium to run on Debian/Ubuntu
RUN apt-get update && apt-get install -y --no-install-recommends \
    libglib2.0-0 \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libdbus-1-3 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PC_Parts_Scrapper.dll"]