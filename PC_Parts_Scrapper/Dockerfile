# --- STAGE 1: Build the app & install Playwright browsers + OS dependencies ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore
COPY *.csproj ./
RUN dotnet restore

# Copy source code and publish
COPY . ./
RUN dotnet publish "PC_Parts_Scrapper.csproj" -c Release -o /app/out

# Install Playwright CLI tool
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"

# Install Firefox AND auto-install all OS dependencies required by Playwright
RUN playwright install --with-deps firefox

# --- STAGE 2: Run the app ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published application
COPY --from=build /app/out .

# Copy installed browser binaries and system libraries from Stage 1 into Stage 2
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Install minimal core libraries for runtime
RUN apt-get update && apt-get install -y \
    libxcursor1 \
    libgtk-3-0* \
    libasound2* \
    && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "PC_Parts_Scrapper.dll"]