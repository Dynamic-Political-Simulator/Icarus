#!/bin/bash

# A script to automate the entire migration and database updating process.
# I have 0 experience in bash scripting so this might also just kill your Linux machine lol
# - Tower 2021
# Back after popular demand!
# - Tower 2022
# Haha this totally didn't nuke the folder you're in if it didn't contain a folder called "Migrations"
# Anyways fixed it haha so it's back and better than ever!
# - Tower 2023
sudo docker ps | grep 'sql1' &> /dev/null
if [ $? != 0 ]; then
    echo "MSSQL docker instance not running!"
else
	if [[ -d ./Migrations ]]; then
    	echo "Deleting existing migrations..."
    	cd ./Migrations
    	rm -rf * # Delete all migrations
    	cd ..
	fi
    echo "Creating a new migration..."
    dotnet ef migrations add initial &> /dev/null
    echo "Updating the database..."
    yes "y" | dotnet ef database drop &> /dev/null
    dotnet ef database update &> /dev/null
    echo "Done."
fi