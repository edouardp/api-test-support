#!/bin/bash

# Check if version parameter is provided
if [ -z "$1" ]; then
    echo "Error: Version number required"
    echo "Usage: ./build-nuget.sh <version>"
    echo "Example: ./build-nuget.sh 1.0.1"
    exit 1
fi

VERSION=$1

# Check if API key is set
if [ -z "$NUGET_API_KEY" ]; then
    echo "Error: NUGET_API_KEY environment variable not set"
    echo "Set your API key with: export NUGET_API_KEY=your_api_key_here"
    exit 1
fi

echo "Building and packing NuGet packages with version $VERSION..."

# Clean previous builds
dotnet clean --configuration Release

# Build individual projects that will be packaged
dotnet build src/PQSoft.HttpFile/PQSoft.HttpFile.csproj --configuration Release --no-restore
dotnet build src/PQSoft.JsonComparer/PQSoft.JsonComparer.csproj --configuration Release --no-restore
dotnet build src/PQSoft.JsonComparer.AwesomeAssertions/PQSoft.JsonComparer.AwesomeAssertions.csproj --configuration Release --no-restore
dotnet build src/PQSoft.ReqNRoll/PQSoft.ReqNRoll.csproj --configuration Release --no-restore

# Pack packages with specified version (in dependency order)
echo "Packing packages..."
dotnet pack src/PQSoft.HttpFile/PQSoft.HttpFile.csproj --configuration Release --no-build -p:Version="$VERSION"
dotnet pack src/PQSoft.JsonComparer/PQSoft.JsonComparer.csproj --configuration Release --no-build -p:Version="$VERSION"
dotnet pack src/PQSoft.JsonComparer.AwesomeAssertions/PQSoft.JsonComparer.AwesomeAssertions.csproj --configuration Release --no-build -p:Version="$VERSION"
dotnet pack src/PQSoft.ReqNRoll/PQSoft.ReqNRoll.csproj --configuration Release --no-build -p:Version="$VERSION"

# Upload to NuGet.org in dependency order
echo "Uploading packages to NuGet.org..."
dotnet nuget push "src/PQSoft.HttpFile/bin/Release/PQSoft.HttpFile.$VERSION.nupkg" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate
dotnet nuget push "src/PQSoft.JsonComparer/bin/Release/PQSoft.JsonComparer.$VERSION.nupkg" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate
dotnet nuget push "src/PQSoft.JsonComparer.AwesomeAssertions/bin/Release/PQSoft.JsonComparer.AwesomeAssertions.$VERSION.nupkg" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate
dotnet nuget push "src/PQSoft.ReqNRoll/bin/Release/PQSoft.ReqNRoll.$VERSION.nupkg" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate

echo "Build and upload complete for version $VERSION!"
