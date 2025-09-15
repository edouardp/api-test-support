#!/bin/bash

# Build and pack NuGet packages
echo "Building and packing NuGet packages..."

# Clean previous builds
dotnet clean --configuration Release

# Build solution
dotnet build --configuration Release --no-restore

# Pack packages
dotnet pack src/PQSoft.HttpFile/PQSoft.HttpFile.csproj --configuration Release --no-build
dotnet pack src/PQSoft.JsonComparer/PQSoft.JsonComparer.csproj --configuration Release --no-build
dotnet pack src/PQSoft.JsonComparer.AwesomeAssertions/PQSoft.JsonComparer.AwesomeAssertions.csproj --configuration Release --no-build

# Upload to NuGet.org (requires API key)
echo "Uploading packages to NuGet.org..."

if [ -z "$NUGET_API_KEY" ]; then
    echo "Warning: NUGET_API_KEY environment variable not set. Skipping upload."
    echo "Set your API key with: export NUGET_API_KEY=your_api_key_here"
    exit 1
fi

dotnet nuget push "src/PQSoft.HttpFile/bin/Release/*.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
dotnet nuget push "src/PQSoft.JsonComparer/bin/Release/*.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
dotnet nuget push "src/PQSoft.JsonComparer.AwesomeAssertions/bin/Release/*.nupkg" --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate

echo "Build and upload complete!"
