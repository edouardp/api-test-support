#!/bin/bash

# Script to fix common markdown issues

echo "Fixing markdown issues..."

# Fix trailing spaces (MD009)
find . -name "*.md" -type f -exec sed -i '' 's/[[:space:]]*$//' {} \;

echo "Fixed trailing spaces"

# Fix multiple consecutive blank lines (MD012) - replace 3+ blank lines with 2
find . -name "*.md" -type f -exec perl -i -pe 's/\n\n\n+/\n\n/g' {} \;

echo "Fixed multiple consecutive blank lines"

echo "Manual fixes still needed for:"
echo "- Line length (MD013) - lines over 80 characters"
echo "- List formatting (MD032) - lists need blank lines around them"
echo "- Ordered list prefixes (MD029) - should be 1. 2. 3. not 1. 1. 1."
echo "- Headers (MD022) - need blank lines around headers"
echo "- Code blocks (MD040) - need language specified"
