#!/bin/bash

# Load environment variables from .env file
if [ -f .env ]; then
    echo "Loading environment variables from .env file..."
    export $(cat .env | grep -v '^#' | xargs)
    echo "API_KEY loaded: ${API_KEY:0:10}..." # Show first 10 chars for verification
else
    echo "Warning: .env file not found!"
fi

# Navigate to the GoalSettingApp directory and run the application
cd GoalSettingApp
echo "Starting the application..."
dotnet run