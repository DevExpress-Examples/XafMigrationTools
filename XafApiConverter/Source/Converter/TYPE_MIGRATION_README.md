# TypeMigrationTool - Hybrid Approach for XAF Web ? Blazor Migration

## Overview

`TypeMigrationTool` implements a **hybrid approach** for migrating XAF projects from ASP.NET Web Forms to Blazor. It combines:

1. **Automatic replacements** (programmatic - NO LLM needed)
2. **Problem detection** and **report generation** for LLM analysis
3. **Clear separation** between what can be automated and what requires human judgment

## Architecture

```
???????????????????????????????????????????????????????????????
?              TypeMigrationTool Workflow                     ?
???????????????????????????????????????????????????????????????
                           ?
            ???????????????????????????????
            ?                             ?
      ? AUTOMATIC                  ?? LLM REQUIRED
      (Programmatic)                (Manual Analysis)
            ?                             ?
    ??????????????????          ?????????????????????
    ?                ?          ?                   ?
 Namespace         Type      Problem           Dependency
 Replacements   Replacements Detection          Analysis
    ?                ?          ?                   ?
    ?                ?          ?                   ?
    ??????????????????          ?????????????????????
             ?                            ?
             ?                            ?
    type-migration-report.md ?????????????
             ?
             ?
       Share with LLM
             ?
             ?
    LLM provides fixes
```

## Features

### ? Automatic Replacements (No LLM Required)

#### TRANS-006: SqlClient Namespace Migration
```csharp
// Before
using System.Data.SqlClient;

// After
using Microsoft.Data.SqlClient;
```

#### TRANS-007: DevExpress Namespace Migrations
```csharp
// Before
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors;
using DevExpress.ExpressApp.Validation.Web;

// After
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Validation.Blazor;
```

**9 namespace replacements:**
- `DevExpress.ExpressApp.Web` ? `DevExpress.ExpressApp.Blazor`
- `DevExpress.ExpressApp.Web.Editors` ? `DevExpress.ExpressApp.Blazor.Editors`
- `DevExpress.ExpressApp.Web.SystemModule` ? `DevExpress.ExpressApp.Blazor.SystemModule`
- `DevExpress.ExpressApp.Validation.Web` ? `DevExpress.ExpressApp.Validation.Blazor`
- `DevExpress.ExpressApp.Scheduler.Web` ? `DevExpress.ExpressApp.Scheduler.Blazor`
- `DevExpress.ExpressApp.Office.Web` ? `DevExpress.ExpressApp.Office.Blazor`
- `DevExpress.ExpressApp.ReportsV2.Web` ? `DevExpress.ExpressApp.ReportsV2.Blazor`
- And more...

#### TRANS-008: Type Replacements
```csharp
// Before
var app = new WebApplication();
var editor = new ASPxGridListEditor();
ModuleBase module = new SystemAspNetModule();

// After
var app = new BlazorApplication();
var editor = new DxGridListEditor();
ModuleBase module = new SystemBlazorModule();
```

**7 type replacements:**
- `WebApplication` ? `BlazorApplication`
- `ASPxGridListEditor` ? `DxGridListEditor`
- `ASPxLookupPropertyEditor` ? `LookupPropertyEditor`
- `SystemAspNetModule` ? `SystemBlazorModule`
- `ValidationAspNetModule` ? `ValidationBlazorModule`
- `SchedulerAspNetModule` ? `SchedulerBlazorModule`
- And more...

#### XAFML File Processing
```xml
<!-- Before -->
<Class Name="Employee" 
       EditorTypeName="DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor" />

<!-- After -->
<Class Name="Employee" 
       EditorTypeName="DevExpress.ExpressApp.Blazor.Editors.DxGridListEditor" />
```

### ?? Problem Detection (For LLM Analysis)

