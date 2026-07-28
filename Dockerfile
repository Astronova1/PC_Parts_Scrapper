# Stage 1: Build using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["PC_Parts_Scrapper/PC_Parts_Scrapper.csproj", "PC_Parts_Scrapper/"]
RUN dotnet restore "PC_Parts_Scrapper/PC_Parts_Scrapper.csproj"

# Copy full solution and build
COPY . .
WORKDIR "/src/PC_Parts_Scrapper"
RUN dotnet build "PC_Parts_Scrapper.csproj" -c Release -o /app/build

# Stage 2: Publish compiled application
FROM build AS publish
RUN dotnet publish "PC_Parts_Scrapper.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Official Playwright .NET runtime container (Pre-installed Chromium & OS libs)
FROM mcr.microsoft.com/playwright/dotnet:v1.61.0-noble AS final
WORKDIR /app

# Copy published binaries into the pre-configured Playwright container
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PC_Parts_Scrapper.dll"]