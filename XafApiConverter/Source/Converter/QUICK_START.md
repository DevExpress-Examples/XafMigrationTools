# Quick Start Guide - CSprojConverter

## Installation

No installation required! The converter is part of the XafApiConverter project.

## Build the Project

```bash
cd XafMigrationTools/XafApiConverter/Source
dotnet build
```

## Basic Usage

### 1. Command Line (Recommended)

```bash
# Convert a single project
dotnet run -- convert MyProject.csproj

# Convert with .NET 10
dotnet run -- convert MyProject.csproj --target-framework net10.0

# Validate only (no changes)
dotnet run -- convert MyProject.csproj --validate

# Get help
dotnet run -- convert --help
```

### 2. Programmatic Usage

```csharp
using XafApiConverter.Converter;

// Simple conversion
var converter = new CSprojConverter();
converter.ConvertProject(@"C:\Projects\MyProject\MyProject.csproj");

// Custom configuration
var config = new ConversionConfig {
    TargetFramework = "net10.0",
    DxPackageVersion = "26.1.6"
};
var converter = new CSprojConverter(config);
converter.ConvertProject(@"C:\Projects\MyProject\MyProject.csproj");
```

### 3. Batch Conversion

```csharp
// Convert all projects in a solution
var projectFiles = Directory.GetFiles(@"C:\Projects\MySolution", "*.csproj", SearchOption.AllDirectories);
var converter = new CSprojConverter();

foreach (var project in projectFiles) {
    converter.ConvertProject(project);
}
```

## What Gets Converted

### Before (Legacy .NET Framework)
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="...">
  <Import Project="..." />
  <PropertyGroup>
    <Configuration>Debug</Configuration>
    <ProjectGuid>{...}</ProjectGuid>
    <TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>
    ...many lines...
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="DevExpress.ExpressApp.v25.1">
      <HintPath>...</HintPath>
    </Reference>
    ...100+ lines of references...
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

### After (SDK-Style .NET)
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>MyProject</RootNamespace>
    <AssemblyName>MyProject</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="DevExpress.ExpressApp" Version="25.1.6" />
    ...35-65 packages...
  </ItemGroup>
</Project>
```

## Features

? **Automatic Detection**
- Windows Forms projects ? `net9.0-windows` + Windows packages
- Web/Blazor projects ? `net9.0` + Blazor packages
- Console/Library projects ? `net9.0` + BASE packages

? **Smart Conversion**
- Removes legacy `<Import>` statements
- Removes DevExpress assembly references (replaced with NuGet)
- Removes System.Web references
- Adds appropriate NuGet packages
- Handles AssemblyInfo.cs correctly
- Manages .resx embedded resources

? **Safety**
- Automatic backup (`.backup` file)
- Validation after conversion
- Idempotent (can run multiple times)
- Detailed error messages

## Validation

After conversion, validate the project:

```bash
dotnet run -- convert MyProject.csproj --validate
```

Output:
```
Validation Results for: MyProject.csproj
------------------------------------------------------------
? SDK-style format verified
? Target framework: net9.0
? No legacy assembly references found
? Found 37 package references
? File size reduced by 78.5% (12453 ? 2678 bytes)
------------------------------------------------------------
Result: PASSED
```

## Next Steps

After conversion:

1. **Build the project**
   ```bash
   dotnet build MyProject.csproj
   ```

2. **Fix any build errors** (rare)
   - Check for custom dependencies
   - Update using statements if needed

3. **Test the application**
   ```bash
   dotnet run --project MyProject.csproj
   ```

4. **Commit changes**
   ```bash
   git add .
   git commit -m "Convert to SDK-style .NET 9 project"
   ```

## Common Options

| Option | Description | Example |
|--------|-------------|---------|
| `--target-framework` | Target .NET version | `--target-framework net10.0` |
| `--dx-version` | DevExpress version | `--dx-version 26.1.6` |
| `--validate` | Validation only | `--validate` |
| `--no-backup` | Skip backup | `--no-backup` |
| `--directory-packages` | Use Directory.Packages.props | `--directory-packages` |

## Troubleshooting

### "Project is already SDK-style"
? Project already converted, no action needed.

### Build errors after conversion
1. Check validation results
2. Verify backup file exists: `MyProject.csproj.backup`
3. Restore if needed: `copy MyProject.csproj.backup MyProject.csproj`

### Missing packages
The converter adds 25-65 packages based on project type. If you need additional packages:
```bash
dotnet add package PackageName
```

## Examples

See `Converter/UsageExamples.cs` for 10 detailed examples including:
- Simple conversion
- Custom configuration
- Batch conversion
- Validation
- Directory.Packages.props
- Package inspection
- Before/after comparison

## Documentation

- **README.md** - Complete documentation
- **IMPLEMENTATION_SUMMARY.md** - Technical details
- **Convert_to_NET.md** - Conversion rules (in UpgradesPrompts folder)
- **Configuration.md** - Configuration reference (in UpgradesPrompts folder)

## Support

For issues or questions:
1. Check the README.md for detailed documentation
2. Review UsageExamples.cs for code samples
3. Check IMPLEMENTATION_SUMMARY.md for technical details

---

**Ready to modernize your .NET projects!** ??
