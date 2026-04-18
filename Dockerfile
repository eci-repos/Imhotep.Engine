# Stage 1: Base runtime environment (INF-001)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Stage 2: Build environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["Imhotep.Macs.Court.Cms.Intake.Console/Imhotep.Macs.Court.Cms.Intake.Console.csproj", "Console/"]
COPY ["Imhotep.Macs.Court.Cms.Intake/Imhotep.Macs.Court.Cms.Intake.csproj", "Library/"]
RUN dotnet restore "Console/Imhotep.Macs.Court.Cms.Intake.Console.csproj"

# Copy the remaining source code and compile
COPY . .
WORKDIR "/src/Imhotep.Macs.Court.Cms.Intake.Console"
RUN dotnet build "Imhotep.Macs.Court.Cms.Intake.Console.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "Imhotep.Macs.Court.Cms.Intake.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Imhotep.Macs.Court.Cms.Intake.Console.dll"]
