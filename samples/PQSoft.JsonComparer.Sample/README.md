# PQSoft.JsonComparer Sample Application

This sample application demonstrates how to use the PQSoft.JsonComparer library to compare JSON documents.

## Features Demonstrated

1. Basic JSON comparison
2. Detecting value differences
3. Case-insensitive string comparison
4. Ignoring array order
5. Excluding specific paths from comparison

## Running the Sample

```bash
dotnet run
```

## Sample Output

```
PQSoft.JsonComparer Sample Application
=====================================

Example 1: Compare identical JSON objects
The JSON documents are equal.

Example 2: Compare JSON objects with different values
The JSON documents are different:
  Path: $.age
  Value 1: 30
  Value 2: 31
  Difference Type: ValueMismatch

Example 3: Compare JSON objects with case differences using case-insensitive comparison
Default comparer (case-sensitive):
The JSON documents are different:
  Path: $.name
  Value 1: "John"
  Value 2: "JOHN"
  Difference Type: ValueMismatch

Custom comparer (case-insensitive):
The JSON documents are equal.

Example 4: Compare JSON arrays with different order
Default comparer (respect array order):
The JSON documents are different:
  Path: $.colors[0]
  Value 1: "red"
  Value 2: "blue"
  Difference Type: ValueMismatch

  Path: $.colors[1]
  Value 1: "green"
  Value 2: "red"
  Difference Type: ValueMismatch

  Path: $.colors[2]
  Value 1: "blue"
  Value 2: "green"
  Difference Type: ValueMismatch

Custom comparer (ignore array order):
The JSON documents are equal.

Example 5: Compare JSON with excluded paths
Default comparer (include all paths):
The JSON documents are different:
  Path: $.metadata.lastUpdated
  Value 1: "2025-07-22"
  Value 2: "2025-07-23"
  Difference Type: ValueMismatch

Custom comparer (exclude $.metadata path):
The JSON documents are equal.
```
