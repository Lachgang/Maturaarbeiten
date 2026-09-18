FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Maturaarbeiten_Wyss_Damian_I3B/Maturaarbeiten_Wyss_Damian_I3B.csproj", "Maturaarbeiten_Wyss_Damian_I3B/"]
RUN dotnet restore "./Maturaarbeiten_Wyss_Damian_I3B/Maturaarbeiten_Wyss_Damian_I3B.csproj"
COPY . .
WORKDIR "/src/Maturaarbeiten_Wyss_Damian_I3B"
RUN dotnet build "./Maturaarbeiten_Wyss_Damian_I3B.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Maturaarbeiten_Wyss_Damian_I3B.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Maturaarbeiten_Wyss_Damian_I3B.dll"]