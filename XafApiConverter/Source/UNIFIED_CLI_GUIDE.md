# XAF Migration Tool - Unified CLI

## Overview

**XafApiConverter** now provides a **unified migration workflow** that executes all three migration steps in sequence:

1. **Project Conversion** (.NET Framework ? .NET 9/10)
2. **Type Migration** (ASP.NET Web ? Blazor)
3. **Security Types Update** (SecuritySystem ? PermissionPolicy)

## Quick Start

### Run Complete Migration (All 3 Steps)

```bash
# Default: Executes all three steps
dotnet run -- MySolution.sln

# Or with explicit path
dotnet run -- --solution MySolution.sln

# Process all solutions in a directory
dotnet run -- C:\Projects\MyXafApp
```

### Run Specific Steps Only

```bash
# Only project conversion
dotnet run -- MySolution.sln --only-conversion

# Only type migration
dotnet run -- MySolution.sln --only-type-migration

# Only security update
dotnet run -- MySolution.sln --only-security-update

# Skip specific step
dotnet run -- MySolution.sln --skip-security-update
```

### Custom Configuration

```bash
# Custom .NET version and DevExpress version
dotnet run -- MySolution.sln --target-framework net10.0 --dx-version 26.1.6

# Use Directory.Packages.props
dotnet run -- MySolution.sln --directory-packages

# Save reports to custom location
dotnet run -- MySolution.sln --output D:\Reports
```

## Command Line Options

### Required Arguments

```
<path>                    Path to solution file (.sln/.slnx) or directory
```

### Common Options

```
-s, --solution <path>     Solution file or directory path
-tf, --target-framework   Target .NET version (default: net9.0)
                          Examples: net8.0, net9.0, net10.0
-dx, --dx-version         DevExpress version (default: 25.1.6)
                          Example: 25.1.6, 26.1.6
-o, --output <path>       Output directory for reports
-nb, --no-backup          Don't create backup files
-dp, --directory-packages Use Directory.Packages.props
```

### Step Control

```
--skip-conversion         Skip step 1 (project conversion)
--skip-type-migration     Skip step 2 (type migration)
--skip-security-update    Skip step 3 (security update)

--only-conversion         Execute only step 1
--only-type-migration     Execute only step 2
--only-security-update    Execute only step 3
```

### Other Options

```
-v, --validate            Validation mode only
-r, --report-only         Generate reports without modifications
-h, --help                Show help message
```

## Migration Steps Explained

### Step 1: Project Conversion

**What it does:**
- Converts `.csproj` files from legacy format to SDK-style
- Updates `TargetFramework` to .NET 9/10
- Adds appropriate NuGet packages based on project type:
  - **BASE** packages (all projects): 25 packages
  - **WINDOWS** packages (WinForms projects): +20 packages
  - **BLAZOR_WEB** packages (web projects): +20 packages
- Removes legacy assembly references
- Handles AssemblyInfo.cs and .resx files

**TRANS Rules Applied:**
- TRANS-001: SDK-Style Conversion
- TRANS-002: Target Framework Selection
- TRANS-003: Windows Desktop Properties
- TRANS-004: Assembly Reference Removal
- TRANS-005: NuGet Package Addition

**Output:**
- Converted `.csproj` files
- Backup files (`.csproj.bak`)
- Validation results

### Step 2: Type Migration

**What it does:**
- Migrates namespaces: `DevExpress.ExpressApp.Web.*` ? `DevExpress.ExpressApp.Blazor.*`
- Replaces types:
  - `WebApplication` ? `BlazorApplication`
  - `ASPxGridListEditor` ? `DxGridListEditor`
  - `*AspNetModule` ? `*BlazorModule`
- Processes both `.cs` and `.xafml` files
- Detects problematic types with NO Blazor equivalent
- Generates detailed migration report

**TRANS Rules Applied:**
- TRANS-006: SqlClient Namespace Migration
- TRANS-007: DevExpress Namespace Migrations
- TRANS-008: Type Replacements
- TRANS-009: NO_EQUIVALENT Detection (for LLM)

**Output:**
- Modified `.cs` and `.xafml` files
- `type-migration-report.md` (for LLM analysis)

### Step 3: Security Types Update

**What it does:**
- Updates security types:
  - `SecuritySystemUser` ? `PermissionPolicyUser`
  - `SecuritySystemRole` ? `PermissionPolicyRole`
  - And other security types
- Removes obsolete feature toggles
- Adds `PermissionPolicyRoleExtensions.cs` if needed
- Updates permission state setters

**Output:**
- Modified `.cs` files
- Added extension files (if needed)

## Examples

### Example 1: Complete Migration

```bash
cd XafApiConverter/Source
dotnet run -- D:\Projects\MyXafApp\MySolution.sln
```

