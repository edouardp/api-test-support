#!/bin/bash

# Script to run shellcheck on all shell scripts

echo "Running shellcheck on shell scripts..."

# Check if shellcheck is installed
if ! command -v shellcheck &> /dev/null; then
    echo "Error: shellcheck is not installed."
    echo "Install with: brew install shellcheck"
    exit 1
fi

# Find and check all shell scripts
find . -name "*.sh" -type f -exec shellcheck {} \;

echo "Shell script checking complete."
