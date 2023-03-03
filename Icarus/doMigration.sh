#!/bin/bash

# A script to automate the entire migration and database updating process.
# I have 0 experience in bash scripting so this might also just kill your Linux machine lol
# - Tower 2021
# Back after popular demand!
# - Tower 2022
# Haha this totally didn't nuke the folder you're in if it didn't contain a folder called "Migrations"
# Anyways fixed it haha so it's back and better than ever!
# - Tower 2023
# This is an updated version for the docker container - the main difference is that it won't nuke the DB nor existing Migrations, making it usable on prod (hopefully). Also skips the check for the MSSQL container, since docker compose should ensure that.
# - Tower 03/03/2023
echo "Creating a new migration..."
dotnet ef migrations add Automated &> /dev/null
echo "Updating the database..."
dotnet ef database update &> /dev/null
echo "Done."