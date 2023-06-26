# syntax=docker/dockerfile:1

# Set up the build environment
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env

# Specify the work directory
WORKDIR /Icarus

# Restore the project
COPY Icarus/*.csproj .
RUN dotnet restore
# Copy all of the other files to the workdir
COPY Icarus .

# Build the project
RUN dotnet publish -c Release -o /publish

# Set up the runtime environment
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine as runtime
WORKDIR /publish
COPY --from=build-env /publish .
COPY ./Icarus/appsettings.json /publish
COPY ./Icarus/appsettings.staging.json /publish
COPY ./Icarus/appsettings.prod.json /publish
COPY ./Icarus/ValueRelationShips.xml /publish

RUN apt-get update
RUN apt-get install -y python3
ENTRYPOINT ["dotnet", "Icarus.dll"]