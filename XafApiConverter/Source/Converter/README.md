# CSprojConverter - .NET Framework to .NET Core/5+ Project Converter

## Overview

`CSprojConverter` is a programmatic tool for converting legacy .NET Framework `.csproj` files to modern SDK-style format for .NET Core, .NET 5, .NET 6, .NET 8, .NET 9, and beyond. It specifically targets DevExpress XAF (eXpressApp Framework) projects but can be adapted for other project types.

## Features

? **Automatic Project Type Detection**
- Detects Windows Forms projects (WinForms)
- Detects Web/Blazor projects
- Detects console and library projects

? **SDK-Style Conversion**
- Converts legacy verbose `.csproj` to concise SDK-style format
- Removes unnecessary `<Import>` statements
- Simplifies property groups
- Reduces file size by 50-80%

? **Smart Package Management**
- Automatically adds appropriate NuGet packages based on project type
- BASE packages (always included)
- WINDOWS packages (for WinForms projects)
- BLAZOR_WEB packages (for web projects)
- Handles package deduplication with proper priority rules

? **Legacy Reference Cleanup**
- Removes DevExpress assembly references (replaced with NuGet packages)
- Removes System.Web assembly references
- Migrates `.Web` packages to `.Blazor`

? **Resource Handling**
- Properly handles embedded resources
- Removes duplicate `.resx` declarations (SDK auto-includes them)
- Preserves custom resources with special metadata

? **AssemblyInfo.cs Management**
- Detects manual `AssemblyInfo.cs` files
- Adds `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` when needed
- Prevents duplicate attribute errors (CS0579)

? **Validation**
- Post-conversion validation
- Checks SDK-style format compliance
- Verifies target framework
- Detects common issues
- Provides colored console output

## Architecture

### Core Classes

```
Converter/
??? CSprojConverter.cs          # Main conversion logic
??? ConversionConfig.cs         # Configuration (target frameworks, versions)
??? PackageManager.cs           # NuGet package management
??? ProjectValidator.cs         # Post-conversion validation
??? ConversionCli.cs           # Command-line interface
```

### Key Components

#### 1. CSprojConverter
Main class that orchestrates the conversion process:
- Reads and parses legacy `.csproj` files
- Analyzes project type
- Creates new SDK-style project structure
- Saves converted project with backup

#### 2. ConversionConfig
Centralized configuration for:
- Target frameworks (net8.0, net9.0, net10.0)
- DevExpress versions
- Package versions
- Directory.Packages.props support

#### 3. PackageManager
Manages NuGet packages:
- Provides BASE, WINDOWS, and BLAZOR_WEB package sets
- Implements deduplication rules (Priority: WINDOWS > BLAZOR_WEB > BASE)
- Handles Web-to-Blazor package migration

#### 4. ProjectValidator
Validates converted projects:
- Checks SDK-style compliance
- Verifies target framework
- Detects legacy references
- Validates package references
- Checks embedded resources

#### 5. ConversionCli
Command-line interface for:
- Interactive conversion
- Validation-only mode
- Custom configuration
- Help documentation

## Usage

### Programmatic Usage

```csharp
using XafApiConverter.Converter;
using Microsoft.CodeAnalysis;

// Using Roslyn Project
Project project = // ... get Roslyn project
CSprojConverter.Convert(project);

// Using file path with default settings
var converter = new CSprojConverter();
converter.ConvertProject("MyProject.csproj");

// Using custom configuration
var config = new ConversionConfig {
    TargetFramework = "net10.0",
    TargetFrameworkWindows = "net10.0-windows",
    DxPackageVersion = "26.1.6",
    DxAssemblyVersion = "v26.1"
};
var converter = new CSprojConverter(config);
converter.ConvertProject("MyProject.csproj");
```

### Command-Line Usage

```bash
# Convert with default settings
XafApiConverter convert MyProject.csproj

# Convert to .NET 10
XafApiConverter convert MyProject.csproj --target-framework net10.0

# Convert with specific DevExpress version
XafApiConverter convert MyProject.csproj --dx-version 26.1.6

# Validate without converting
XafApiConverter convert MyProject.csproj --validate

# Use Directory.Packages.props
XafApiConverter convert MyProject.csproj --directory-packages

# Skip backup creation
XafApiConverter convert MyProject.csproj --no-backup
```

