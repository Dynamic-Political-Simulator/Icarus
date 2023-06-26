# syntax=docker/dockerfile:1

# Python stuff
FROM python:slim as python
RUN python -m venv /venv
COPY Icarus/requirements.txt .
RUN /venv/bin/python -m pip install -r requirements.txt


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
COPY ./dpsproject-11f5133691e4.json /publish
COPY ./PythonScripts /publish


COPY --from=python /venv /python

ENTRYPOINT ["dotnet", "Icarus.dll"]
