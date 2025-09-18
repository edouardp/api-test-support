#!/bin/bash

# Script to run trufflehog to scan for secrets in the repository

echo "Running trufflehog secret scan..."

# Check if trufflehog is installed
if ! command -v trufflehog &> /dev/null; then
    echo "Error: trufflehog is not installed."
    echo "Install with: brew install trufflehog"
    exit 1
fi

# Scan the current git repository for secrets
trufflehog git file://. --only-verified

echo "Secret scan complete."
