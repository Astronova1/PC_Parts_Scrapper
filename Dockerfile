# Stage 1: Build using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the csproj from the subfolder and restore dependencies
COPY ["PC_Parts_Scrapper/PC_Parts_Scrapper.csproj", "PC_Parts_Scrapper/"]
RUN dotnet restore "PC_Parts_Scrapper/PC_Parts_Scrapper.csproj"

# Copy all project files into the image
COPY . .

# Set working directory to the subfolder containing the code
WORKDIR "/src/PC_Parts_Scrapper"
RUN dotnet build "PC_Parts_Scrapper.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "PC_Parts_Scrapper.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Setup the ASP.NET Core 10 runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render exposes the application port via environment variable
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PC_Parts_Scrapper.dll"]