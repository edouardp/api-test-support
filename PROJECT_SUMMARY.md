# PQSoft Open Source Project Summary

## Project Structure

```text
api-test-support/
├── .editorconfig                  # Editor configuration
├── .github/                       # GitHub configuration
│   └── workflows/                 # GitHub Actions workflows
├── .gitignore                     # Git ignore file
├── CHANGELOG.md                   # Changelog
├── CONTRIBUTING.md                # Contributing guidelines
├── GETTING_STARTED.md             # Getting started guide
├── LICENSE                        # MIT License
├── PROJECT_SUMMARY.md             # This file
├── PUBLISHING.md                  # Publishing guide
├── README.md                      # Main README
├── api-test-support.sln           # Solution file
├── build-nuget.sh                 # Build and publish script
├── docs/                          # Documentation
│   ├── api/                       # API documentation
│   └── index.md                   # Documentation index
├── lint-markdown.sh               # Markdown linting script
├── samples/                       # Sample projects
│   └── PQSoft.JsonComparer.Sample/  # Sample console application
└── src/                           # Source code
    ├── PQSoft.HttpFile/           # HTTP file parsing library
    ├── PQSoft.HttpFile.UnitTests/ # HTTP file tests
    ├── PQSoft.JsonComparer/       # JSON comparison library
    ├── PQSoft.JsonComparer.UnitTests/  # JSON comparer tests
    ├── PQSoft.JsonComparer.AwesomeAssertions/  # FluentAssertions extensions
    └── PQSoft.JsonComparer.AwesomeAssertions.UnitTests/  # Assertions tests
```

## Features Implemented

### PQSoft.HttpFile

- Parse HTTP requests with method, URL, headers, and body
- Parse HTTP responses with status code, headers, and body
- Handle HTTP headers with parameters
- Convert parsed requests to HttpRequestMessage objects
- Semantic header comparison for testing

### PQSoft.JsonComparer

- **Exact Match**: Validates that two JSON documents are identical (except for
  tokens)
- **Subset Match**: Verifies that all elements in expected JSON exist within
  actual JSON
- **Token Support**: Extract values from actual JSON using tokens like
  `[[TOKEN_NAME]]`
- **Function Execution**: Execute functions like `{{GUID()}}`, `{{NOW()}}`, and
  `{{UTCNOW()}}`
- **Variable Substitution**: Substitute variables from provided context during
  preprocessing
- **Detailed Mismatch Reporting**: Provides structured information on
  differences
- **Custom Function Registration**: Extend functionality with custom functions

### PQSoft.JsonComparer.AwesomeAssertions

- FluentAssertions extensions for JSON comparison
- Fluent API for JSON assertions
- Token extraction integration
- Both exact match and subset match assertions

## Current Status

- All three packages are implemented and functional
- Comprehensive unit test coverage
- Documentation available at GitHub Pages
- Automated build and publish pipeline via build-nuget.sh script
- Published to NuGet.org

## Next Steps

### Potential Enhancements

- Add JsonComparerOptions class for configurable comparison behavior
    - Case-insensitive string comparison
    - Ignore array order
    - Exclude specific paths from comparison
- Support for JSON Schema validation
- Performance optimizations for large JSON documents
- Support for JSON Patch generation

### Documentation Improvements

- Add more comprehensive examples
- Create video tutorials
- Expand API documentation

### Community

- Set up issue templates
- Create a roadmap for future development
- Add more sample applications
