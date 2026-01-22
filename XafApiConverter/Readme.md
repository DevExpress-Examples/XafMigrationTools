# XAF Legacy API Converter Tool

XAF v25.2 ends support for .NET Framework and other legacy APIs and features. Review the full list of removed APIs in the following knowledge base article: [T1312589 - XAF - Legacy .NET Framework (WinForms and ASP.NET WebForms) APIs, .NET-based API/Modules, and Security System have been removed from distribution](https://supportcenter.devexpress.com/ticket/details/t1312589/xaf-legacy-net-framework-winforms-and-asp-net-webforms-apis-net-based-api-modules-and).

This repository contains the **XafApiConverter** tool that helps you to migrate your XAF application to v25.2. It automates the following routine tasks:

1. Update legacy security APIs.
2. Remove .NET Framework APIs and legacy .NET-based APIs and modules.
3. Convert your Application from .NET Framework to .NET.

Find a full step-by-step migration guide in the following DevExpress documentation:
* [Migrate XAF ASP.NET WebForms to ASP.NET Core Blazor](https://docs.devexpress.com/eXpressAppFramework/405736)

## Prerequisites

Before you migrate your application, perform the following steps:

* Upgrade your project to v25.1. Ensure your project compiles, runs, and works as expected.
* Back up your databases. The conversion changes your database structure.
* If you use our tools to update your project, use a version control system to review all changes.

## How to Use the Converter

1. Download the repository.
2. Open the _XafApiConverter/Source/XafApiConverter.sln_ solution.
3. Build the solution.
4. Once completed, run the executable file from the command line. Use this format:

```console
XafApiConverter.exe <path> <step> [step] [step] [options]
```

`<path>` - the path to the .sln file or project folder.

`<step>` - the migration step to perform. You can run several steps at once. Follow the steps in this order:

* `security-update` - updates legacy security types:
    * Replaces `SecuritySystem*` with `PermissionPolicy*`.
    * Removes obsolete feature toggles.
    * Adds `PermissionPolicyRoleExtensions`.
    * Updates permission state setters.
* `migrate-types` removes .NET Framework APIs and legacy .NET-based APIs and modules. The tool marks or comments out problematic classes.
* `project-conversion` converts projects from .NET Framework to .NET:
    * Converts _.csproj_ to SDK-style format.
    * Updates the target framework to .NET 9/10.
    * Adds NuGet packages (BASE/WINDOWS/BLAZOR_WEB) if needed.
    * Removes legacy assembly references.
    * Validates converted projects.
    * Changes System.Data.SqlClient to Microsoft.Data.SqlClient.
    * Changes DevExpress.ExpressApp.Web.* to DevExpress.ExpressApp.Blazor.*
    * Replaces types (for instance, `WebApplication` with `BlazorApplication`).
    * Processes _.cs_ and _.xafml_ files.

### Options

`-tf`, `--target-framework` - sets target .NET version: `net8.0`, `net9.0`, or `net10.0`. The default is `net9.0`.

`-dx`, `--dx-version` - sets DevExpress version, for instance: `25.2.2`, `26.1.6`. The default is `25.1.6`.

`-o`, `--output <path>` - sets folder to save reports.

`-b`, `--backup` - creates backup files.

`-dp`, `--directory-packages` - adds/uses the _Directory.Packages.props_ file to centrally manage common dependencies across all projects within a solution.

`-c`, `--comment-issues-only` - adds comments to every problematic class without commenting out the code.  
When you do not use this option, the converter comments out problematic classes. The tool only adds a warning to protected classes (such as ModuleBase and BaseObject).

`-m`, `--show-mappings` - displays all type and namespace mappings.

`-h`, `--help` - displays help.

## Examples
Run one step with default options:
```
XafApiConverter.exe MySolution.sln migrate-types
```
Run all steps, specify the .NET version:

```
XafApiConverter.exe MySolution.sln security-update migrate-types project-conversion -tf net10.0
```