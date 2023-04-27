# syntax=docker/dockerfile:1

# Set up the build environment
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine as build-env

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
ENTRYPOINT ["dotnet", "Icarus.dll"]