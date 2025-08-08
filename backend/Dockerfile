FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MedicalScheduling.API/MedicalScheduling.API.csproj", "MedicalScheduling.API/"]
RUN dotnet restore "MedicalScheduling.API/MedicalScheduling.API.csproj"
COPY . .
WORKDIR "/src/MedicalScheduling.API"
RUN dotnet build "MedicalScheduling.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MedicalScheduling.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedicalScheduling.API.dll"]