# Publishing Guide

This document provides detailed instructions for building and publishing PQSoft packages to NuGet.org.

## Prerequisites

### 1. NuGet API Key

1. Go to [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
2. Create a new API key with appropriate permissions
3. Set the API key as an environment variable:

```bash
# For current session
export NUGET_API_KEY="your-api-key-here"

# For permanent setup (add to ~/.zshrc or ~/.bashrc)
echo 'export NUGET_API_KEY="your-api-key-here"' >> ~/.zshrc
source ~/.zshrc
```

### 2. Version Planning

Follow [Semantic Versioning (SemVer)](https://semver.org/):
- **MAJOR** (1.0.0 → 2.0.0): Breaking changes
- **MINOR** (1.0.0 → 1.1.0): New features, backward compatible
- **PATCH** (1.0.0 → 1.0.1): Bug fixes, backward compatible

## Quick Publishing

Use the automated build script for the easiest publishing experience:

```bash
./build-nuget.sh 1.0.1
```

This script will:
1. Validate the version parameter and API key
2. Clean and build the solution
3. Pack all packages with the specified version
4. Upload packages to NuGet.org in dependency order

## Manual Publishing Process

### Step 1: Update Version

Edit `Directory.Build.props` to set the new version:

```xml
<PropertyGroup>
  <Version>1.0.1</Version>
  <!-- other properties -->
</PropertyGroup>
```

### Step 2: Build and Test

```bash
# Clean previous builds
dotnet clean --configuration Release

# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release --no-build
```

### Step 3: Pack Packages

```bash
# Pack with version override (optional if Directory.Build.props is updated)
dotnet pack src/PQSoft.HttpFile/PQSoft.HttpFile.csproj --configuration Release --no-build -p:Version=1.0.1
dotnet pack src/PQSoft.JsonComparer/PQSoft.JsonComparer.csproj --configuration Release --no-build -p:Version=1.0.1
dotnet pack src/PQSoft.JsonComparer.AwesomeAssertions/PQSoft.JsonComparer.AwesomeAssertions.csproj --configuration Release --no-build -p:Version=1.0.1
```

### Step 4: Verify Packages

Check the generated `.nupkg` files:

```bash
ls -la src/*/bin/Release/*.nupkg
```

### Step 5: Upload to NuGet.org

**Important**: Upload in dependency order to avoid failures:

```bash
# 1. Independent packages first
dotnet nuget push "src/PQSoft.HttpFile/bin/Release/PQSoft.HttpFile.1.0.1.nupkg" \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate

dotnet nuget push "src/PQSoft.JsonComparer/bin/Release/PQSoft.JsonComparer.1.0.1.nupkg" \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate

# 2. Dependent packages last
dotnet nuget push "src/PQSoft.JsonComparer.AwesomeAssertions/bin/Release/PQSoft.JsonComparer.AwesomeAssertions.1.0.1.nupkg" \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

## Package Dependencies

Understanding the dependency chain is crucial for upload order:

```text
PQSoft.HttpFile (no dependencies)
PQSoft.JsonComparer (depends on System.Text.Json)
PQSoft.JsonComparer.AwesomeAssertions (depends on PQSoft.JsonComparer + AwesomeAssertions)
```

## Troubleshooting

### Version Already Exists

If you get a "version already exists" error:
1. Check [nuget.org](https://www.nuget.org/packages) to confirm
2. Increment the version number
3. Try again

### API Key Issues

```bash
# Verify API key is set
echo $NUGET_API_KEY

# Test API key (should return your account info)
curl -H "X-NuGet-ApiKey: $NUGET_API_KEY" https://www.nuget.org/api/v2/Packages
```

### Dependency Resolution Failures

If `PQSoft.JsonComparer.AwesomeAssertions` fails to upload:
1. Ensure `PQSoft.JsonComparer` was uploaded successfully first
2. Wait a few minutes for NuGet indexing
3. Retry the upload

### Build Failures

```bash
# Clean everything and start fresh
dotnet clean
rm -rf src/*/bin src/*/obj
dotnet restore
dotnet build --configuration Release
```

## Best Practices

1. **Test Before Publishing**: Always run full test suite before publishing
2. **Version Consistency**: Keep all packages at the same version for simplicity
3. **Changelog**: Update CHANGELOG.md with release notes
4. **Git Tags**: Tag releases in Git for tracking
5. **Pre-release Testing**: Use pre-release versions (1.0.1-beta1) for testing

## Post-Publishing

After successful publishing:

1. **Verify on NuGet.org**: Check that packages appear correctly
2. **Update Documentation**: Update README with new version examples
3. **Create Git Tag**: Tag the release in Git
4. **Test Installation**: Verify packages can be installed in a fresh project

```bash
# Create and push git tag
git tag v1.0.1
git push origin v1.0.1

# Test installation in a new project
mkdir test-install && cd test-install
dotnet new console
dotnet add package PQSoft.JsonComparer --version 1.0.1
```