#### NO_EQUIVALENT Types
```csharp
// These types have NO Blazor equivalent and require LLM analysis:

// 1. System.Web.UI.Page (Web Forms specific)
public class Default : Page { } // ? Will be detected

// 2. TemplateType enum
var template = TemplateType.Horizontal; // ? Will be detected

// 3. PopupShowingEventArgs
void OnPopup(PopupShowingEventArgs e) { } // ? Will be detected

// 4. Maps/ScriptRecorder modules
RequiredModuleTypes.Add(typeof(MapsAspNetModule)); // ? Will be detected
```

**9 NO_EQUIVALENT types detected:**
- `Page` (System.Web.UI)
- `PopupShowingEventArgs`
- `TemplateType` enum
- `MapsAspNetModule`
- `ScriptRecorderAspNetModule`
- `WebMapsPropertyEditor`
- `WebMapsListEditor`
- `ASPxRichTextPropertyEditor`
- `AnalysisControlWeb`

#### Dependency Analysis
```csharp
// If OrderController uses Page:
public class OrderController : Page { }

// Tool will find ALL classes that use OrderController:
// - InvoiceController
// - CustomerController  
// - ReportController

// All will be reported as requiring LLM review
```

## Installation & Setup

### Prerequisites
- .NET 8.0 SDK or higher
- Solution with XAF projects

### Build
```bash
cd XafMigrationTools/XafApiConverter/Source
dotnet build
```

## Usage

### Command Line Interface

#### Basic Usage
```bash
# Run migration with automatic replacements + problem detection
dotnet run -- migrate-types MySolution.sln
```

#### Show All Mappings
```bash
# Display all type and namespace replacements
dotnet run -- migrate-types --show-mappings
```

#### Custom Output Path
```bash
# Save report to custom location
dotnet run -- migrate-types MySolution.sln --output custom-report.md
```

#### Report Only Mode
```bash
# Generate report without modifying files
dotnet run -- migrate-types MySolution.sln --report-only
```

### Programmatic Usage

```csharp
using XafApiConverter.Converter;

// Simple usage
var tool = new TypeMigrationTool("MySolution.sln");
var report = tool.RunMigration();

// Check results
if (report.ProblematicClasses.Any()) {
    Console.WriteLine($"Found {report.ProblematicClasses.Count} problems");
    Console.WriteLine("Review the report and share with LLM");
}

// Save custom report
report.SaveToFile("my-report.md");
```

## Workflow

### Step 1: Run TypeMigrationTool
```bash
dotnet run -- migrate-types MySolution.sln
```

**Output:**
```
Starting Type Migration...

Phase 1: Loading solution...
  Loaded solution: MySolution.sln
  Projects: 5

Phase 2: Applying automatic replacements...
  Processing project: MySolution.Module
  Processing project: MySolution.Module.Blazor
  Processing project: MySolution.Blazor
  ...

Phase 3: Detecting problems for LLM analysis...
  Found 3 problematic classes
  Found 2 XAFML problems

Phase 4: Building project...
  Build analysis skipped (requires integration with upgrade tools)

Phase 5: Generating report...
  Report saved to: D:\MySolution\type-migration-report.md

? Migration analysis complete!

???????????????????????????????????????????????
      Type Migration Report Summary
???????????????????????????????????????????????

? Automatic Changes:
   • Namespaces replaced: 45
   • Types replaced: 18
   • Files processed: 23
   • XAFML files: 3

??  Requires LLM Analysis:
   • Problematic classes: 3
   • Dependent classes: 5
   • XAFML problems: 2
```

### Step 2: Review Generated Report

Report file: `type-migration-report.md`

**Contents:**
- Executive summary with statistics
- List of automatic changes applied
- **Problematic classes requiring LLM analysis**
- Dependency graph
- XAFML problems
- Suggested actions

