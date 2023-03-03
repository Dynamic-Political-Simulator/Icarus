# syntax=docker/dockerfile:1

# Set up the build environment
FROM mcr.microsoft.com/dotnet/sdk:3.1 as build-env
WORKDIR /Icarus
# Restore the project
COPY Icarus/*.csproj .
COPY Icarus/doMigration.sh .
RUN dotnet restore

RUN ./doMigration.sh

# Build the project
COPY Icarus .
RUN dotnet publish -c Release -o /publish

# Set up the runtime environment
FROM mcr.microsoft.com/dotnet/sdk:3.1 as runtime
WORKDIR /publish
COPY --from=build-env /publish .
COPY ./Icarus/appsettings.json /publish
ENTRYPOINT ["dotnet", "Icarus.dll"]