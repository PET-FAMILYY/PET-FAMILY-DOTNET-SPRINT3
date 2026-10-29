# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro para aproveitar o cache do restore
COPY src/PetCare.Domain/PetCare.Domain.csproj src/PetCare.Domain/
COPY src/PetCare.Application/PetCare.Application.csproj src/PetCare.Application/
COPY src/PetCare.Infrastructure/PetCare.Infrastructure.csproj src/PetCare.Infrastructure/
COPY src/PetCare.API/PetCare.API.csproj src/PetCare.API/
RUN dotnet restore src/PetCare.API/PetCare.API.csproj

COPY src/ src/
RUN dotnet publish src/PetCare.API/PetCare.API.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PetCare.API.dll"]
