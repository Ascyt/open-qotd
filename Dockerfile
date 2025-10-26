FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App/Bot

# Copy project files
COPY ./Bot ./
# Restore as distinct layers
RUN dotnet restore ./OpenQotd.csproj
# Build and publish a release
RUN dotnet publish ./OpenQotd.csproj -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
#COPY .env /App/
WORKDIR /App/Bot
COPY --from=build /App/Bot/out .
ENTRYPOINT ["dotnet", "OpenQotd.dll"]
