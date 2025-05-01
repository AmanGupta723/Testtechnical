FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the dashboardAPI project files
COPY backend/dashboardAPI/*.csproj ./backend/dashboardAPI/
WORKDIR /src/backend/dashboardAPI
RUN dotnet restore

# Copy the rest of the dashboardAPI project files
COPY backend/dashboardAPI/ ./
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://*:$PORT
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE $PORT
ENTRYPOINT ["dotnet", "DashboardAPI.dll"] 