Example section:
```markdown
## ?? Classes Requiring LLM Analysis

### Class: `OrderController`

**File:** `Controllers\OrderController.cs`

**Problems:**
- ?? CRITICAL: Base class 'Page' has no Blazor equivalent
  - Type: `System.Web.UI.Page`
  - **Action Required:** Comment out entire class

**?? Dependent Classes (will also need to be commented out):**
- `InvoiceController`
- `CustomerController`

**Suggested Actions:**
1. Review class functionality and business logic
2. Determine if functionality is critical or optional
3. Options:
   - Comment out class if functionality is optional
   - Find alternative Blazor implementation if critical
   - Consult XAF Blazor documentation for equivalents
```

### Step 3: Share Report with LLM

```
You: "I've run the type migration tool. Here's the report:
     [paste type-migration-report.md content]
     
     Please analyze the problematic classes and suggest fixes."

LLM: "I've analyzed the report. Here's what I recommend:
     
     1. OrderController (uses Page):
        - This is a Web Forms controller
        - Recommend commenting out entire class
        - Alternative: Rewrite as Blazor ComponentBase
        
     2. InvoiceController (depends on OrderController):
        - Must also be commented out
        - Or rewritten to not depend on OrderController
     
     [LLM provides detailed analysis and code fixes]"
```

### Step 4: Apply LLM Fixes

LLM will provide specific code changes to:
- Comment out problematic classes
- Handle dependency cascade
- Fix build errors
- Suggest alternative implementations

### Step 5: Build and Test

```bash
dotnet build MySolution.sln
dotnet test
```

## Generated Report Structure

### Executive Summary
```markdown
| Metric | Value |
|--------|-------|
| Files Processed | 23 |
| Namespaces Replaced | 45 |
| Types Replaced | 18 |
| Problematic Classes | 3 |
| Build Status | ? Failed |
```

### Automatic Changes
- List of namespace replacements
- List of type replacements
- Files modified

### Problematic Classes
For each class:
- File location
- Problem description (what NO_EQUIVALENT type is used)
- Severity (Critical/High/Medium/Low)
- Dependent classes
- Suggested actions

### XAFML Problems
For each XAFML file:
- File location
- Problematic types found
- Recommended actions

### Build Errors (if available)
- Fixable errors (can be fixed automatically)
- Unfixable errors (require manual intervention)
- Suggested fixes

### Next Steps
Concrete action items for LLM to work on

## Type Replacement Map

### Namespace Replacements

| Old Namespace | New Namespace | Status |
|---------------|---------------|--------|
| `System.Data.SqlClient` | `Microsoft.Data.SqlClient` | ? Has Equivalent |
| `DevExpress.ExpressApp.Web` | `DevExpress.ExpressApp.Blazor` | ? Has Equivalent |
| `DevExpress.ExpressApp.Web.Editors` | `DevExpress.ExpressApp.Blazor.Editors` | ? Has Equivalent |
| `DevExpress.ExpressApp.Maps.Web` | - | ? NO Equivalent |
| `DevExpress.ExpressApp.ScriptRecorder.Web` | - | ? NO Equivalent |

### Type Replacements

| Old Type | New Type | Status |
|----------|----------|--------|
| `WebApplication` | `BlazorApplication` | ? Has Equivalent |
| `ASPxGridListEditor` | `DxGridListEditor` | ? Has Equivalent |
| `SystemAspNetModule` | `SystemBlazorModule` | ? Has Equivalent |
| `Page` | - | ? NO Equivalent (comment out class) |
| `TemplateType` | - | ? NO Equivalent (comment out class) |
| `MapsAspNetModule` | - | ? NO Equivalent |

## Configuration

### TypeReplacementMap.cs

All mappings are defined in `TypeReplacementMap.cs`:

```csharp
// Add custom namespace replacement
NamespaceReplacements.Add("MyOldNamespace", new NamespaceReplacement(
    "MyOldNamespace",
    "MyNewNamespace",
    "Description",
    new[] { ".cs", ".xafml" }
));

// Add custom type replacement
TypeReplacements.Add("OldType", new TypeReplacement(
    "OldType",
    "NewType",
    "OldNamespace",
    "NewNamespace",
    "Description"
));

// Mark type as NO_EQUIVALENT
NoEquivalentTypes.Add("ProblematicType", new TypeReplacement(
    "ProblematicType",
    null,
    "OldNamespace",
    null,
    "Has no Blazor equivalent",
    commentOutEntireClass: true
));
```

