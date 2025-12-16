# Migration Instructions

## Moving Test Files to XafApiConverterTests Project

### Files to Copy

Copy the following files from `XafApiConverter/Tests/ClassCommenterTests/TestFiles/` to `XafApiConverterTests/TestFiles/ClassCommenterTests/`:

```
CustomIntegerEditor.cs
CustomIntegerEditor_commented.cs
CustomLayoutTemplates.cs
CustomLayoutTemplates_commented.cs
CustomStringEditor.cs
CustomStringEditor_commented.cs
WebModule.cs
WebModule.Designer.cs
WebModule_commented.cs
WebModule_commented.Designer.cs
```

### PowerShell Script to Copy Files

```powershell
# Run from solution root directory

$source = "XafApiConverter\Tests\ClassCommenterTests\TestFiles"
$dest = "XafApiConverterTests\TestFiles\ClassCommenterTests"

# Create destination directory
New-Item -ItemType Directory -Path $dest -Force

# Copy all test files
Copy-Item "$source\*.cs" -Destination $dest -Force

Write-Host "Test files copied successfully!" -ForegroundColor Green
```

### Bash Script to Copy Files

```bash
#!/bin/bash
# Run from solution root directory

SOURCE="XafApiConverter/Tests/ClassCommenterTests/TestFiles"
DEST="XafApiConverterTests/TestFiles/ClassCommenterTests"

# Create destination directory
mkdir -p "$DEST"

# Copy all test files
cp "$SOURCE"/*.cs "$DEST/"

echo "Test files copied successfully!"
```

### Manual Steps

1. Create directory: `XafApiConverterTests/TestFiles/ClassCommenterTests/`
2. Copy all `.cs` files from source to destination
3. Verify files are copied

### After Migration

1. Delete old test files from `XafApiConverter/Tests/`
2. Delete `Tests` folder from XafApiConverter project
3. Update solution file if needed
4. Run `dotnet build` to verify
5. Run `dotnet test` to verify tests work

### Verification

```bash
# Verify structure
cd XafApiConverterTests
tree TestFiles  # or dir /s on Windows

# Build and test
dotnet build
dotnet test
```

Expected output:
```
Test run successful: 9 tests passed
```
