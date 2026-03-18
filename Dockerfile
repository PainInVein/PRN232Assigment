FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG BUILD_CONFIGURATION=Release

WORKDIR /app

EXPOSE 80


COPY ["PRN232.NMS.API/PRN232.NMS.API.csproj", "PRN232.NMS.API/"]
COPY ["PRN232.NMS.Repo/PRN232.NMS.Repo.csproj", "PRN232.NMS.Repo/"]
COPY ["PRN232.NMS.Services/PRN232.NMS.Services.csproj", "PRN232.NMS.Services/"]
RUN dotnet restore "PRN232.NMS.API/PRN232.NMS.API.csproj"


COPY . .

WORKDIR /app/PRN232.NMS.API
RUN dotnet publish "PRN232.NMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore


WORKDIR /app/publish


ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80


COPY SU25LeopardDB.sql .

ENTRYPOINT ["dotnet", "PRN232.NMS.API.dll"]