**Output:**
```
?????????????????????????????????????????????????????????????
?     XAF Migration Tool - Complete Workflow               ?
?     .NET Framework ? .NET + Web ? Blazor Migration      ?
?????????????????????????????????????????????????????????????

Configuration:
  Path: D:\Projects\MyXafApp\MySolution.sln
  Target Framework: net9.0
  DevExpress Version: 25.1.6

Steps to execute:
  ? Step 1: Project Conversion
  ? Step 2: Type Migration
  ? Step 3: Security Update

Found 1 solution(s):
  • MySolution.sln

???????????????????????????????????????????????????????????
Processing: MySolution.sln
???????????????????????????????????????????????????????????

? Step 1/3: Project Conversion (.NET Framework ? .NET)

Found 5 project(s) to convert

  Converting MyXafApp.Module... ?
  Converting MyXafApp.Module.Blazor... ?
  Converting MyXafApp.Blazor.Server... ?
  Converting MyXafApp.Win... ?
  Converting MyXafApp.Module.Win... ?

Summary: 5 converted, 0 skipped, 0 failed

? Step 1 completed successfully

? Step 2/3: Type Migration (Web ? Blazor)

Starting Type Migration...

Phase 1: Loading solution...
  Loaded solution: MySolution.sln
  Projects: 5

Phase 2: Applying automatic replacements...
  Processing project: MyXafApp.Module
  Processing project: MyXafApp.Module.Blazor
  ...

Phase 3: Detecting problems for LLM analysis...
  Found 2 problematic classes
  Found 1 XAFML problems

Phase 5: Generating report...
  Report saved to: type-migration-report.md

? Step 2 completed successfully

? Step 3/3: Security Types Update

Processing security types...
[CHANGED] D:\...\Updater.cs
[ADDED] D:\...\PermissionPolicyRoleExtensions.cs

Security types updated: 3 file(s) changed

? Step 3 completed successfully

???????????????????????????????????????????????????????????
                    Final Summary
???????????????????????????????????????????????????????????

Solutions processed: 1
? All migrations completed successfully!
```

### Example 2: Custom .NET and DX Versions

```bash
dotnet run -- MySolution.sln \
  --target-framework net10.0 \
  --dx-version 26.1.6
```

### Example 3: Skip Security Update

```bash
dotnet run -- MySolution.sln --skip-security-update
```

### Example 4: Only Type Migration

```bash
dotnet run -- MySolution.sln --only-type-migration
```

### Example 5: Process Directory

```bash
# Finds all .sln files in directory recursively
dotnet run -- C:\Projects\MyXafApp
```

## Backward Compatibility

The tool maintains **backward compatibility** with legacy commands:

```bash
# Legacy: Explicit "convert" command
dotnet run -- convert MyProject.csproj

# Legacy: Explicit "migrate-types" command
dotnet run -- migrate-types MySolution.sln

# Legacy: Explicit "security-update" command
dotnet run -- security-update MySolution.sln
```

## Output Files

### Project Conversion
- `*.csproj` - Converted project files
- `*.csproj.bak` - Backup files (if `--no-backup` not specified)

### Type Migration
- Modified `.cs` and `.xafml` files
- `type-migration-report.md` - Detailed report for LLM analysis

### Security Update
- Modified `.cs` files
- `PermissionPolicyRoleExtensions.cs` - Added extension methods (if needed)

## Workflow Recommendations

### For New Projects
```bash
# Run complete migration
dotnet run -- MySolution.sln

# Review type-migration-report.md
# Share with LLM if needed

# Build and test
dotnet build MySolution.sln
dotnet test
```

### For Incremental Migration
```bash
# Step 1: Convert projects
dotnet run -- MySolution.sln --only-conversion

# Build and test
dotnet build

# Step 2: Migrate types
dotnet run -- MySolution.sln --only-type-migration

# Review report, apply LLM fixes

# Build and test
dotnet build

# Step 3: Update security
dotnet run -- MySolution.sln --only-security-update

# Final build and test
dotnet build
dotnet test
```

## Configuration Files

### Directory.Packages.props Support

Use `--directory-packages` flag to enable centralized package management:

```bash
dotnet run -- MySolution.sln --directory-packages
```

This creates/updates `Directory.Packages.props` instead of adding packages to individual project files.

## Error Handling

The tool provides detailed error messages and continues processing even if individual steps fail:

```
??  Step 1 completed with warnings
??  Completed with 1 error(s)
Review the output above for details
```

Exit codes:
- `0` - Success
- `1` - Errors occurred

## Advanced Usage

### Validation Mode

```bash
# Check projects without making changes
dotnet run -- MySolution.sln --validate
```

### Report Only Mode

```bash
# Generate reports without modifying files
dotnet run -- MySolution.sln --report-only
```

### Custom Report Location

```bash
dotnet run -- MySolution.sln --output D:\Reports\Migration
```

## Troubleshooting

### "No solution files found"
- Verify the path is correct
- Ensure `.sln` or `.slnx` files exist
- Check file permissions

### "Project conversion failed"
- Verify `.csproj` files are not corrupted
- Check for write permissions
- Review backup files (`.bak`)

### "Type migration failed"
- Check that solution loads correctly
- Verify Roslyn/MSBuild installation
- Review `type-migration-report.md` for details

### "Security update failed"
- Ensure projects are SDK-style (run step 1 first)
- Check for syntax errors in `.cs` files

## Related Documentation

- `README.md` - CSprojConverter documentation
- `TYPE_MIGRATION_README.md` - TypeMigrationTool documentation
- `QUICK_START.md` - Quick start guide
- `FINAL_SUMMARY.md` - Project completion summary

## Version History

- **v2.0** - Unified CLI with 3-step workflow
- **v1.0** - Separate CLI tools (convert, migrate-types)

---

**Ready to migrate your XAF project!** ??
