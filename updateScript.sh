#!/bin/bash
echo "Checking for updates..."
# Refreshes remote state, not sure if necessary but just in case I put it in here
git remote update

# Shoutout to https://stackoverflow.com/a/75457224
# This essentially checks if there are any new commits in remote, the count of which is saved in $commits_behind
# Get the name of the current branch
branch=$(git rev-parse --abbrev-ref HEAD)

# Get the hash of the most recent commit on the local branch
local_commit=$(git rev-list --max-count=1 $branch)

# Get the hash of the most recent commit on the corresponding remote branch
remote_commit=$(git rev-list --max-count=1 origin/$branch)

# Count the number of commits between the local and remote branches
commits_behind=$(git rev-list --count $local_commit..$remote_commit)

if [ $commits_behind != "0" ]; then
	# If we are behind any commits, run git pull, rebuild the docker images and update the docker containers
	echo "Update found, rebuilding docker containers!"
	git pull
	./Icarus/doMigration.sh
	docker compose build
	docker compose up -d --remove-orphans
	yes | docker image prune # Prune the images, this is optional but I think it's better to run it since idk if docker compose build prunes previous images of Icarus
else
	# No updates found, so we do nothing
	echo "No updates found."
fi