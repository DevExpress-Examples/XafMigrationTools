# XAF Converter for Legacy API in v25.2

XAF v25.2 ends support for .NET Framework and other legacy APIs and features. Review the full list of removed APIs in the following knowledge base article: [T1312589 - XAF - Legacy .NET Framework (WinForms and ASP.NET WebForms) APIs, .NET-based API/Modules, and Security System have been removed from distribution](https://supportcenter.devexpress.com/ticket/details/t1312589/xaf-legacy-net-framework-winforms-and-asp-net-webforms-apis-net-based-api-modules-and).

This repository contains the **XafApiConverter** tool that helps you migrate your XAF application to v25.2. It automates the following routine tasks:

1. Updates legacy security APIs.
2. Removes .NET Framework APIs, and legacy .NET-based APIs and modules.
3. Converts your application from .NET Framework to .NET.

Find a full step-by-step migration guide in the following DevExpress help topic:
* [Migrate XAF ASP.NET WebForms to ASP.NET Core Blazor](https://docs.devexpress.com/eXpressAppFramework/405736)

## How to Use the Converter

> [!IMPORTANT]  
> **XafApiConverter** analyzes semantic trees that depend on the DevExpress version. To recognize types correctly **the conversion should be performed on a v25.1 application**.


1. Download the repository.
2. Open the _XafApiConverter/Source/XafApiConverter.sln_ solution.
3. Build the solution.
4. Once completed, run the executable file from the command line. Use this format:

```console
XafApiConverter.exe <path> <step> [step] [step] [options]
```

`<path>` - the path to the .sln file or project folder.

`<step>` - the migration step to perform. You can run several steps simultaneously in the following order:

* `security-update` step updates legacy security types:
    * Replaces `SecuritySystem*` with `PermissionPolicy*`.
    * Removes obsolete feature toggles.
    * Adds `PermissionPolicyRoleExtensions`.
    * Updates permission state setters.
* `migrate-types` step removes .NET Framework APIs, and legacy .NET-based APIs and modules. The tool marks or comments out problematic classes.
* `project-conversion` step converts projects from .NET Framework to .NET:
    * Converts _.csproj_ to SDK-style format.
    * Updates the target framework to .NET 9/10.
    * Adds NuGet packages (base, Microsoft, Blazor) if needed.
    * Removes legacy assembly references.
    * Validates converted projects.
    * Changes `System.Data.SqlClient` to `Microsoft.Data.SqlClient`
    * Changes `DevExpress.ExpressApp.Web.*` to `DevExpress.ExpressApp.Blazor.*`
    * Replaces types (for instance, `WebApplication` with `BlazorApplication`).
    * Processes _.cs_ and _.xafml_ files.

### Options

`-tf`, `--target-framework` - sets target .NET version: `net8.0`, `net9.0` (default), or `net10.0`.

`-dx`, `--dx-version` - sets version of added/updated DevExpress packages, for instance: `25.2.2`, `26.1.6`. The default is `25.1.6`.

`-o`, `--output <path>` - sets folder to save reports.

`-b`, `--backup` - creates backup files.

`-dp`, `--directory-packages` - adds/uses the _Directory.Packages.props_ file to manage common dependencies across all projects within a solution.

`-c`, `--comment-issues-only` - adds comments to every problematic class without commenting out the code.  
When you do not use this option, the converter comments out problematic classes. The tool only adds a warning to protected classes (such as `ModuleBase` and `BaseObject`).

`-m`, `--show-mappings` - displays all type and namespace mappings.

`-h`, `--help` - displays complete list of available options.

## Command Usage Examples

### Migrate XAF Application from Web Forms to ASP.NET Core Blazor

```
XafApiConverter.exe MySolution.sln security-update migrate-types project-conversion -tf net10.0 -b
```

> [!NOTE]  
> Migrating from Web Forms to ASP.NET Core Blazor involves number of manual steps, detailed in the topic: [Migrate XAF ASP.NET WebForms to ASP.NET Core Blazor](https://docs.devexpress.com/eXpressAppFramework/405736). We recommend that you follow the steps in order and use the **XafApiConverter** tool as described.

### Update legacy XAF Security System APIs

```
XafApiConverter.exe MySolution.sln security-update -b
```

### Remove legacy .NET-based APIs and modules from XAF WinForms/Blazor Application

```
XafApiConverter.exe MySolution.sln migrate-types -b
```

### Migrate XAF WinForms Application from .NET Framework to .NET

```
XafApiConverter.exe MySolution.sln security-update migrate-types project-conversion -b
```

### Remove legacy .NET Framework APIs from XAF WinForms/Web Forms Application

```
XafApiConverter.exe MySolution.sln migrate-types -b
```