### Validation

```csharp
using XafApiConverter.Converter;

// Validate a converted project
var result = ProjectValidator.Validate("MyProject.csproj");
result.PrintResults();

if (result.IsValid) {
    Console.WriteLine("Project is valid!");
}
```

## Conversion Rules

Based on `Convert_to_NET.md` specifications:

### TRANS-001: SDK-Style Conversion
- Sets first line to `<Project Sdk="Microsoft.NET.Sdk">`
- Removes all `<Import>` statements
- Removes verbose `<PropertyGroup>` elements
- Keeps only essential properties

### TRANS-002: Target Framework Selection
- Detects Windows projects ? uses `net9.0-windows`
- Detects non-Windows projects ? uses `net9.0`
- Based on DevExpress.ExpressApp.Win reference detection

### TRANS-003: Windows Desktop Properties
- Adds `<UseWindowsForms>true</UseWindowsForms>`
- Adds `<ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>`
- Only for Windows projects

### TRANS-004: Assembly Reference Removal
- Removes all `DevExpress.*` assembly references
- Removes all `System.Web.*` assembly references
- Replaces with NuGet packages

### TRANS-005: NuGet Package Addition
- Adds BASE packages (25 packages) - always
- Adds WINDOWS packages (12 packages) - for Windows projects
- Adds BLAZOR_WEB packages (28 packages) - for web projects

### TRANS-006: AssemblyInfo.cs Handling
- Detects `Properties\AssemblyInfo.cs`
- Adds `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` if manual file exists
- Prevents duplicate attribute errors

### TRANS-007: EmbeddedResource .resx Handling
- Removes `.resx` files with `<DependentUpon>` (auto-included by SDK)
- Keeps standalone `.resx` files
- Keeps non-`.resx` embedded resources

### TRANS-008: Web to Blazor Package Migration
- Converts `DevExpress.*.Web` ? `DevExpress.*.Blazor`
- Example: `DevExpress.ExpressApp.Office.Web` ? `DevExpress.ExpressApp.Office.Blazor`

## Package Sets

### BASE (All Projects) - 25 packages
```
DevExpress packages (11):
- DevExpress.ExpressApp.CodeAnalysis
- DevExpress.ExpressApp.CloneObject
- DevExpress.ExpressApp.ConditionalAppearance
- ... and 8 more

Microsoft packages (7):
- Microsoft.CodeAnalysis.CSharp
- Microsoft.Extensions.Configuration.Abstractions
- ... and 5 more

Azure packages (2):
- Azure.Identity
- Microsoft.Identity.Client

System packages (5):
- System.Configuration.ConfigurationManager
- System.Text.Json
- ... and 3 more
```

### WINDOWS (Windows Projects) - 12 packages
```
All DevExpress packages:
- DevExpress.ExpressApp.Security.Xpo.Extensions.Win
- DevExpress.ExpressApp.Win.Design
- ... and 10 more
```

### BLAZOR_WEB (Web Projects) - 28 packages
```
DevExtreme (1):
- DevExtreme.AspNet.Data

Microsoft ASP.NET (2):
- Microsoft.AspNetCore.OData
- Microsoft.Extensions.DependencyModel

Swagger (2):
- Swashbuckle.AspNetCore
- Swashbuckle.AspNetCore.Annotations

System (4):
- System.CodeDom
- System.Drawing.Common
- ... and 2 more

DevExpress Blazor (10):
- DevExpress.ExpressApp.Notifications.Blazor
- DevExpress.ExpressApp.ReportsV2.Blazor
- ... and 8 more
```

## Configuration Presets

### Default (.NET 9.0 + DevExpress 25.1)
```csharp
var config = ConversionConfig.Default;
```

### .NET 10.0
```csharp
var config = ConversionConfig.ForNet10();
```

### DevExpress 26.1
```csharp
var config = ConversionConfig.ForDevExpress26();
```

### Custom Configuration
```csharp
var config = new ConversionConfig {
    TargetFramework = "net10.0",
    TargetFrameworkWindows = "net10.0-windows",
    DxPackageVersion = "26.1.6",
    DxAssemblyVersion = "v26.1",
    UseDirectoryPackages = true
};
```