## Limitations

### Current Limitations

1. **Build Error Analysis**
   - Currently placeholder
   - Requires integration with upgrade tools
   - LLM can integrate when using

2. **Automatic Commenting**
   - Tool detects problems but doesn't auto-comment
   - Commenting requires semantic analysis
   - LLM handles commenting decisions

3. **Dependency Cascade**
   - Tool detects dependencies
   - But doesn't auto-comment entire cascade
   - LLM makes final decisions

### Why These Are NOT Bugs

These are **intentional design decisions** for the hybrid approach:
- ? Tool does what it can reliably automate
- ? Tool detects what requires human judgment
- ? LLM handles complex semantic analysis

## Examples

### Example 1: Simple Migration

**Before:**
```csharp
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors.ASPx;

public class MyModule : ModuleBase {
    public MyModule() {
        RequiredModuleTypes.Add(typeof(SystemAspNetModule));
    }
}
```

**After (automatic):**
```csharp
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Editors;

public class MyModule : ModuleBase {
    public MyModule() {
        RequiredModuleTypes.Add(typeof(SystemBlazorModule));
    }
}
```

### Example 2: Problematic Class (LLM Required)

**Before:**
```csharp
using System.Web.UI;

public class Default : Page {
    protected void Page_Load(object sender, EventArgs e) {
        // logic
    }
}
```

**Tool Detection:**
```
?? Class 'Default' uses System.Web.UI.Page
   - Page has no Blazor equivalent
   - Requires commenting out entire class
   - Used by: (no dependencies)
```

**After (LLM applies fix):**
```csharp
// NOTE: System.Web.UI.Page is Web Forms specific, no Blazor equivalent
// TODO: Application behavior verification required and new solution if necessary
/*
using System.Web.UI;

public class Default : Page {
    protected void Page_Load(object sender, EventArgs e) {
        // logic
    }
}
*/
```

## Troubleshooting

### "No namespaces replaced"
- Check that files use `using` statements
- Verify namespace spelling
- Ensure files are .cs or .xafml

### "Build analysis skipped"
- Normal in standalone mode
- Integrate with upgrade tools for build analysis
- Or run manual build after migration

### "Too many problematic classes"
- This is expected for large projects
- Review report carefully
- Work with LLM to prioritize critical classes

## Integration with LLM

### Best Practices

1. **Generate Report First**
   ```bash
   dotnet run -- migrate-types MySolution.sln
   ```

2. **Review Report**
   - Check automatic changes
   - Identify problematic classes
   - Note dependency relationships

3. **Share with LLM**
   - Paste entire report OR specific sections
   - Ask for specific analysis
   - Request concrete fixes

4. **Apply Fixes Iteratively**
   - Don't try to fix everything at once
   - Fix one problematic class at a time
   - Build and test after each fix

5. **Iterate**
   - Re-run tool if needed
   - Update report
   - Continue until build succeeds

## Related Documentation

- `UpdateTypes.md` - Complete type migration rules (TRANS-006 to TRANS-010)
- `Convert_to_NET.md` - Project conversion rules (TRANS-001 to TRANS-005)
- `Configuration.md` - Version and configuration settings
- `Safety_Rule.md` - Safety guidelines

## Version History

- **v1.0** (Current) - Initial hybrid implementation
  - Automatic replacements (TRANS-006, 007, 008)
  - Problem detection (TRANS-009)
  - Report generation
  - CLI interface

## Contributing

To add new type replacements:

1. Edit `TypeReplacementMap.cs`
2. Add to appropriate dictionary
3. Rebuild
4. Test with sample project

## License

Part of XAF Migration Tools project.

---

**Ready to modernize your XAF project to Blazor!** ??
