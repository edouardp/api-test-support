# GitHub Actions Workflows

This directory contains the GitHub Actions workflows for the PQSoft API Test Support libraries.

## Workflows

### Build and Test (`build-and-test.yml`)

**Triggers:**
- Push to `main` branch
- Pull requests to `main` branch

**Actions:**
- Restores dependencies
- Builds all projects in Release configuration
- Runs all unit tests
- Collects code coverage
- Uploads test results as artifacts

### Publish NuGet Packages (`publish.yml`)

**Triggers:**
- GitHub releases (when a release is published)
- Manual workflow dispatch (allows specifying version)

**Packages Created:**
- `PQSoft.JsonComparer` - Core JSON comparison library
- `PQSoft.HttpFile` - HTTP file parsing library
- `PQSoft.JsonComparer.AwesomeAssertions` - AwesomeAssertions extensions

**Actions:**
- Builds all projects in Release configuration
- Runs all tests to ensure quality
- Packages all non-test projects into NuGet packages
- Publishes packages to NuGet.org
- Uploads package artifacts

## Setup Requirements

### NuGet API Key

To publish packages to NuGet.org, you need to set up a repository secret:

1. Go to your repository's Settings > Secrets and variables > Actions
2. Add a new repository secret named `NUGET_API_KEY`
3. Set the value to your NuGet.org API key

### Publishing Packages

#### Method 1: Create a GitHub Release (Recommended)

1. Go to your repository on GitHub
2. Click "Releases" → "Create a new release"
3. Create a new tag (e.g., `v1.0.0`)
4. Fill in the release title and description
5. Click "Publish release"
6. The workflow will automatically trigger and publish packages with the version from the tag

#### Method 2: Manual Workflow Dispatch

1. Go to Actions tab in your repository
2. Select "Publish NuGet Packages" workflow
3. Click "Run workflow"
4. Specify the version number (e.g., `1.0.0`)
5. Click "Run workflow"

## Package Versioning

- The workflow automatically extracts version from Git tags (removes 'v' prefix if present)
- For manual dispatch, you can specify any semantic version
- All three packages will use the same version number
- Packages are built with `--skip-duplicate` to avoid conflicts

## Package Metadata

Each package includes:
- Proper semantic versioning
- Author and company information
- Comprehensive descriptions
- Relevant tags for discoverability
- Repository links
- MIT license information
- README file references

## Artifacts

Both workflows upload artifacts:
- **Build and Test**: Test results and coverage reports
- **Publish**: Generated NuGet package files (.nupkg)

These artifacts are available for download from the workflow run page for 90 days.