## Output Example

### Before Conversion (Legacy .csproj - ~400 lines)
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\..." />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{12345678-1234-1234-1234-123456789012}</ProjectGuid>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>
    ... 50 more lines ...
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="DevExpress.ExpressApp.v25.1">
      <HintPath>..\packages\DevExpress.ExpressApp.25.1.6\lib\net48\...</HintPath>
    </Reference>
    ... 100 more references ...
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
  ... 200 more lines ...
</Project>
```

### After Conversion (SDK-style - ~80 lines)
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <RootNamespace>MyProject</RootNamespace>
    <AssemblyName>MyProject</AssemblyName>
    <UseWindowsForms>true</UseWindowsForms>
    <ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Azure.Identity" Version="1.17.1" />
    <PackageReference Include="DevExpress.ExpressApp.CodeAnalysis" Version="25.1.6" />
    ... 35 more packages ...
  </ItemGroup>
</Project>
```

### Conversion Output
```
Converting project: D:\Projects\MyProject\MyProject.csproj
? Successfully converted: D:\Projects\MyProject\MyProject.csproj
  Backup saved to: D:\Projects\MyProject\MyProject.csproj.backup
  Project type: Windows

Validation Results for: MyProject.csproj
------------------------------------------------------------
? SDK-style format verified
? Target framework: net9.0-windows
? No legacy assembly references found
? Found 37 package references
? File size reduced by 78.5% (12453 ? 2678 bytes)
------------------------------------------------------------
Result: PASSED
```

## Testing

### Manual Testing
1. Create a test legacy .csproj file
2. Run conversion: `converter.ConvertProject("test.csproj")`
3. Check backup file: `test.csproj.backup`
4. Validate: `ProjectValidator.Validate("test.csproj")`
5. Build project to verify compilation

### Validation Checks
- ? SDK-style format (`<Project Sdk="...">`)
- ? Correct target framework
- ? No `<Import>` statements
- ? No legacy assembly references
- ? All required packages present
- ? No duplicate packages
- ? Correct DevExpress package versions
- ? No `.Web` packages (should be `.Blazor`)
- ? File size reduction (50-80%)

## Safety Features

1. **Automatic Backup**: Creates `.backup` file before conversion
2. **Validation**: Post-conversion validation with detailed report
3. **Error Handling**: Graceful error handling with informative messages
4. **Idempotency**: Can detect already-converted projects
5. **Dry Run**: Validation-only mode (`--validate`)

## Extensibility

### Add Custom Package Set
```csharp
// In PackageManager.cs
private void AddCustomPackages(List<PackageReference> packages) {
    packages.AddRange(new[] {
        new PackageReference("CustomPackage", "1.0.0", PackageSet.Custom)
    });
}
```

### Custom Validation Rules
```csharp
// In ProjectValidator.cs
private static void ValidateCustomRule(XDocument doc, ValidationResult result) {
    // Add custom validation logic
}
```

### Custom Configuration
```csharp
var config = new ConversionConfig {
    // Override any configuration values
    PackageVersions = {
        ["VER_CUSTOM"] = "1.0.0"
    }
};
```

## Troubleshooting

### Project Already SDK-Style
```
Project is already SDK-style: MyProject.csproj
```
**Solution**: Project is already converted, no action needed.

### Validation Warnings
```
? DevExpress packages with incorrect version: DevExpress.ExpressApp (26.1.6)
```
**Solution**: Check if mixed versions are intentional, or re-run with correct config.

### Build Errors After Conversion
1. Check validation results
2. Verify backup file exists
3. Check for custom dependencies not in package sets
4. Manually add missing packages

## Related Files

- `Convert_to_NET.md` - Full conversion rules specification
- `Configuration.md` - Configuration variables reference
- `Safety_Rule.md` - Safety and verification guidelines

## License

This tool is part of the XAF Migration Tools project.

## Contributing

Contributions are welcome! Please ensure:
- Code follows existing patterns
- Add appropriate validation rules
- Update this README for new features
- Test with various project types

## Version History

- **2.2** - Current version with full feature set
- **2.1** - Added validation and CLI
- **2.0** - Refactored with PackageManager and ConversionConfig
- **1.0** - Initial implementation
