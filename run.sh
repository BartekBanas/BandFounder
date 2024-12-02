#!/bin/bash

# Navigate to Backend and start the .NET application
echo "Starting .NET Backend..."
cd ./Backend/BandFounder/BandFounder.Api || { echo "Backend directory not found!"; exit 1; }
dotnet run --launch-profile https &  # Run the .NET application in the background
BACKEND_PID=$!  # Capture the process ID to terminate later if needed
echo "Backend started with PID $BACKEND_PID"

# Navigate to Frontend and start the React project
echo "Starting React Frontend..."
cd ../../../Frontend/BandFounder || { echo "Frontend directory not found!"; exit 1; }
npm start &  # Run the React application in the background
FRONTEND_PID=$!  # Capture the process ID to terminate later if needed
echo "Frontend started with PID $FRONTEND_PID"

# Wait for user input to terminate the processes
echo "Both applications are running. Press [Ctrl+C] to stop or wait for termination."
trap "echo 'Stopping applications...'; kill $BACKEND_PID $FRONTEND_PID; exit" INT

# Keep the script running to maintain background processes
wait
