#!/bin/bash

# Script to run mado (markdown linter) on all markdown files

echo "Running mado on markdown files..."

# Run mado check on all markdown files
mado check .

echo "Markdown linting complete."
