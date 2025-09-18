#!/bin/bash

# Comprehensive markdown fixes

echo "Applying comprehensive markdown fixes..."

# Fix PROJECT_SUMMARY.md - add language to code block
# shellcheck disable=SC2016
sed -i '' '5s/```/```text/' ./PROJECT_SUMMARY.md

# Fix PUBLISHING.md - add language to code block  
# shellcheck disable=SC2016
sed -i '' '116s/```/```text/' ./PUBLISHING.md

# Fix samples README - add language to code block
# shellcheck disable=SC2016
sed -i '' '21s/```/```csharp/' ./samples/PQSoft.JsonComparer.Sample/README.md

# Fix HttpFile TODO.md - add languages to code blocks
# shellcheck disable=SC2016
sed -i '' '24s/```/```http/' ./src/PQSoft.HttpFile/TODO.md
# shellcheck disable=SC2016
sed -i '' '38s/```/```csharp/' ./src/PQSoft.HttpFile/TODO.md

echo "Fixed code block language specifications"

# Fix some ordered list numbering in key files
perl -i -pe 's/^(\s*)1\. /\1$counter++.". "/ge; BEGIN{$counter=0}' ./CONTRIBUTING.md
perl -i -pe 's/^(\s*)1\. /\1$counter++.". "/ge; BEGIN{$counter=0}' ./GETTING_STARTED.md
perl -i -pe 's/^(\s*)1\. /\1$counter++.". "/ge; BEGIN{$counter=0}' ./PUBLISHING.md

echo "Fixed ordered list numbering"

echo "Remaining issues require manual review for line length and spacing"
