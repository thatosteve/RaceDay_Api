# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first so "dotnet restore" is cached until packages change
COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime stage (small image, no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Plain HTTP inside the container; the API address is injected at run time.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseHttpsRedirection=false \
    ApiBaseUrl=http://host.docker.internal:5080

EXPOSE 8080
COPY --from=build /app/publish .

# Run as the non-root user that ships with the .NET 8 images
USER app
ENTRYPOINT ["dotnet", "RaceDay_Mvc.dll"]
