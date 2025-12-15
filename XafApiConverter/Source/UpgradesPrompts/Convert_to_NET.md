# .NET Project Conversion Rules (LLM Optimized)

> **Version:** 2.2 | **Priority:** CRITICAL | **Apply with:** Safety_Rule.md, Configuration.md

---

## PROJECT FILE TRANSFORMATIONS

### TRANS-001: SDK-Style Conversion
```yaml
rule_id: TRANS-001
applies_to: [.csproj, .vbproj, .vcxproj]
priority: CRITICAL

conversion:
  from: legacy_csproj_format
  to: sdk_style_format
  
changes:
  - set_first_line: '<Project Sdk="Microsoft.NET.Sdk">'
  - remove: all_Import_statements
  - remove: verbose_PropertyGroup_elements
  - keep: [TargetFramework, RootNamespace, AssemblyName]

safety: SAFE-001 (unload-edit-reload), SAFE-002 (verify)
```

**Before ? After:**
```xml
<!-- BEFORE (Legacy) -->
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\..." />
  <PropertyGroup>
    <Configuration>...</Configuration>
    <ProjectGuid>{GUID}</ProjectGuid>
    <TargetFrameworkVersion>{{SOURCE_FRAMEWORK}}</TargetFrameworkVersion>
    ...many verbose elements...
  </PropertyGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>

<!-- AFTER (SDK-Style) -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>{{TARGET_FRAMEWORK}}</TargetFramework>
    <RootNamespace>{{MODULE_PROJECT}}</RootNamespace>
    <AssemblyName>{{MODULE_PROJECT}}</AssemblyName>
  </PropertyGroup>
</Project>
```

**Verification:**
```yaml
checks:
  - First line: '<Project Sdk="Microsoft.NET.Sdk">'
  - No <Import> statements
  - File size < 50% of original
```

---

### TRANS-002: Target Framework Selection
```yaml
rule_id: TRANS-002
applies_to: all_projects
priority: CRITICAL

decision_tree:
  if: "project references DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}"
  then:
    target_framework: "{{TARGET_FRAMEWORK_WINDOWS}}"
    apply: TRANS-003 (Windows properties)
  else:
    target_framework: "{{TARGET_FRAMEWORK}}"
    remove: Windows properties (if present)

safety: SAFE-001 (unload-edit-reload), SAFE-002 (verify)
```

**Detection Logic:**
```pseudocode
function determine_target_framework(project_path):
    dependencies = upgrade_get_project_dependencies(solution_path, project_path)
    
    has_win_assembly = dependencies.packages.any(
        pkg => pkg.name == "DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}"
    )
    
    return has_win_assembly ? "{{TARGET_FRAMEWORK_WINDOWS}}" : "{{TARGET_FRAMEWORK}}"
```

**Results:**
```xml
<!-- Windows Project -->
<PropertyGroup>
  <TargetFramework>{{TARGET_FRAMEWORK_WINDOWS}}</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
  <ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>
</PropertyGroup>

<!-- Non-Windows Project -->
<PropertyGroup>
  <TargetFramework>{{TARGET_FRAMEWORK}}</TargetFramework>
  <!-- NO Windows properties -->
</PropertyGroup>
```

**Verification:**
```yaml
checks:
  - TargetFramework = "{{TARGET_FRAMEWORK}}" OR "{{TARGET_FRAMEWORK_WINDOWS}}"
  - If Windows TFM ? Windows properties present
  - If non-Windows TFM ? Windows properties absent
```

---

### TRANS-003: Windows Desktop Properties
```yaml
rule_id: TRANS-003
applies_to: all_projects
priority: MANDATORY

rules:
  for_windows_projects:
    condition: 'TargetFramework == "{{TARGET_FRAMEWORK_WINDOWS}}"'
    add:
      - UseWindowsForms: true
      - ImportWindowsDesktopTargets: true
      
  for_non_windows_projects:
    condition: 'TargetFramework == "{{TARGET_FRAMEWORK}}"'
    remove:
      - UseWindowsForms
      - ImportWindowsDesktopTargets

safety: SAFE-001 (unload-edit-reload), SAFE-002 (verify)
```

**Execution:**
```pseudocode
function apply_windows_properties(project_path, target_framework):
    upgrade_unload_project(project_path)
    content = upgrade_read_file(project_path)
    
    if target_framework == "{{TARGET_FRAMEWORK_WINDOWS}}":
        content = ensure_property("UseWindowsForms", "true")
        content = ensure_property("ImportWindowsDesktopTargets", "true")
    else:
        content = remove_property("UseWindowsForms")
        content = remove_property("ImportWindowsDesktopTargets")
    
    edit_file(project_path, content)
    upgrade_reload_project(project_path)
    verify_windows_properties(project_path, target_framework)
```

---

## DEPENDENCY TRANSFORMATIONS

### TRANS-004: Assembly Reference Removal
```yaml
rule_id: TRANS-004
applies_to: all_projects
priority: CRITICAL

removal_rules:
  devexpress_assemblies:
    pattern: '<Reference Include="DevExpress'
    action: remove_all
    expected_count_after: 0
    
  system_web:
    pattern: '<Reference Include="System.Web'
    action: remove_all
    expected_count_after: 0

safety: SAFE-001 (unload-edit-reload), SAFE-002 (verify), SAFE-008 (idempotent)
```

**Assemblies to Remove:**
```xml
<!-- Remove ALL DevExpress assemblies -->
<Reference Include="DevExpress.ExpressApp.{{DX_ASSEMBLY_VERSION}}" />
<Reference Include="DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}" />
<Reference Include="DevExpress.ExpressApp.Xpo.{{DX_ASSEMBLY_VERSION}}" />
<!-- ... and ALL other DevExpress.* -->

<!-- Remove ALL System.Web assemblies -->
<Reference Include="System.Web" />
<Reference Include="System.Web.ApplicationServices" />
<Reference Include="System.Web.Extensions" />
<!-- ... and ALL other System.Web.* -->
```

**Execution:**
```pseudocode
function remove_assembly_references(project_path):
    upgrade_unload_project(project_path)
    content = upgrade_read_file(project_path)
    
    content = remove_all_matching('<Reference Include="DevExpress')
    content = remove_all_matching('<Reference Include="System.Web')
    
    edit_file(project_path, content)
    upgrade_reload_project(project_path)
    verify_assembly_removal(project_path)
```

**Verification:**
```yaml
checks:
  - No '<Reference Include="DevExpress' found
  - No '<Reference Include="System.Web' found
  - No empty <ItemGroup> elements
```

---

### TRANS-005: NuGet Package Addition
```yaml
rule_id: TRANS-005
applies_to: all_projects
priority: CRITICAL

application_rules:
  1: determine_applicable_package_sets
  2: load_packages_from_appendix_a
  3: merge_and_deduplicate (Appendix B)
  4: add_to_project_file

package_format: '<PackageReference Include="<Name>" Version="<Version>" />'

safety: SAFE-001 (unload-edit-reload), SAFE-002 (verify), SAFE-008 (idempotent)
```

**Package Set Selection:**
```pseudocode
function determine_package_sets(project_path):
    applicable_sets = ["BASE"]  // Always applies
    
    dependencies = upgrade_get_project_dependencies(solution_path, project_path)
    
    // Check for Windows
    if dependencies.has("DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}"):
        applicable_sets.append("WINDOWS")
    
    // Check for Web
    if file_exists("Global.asax.cs") OR project_path.contains(".Web"):
        applicable_sets.append("BLAZOR_WEB")
    
    return applicable_sets
```

**Execution:**
```pseudocode
function add_nuget_packages(project_path, package_sets):
    all_packages = load_package_sets(package_sets)  // From Appendix A
    unique_packages = deduplicate(all_packages)      // Appendix B rules
    
    upgrade_unload_project(project_path)
    content = upgrade_read_file(project_path)
    
    for package in unique_packages:
        if not package_exists(content, package.name):  // SAFE-008
            content = add_package_reference(package.name, package.version)
    
    edit_file(project_path, content)
    upgrade_reload_project(project_path)
    verify_packages(project_path, unique_packages)
```

**Verification:**
```yaml
checks:
  - PackageReference count >= 25 (BASE minimum)
  - No duplicate package names
  - All DevExpress packages have version {{DX_PACKAGE_VERSION}}
```

---

### TRANS-006: AssemblyInfo.cs Handling
```yaml
rule_id: TRANS-006
applies_to: all_projects
priority: CRITICAL

problem: "SDK-style auto-generates AssemblyInfo.cs in obj/. Manual Properties\AssemblyInfo.cs causes duplicate attribute errors (CS0579)"

detection:
  check_paths: ["Properties\AssemblyInfo.cs", "AssemblyInfo.cs"]
  exclude_paths: ["obj\**", "bin\**"]

action:
  if_manual_exists: add '<GenerateAssemblyInfo>false</GenerateAssemblyInfo>' to <PropertyGroup>
  if_not_exists: use default (auto-generation enabled)

safety: SAFE-001, SAFE-002, SAFE-008
```

**Execution:**
```pseudocode
function handle_assemblyinfo(project_path):
    project_dir = get_directory(project_path)
    manual_exists = file_exists(join(project_dir, "Properties", "AssemblyInfo.cs")) OR 
                    file_exists(join(project_dir, "AssemblyInfo.cs"))
    
    if manual_exists:
        content = upgrade_read_file(project_path)
        if not contains(content, "<GenerateAssemblyInfo>"):
            content = add_to_first_propertygroup(content, "<GenerateAssemblyInfo>false</GenerateAssemblyInfo>")
            edit_file(project_path, content)
```

---

### TRANS-007: EmbeddedResource .resx Handling
```yaml
rule_id: TRANS-007
applies_to: all_projects
priority: CRITICAL

problem: "SDK-style auto-includes *.resx files as EmbeddedResource. Explicit declarations cause duplicate errors (NETSDK1022)"

auto_included: "*.resx files with matching .cs files (e.g., Form1.resx + Form1.cs)"

action:
  remove_explicit_declarations:
    - "<EmbeddedResource Include='**\*.resx'><DependentUpon>*.cs</DependentUpon></EmbeddedResource>"
    - All .resx with <DependentUpon> tags
  
  keep_explicit_declarations:
    - .resx WITHOUT matching .cs files (standalone resources)
    - Non-.resx EmbeddedResource (xml, pdf, svg, etc)
    - Resources requiring special metadata

safety: SAFE-001, SAFE-002, SAFE-008
```

**Execution:**
```pseudocode
function handle_resx_files(project_path):
    content = upgrade_read_file(project_path)
    
    // Remove all .resx files with <DependentUpon>
    content = remove_all_matching('<EmbeddedResource Include="*\.resx">.*<DependentUpon>.*</DependentUpon>.*</EmbeddedResource>')
    
    // Keep standalone .resx (no <DependentUpon>)
    // Keep non-.resx EmbeddedResource (.xml, .pdf, .svg, etc)
    
    edit_file(project_path, content)
```

---

### TRANS-008: Web to Blazor Package Migration
```yaml
rule_id: TRANS-008
applies_to: all_projects
priority: CRITICAL

pattern_replacement:
  match: '<PackageReference Include="DevExpress.*.Web" Version="*" />'
  action: replace ".Web" with ".Blazor"
  
examples:
  - from: DevExpress.ExpressApp.Office.Web
    to: DevExpress.ExpressApp.Office.Blazor
  - from: DevExpress.ExpressApp.Validation.Web
    to: DevExpress.ExpressApp.Validation.Blazor

safety: SAFE-001, SAFE-002, SAFE-008
```

**Execution:**
```pseudocode
function migrate_web_to_blazor_packages(project_path):
    upgrade_unload_project(project_path)
    content = upgrade_read_file(project_path)
    
    // Replace all DevExpress.*.Web with DevExpress.*.Blazor
    content = replace_all_matching(
        pattern: '<PackageReference Include="(DevExpress\.[^"]*?)\.Web"',
        replacement: '<PackageReference Include="$1.Blazor"'
    )
    
    edit_file(project_path, content)
    upgrade_reload_project(project_path)
    verify_no_web_packages(project_path)
```

**Verification:**
```yaml
checks:
  - No PackageReference contains ".Web"
  - All DevExpress packages end with ".Blazor" or other valid suffix
```

---

## APPENDIX A: PACKAGE SETS

### BASE (ALL projects)
```yaml
count: 25
packages:
  DevExpress (v{{DX_PACKAGE_VERSION}}):
    - DevExpress.ExpressApp.CodeAnalysis
    - DevExpress.ExpressApp.CloneObject
    - DevExpress.ExpressApp.ConditionalAppearance
    - DevExpress.ExpressApp.TreeListEditors
    - DevExpress.ExpressApp.Office
    - DevExpress.ExpressApp.PivotChart
    - DevExpress.ExpressApp.ReportsV2
    - DevExpress.ExpressApp.Security
    - DevExpress.ExpressApp.Validation
    - DevExpress.ExpressApp.ViewVariantsModule
    - DevExpress.Persistent.BaseImpl.Xpo
  
  Microsoft:
    - Microsoft.CodeAnalysis.CSharp ({{VER_MS_CODEANALYSIS}})
    - Microsoft.Extensions.Configuration.Abstractions ({{VER_MS_EXTENSIONS}})
    - Microsoft.Extensions.DependencyInjection.Abstractions ({{VER_MS_EXTENSIONS}})
    - Microsoft.Extensions.DependencyInjection ({{VER_MS_EXTENSIONS}})
    - Microsoft.Extensions.Options ({{VER_MS_EXTENSIONS}})
    - Microsoft.Data.SqlClient ({{VER_MS_SQLCLIENT}})
    - Microsoft.IdentityModel.Protocols.OpenIdConnect ({{VER_MS_IDENTITY_PROTOCOLS}})
  
  Azure:
    - Azure.Identity ({{VER_AZURE_IDENTITY}})
    - Microsoft.Identity.Client ({{VER_MS_IDENTITY_CLIENT}})
  
  System:
    - System.Configuration.ConfigurationManager ({{VER_MS_EXTENSIONS}})
    - System.IdentityModel.Tokens.Jwt ({{VER_MS_IDENTITY_PROTOCOLS}})
    - Microsoft.NETCore.Platforms ({{VER_NETCORE_PLATFORMS}})
    - System.Security.AccessControl ({{VER_SYSTEM_SECURITY_ACCESSCONTROL}})
    - System.Text.Json ({{VER_SYSTEM_TEXT_JSON}})
```

### WINDOWS (Windows projects)
```yaml
count: 12
condition: "references DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}"
packages (all v{{DX_PACKAGE_VERSION}}):
  - DevExpress.ExpressApp.Security.Xpo.Extensions.Win
  - DevExpress.ExpressApp.Win.Design
  - DevExpress.ExpressApp.ReportsV2.Win
  - DevExpress.ExpressApp.Notifications.Win
  - DevExpress.ExpressApp.Office.Win
  - DevExpress.ExpressApp.Validation.Win
  - DevExpress.ExpressApp.PivotChart.Win
  - DevExpress.ExpressApp.ScriptRecorder.Win
  - DevExpress.ExpressApp.FileAttachment.Win
  - DevExpress.ExpressApp.Scheduler.Win
  - DevExpress.ExpressApp.TreeListEditors.Win
  - DevExpress.Win.Demos
```

### BLAZOR_WEB (Web projects)
```yaml
count: 28
condition: "is web project OR contains Global.asax.cs"
packages:
  DevExtreme:
    - DevExtreme.AspNet.Data ({{VER_DEVEXTREME_ASPNET}})
  
  Microsoft ASP.NET:
    - Microsoft.AspNetCore.OData ({{VER_MS_ASPNETCORE_ODATA}})
    - Microsoft.Extensions.DependencyModel ({{VER_MS_EXTENSIONS}})
  
  Swagger:
    - Swashbuckle.AspNetCore ({{VER_SWASHBUCKLE}})
    - Swashbuckle.AspNetCore.Annotations ({{VER_SWASHBUCKLE}})
  
  System:
    - System.CodeDom ({{VER_SYSTEM_CODEDOM}})
    - System.Drawing.Common ({{VER_SYSTEM_DRAWING_COMMON}})
    - System.Reactive ({{VER_SYSTEM_SECURITY_ACCESSCONTROL}})
    - System.Configuration.ConfigurationManager ({{VER_MS_EXTENSIONS}})
    - System.Security.Permissions ({{VER_SYSTEM_SECURITY_PERMISSIONS}})
    - System.Text.Json ({{VER_SYSTEM_TEXT_JSON}})
    - Microsoft.Identity.Client ({{VER_MS_IDENTITY_CLIENT}})
    - Microsoft.NETCore.Platforms ({{VER_NETCORE_PLATFORMS}})
    - System.Security.AccessControl ({{VER_SYSTEM_SECURITY_ACCESSCONTROL}})
  
  DevExpress Blazor (all v{{DX_PACKAGE_VERSION}}):
    - DevExpress.ExpressApp.Notifications.Blazor
    - DevExpress.ExpressApp.AuditTrail.Xpo
    - DevExpress.ExpressApp.ReportsV2.Blazor
    - DevExpress.ExpressApp.TreeListEditors
    - DevExpress.ExpressApp.Scheduler.Blazor
    - DevExpress.ExpressApp.FileAttachment.Blazor
    - DevExpress.ExpressApp.PivotChart
    - DevExpress.ExpressApp.ViewVariantsModule
    - DevExpress.ExpressApp.Office.Blazor
    - DevExpress.ExpressApp.Validation.Blazor
    - DevExpress.ExpressApp.Security.Xpo
    - DevExpress.ExpressApp.WebApi.Xpo
    - DevExpress.Drawing.Skia
```

---

## APPENDIX B: PACKAGE DEDUPLICATION RULES

```yaml
deduplication_strategy:
  when: "same package in multiple sets"
  
rules:
  1_prefer_specific_set:
    priority: WINDOWS > BLAZOR_WEB > BASE
    example: "If package in both BASE and WINDOWS, keep WINDOWS version"
  
  2_base_fallback:
    when: "versions differ and no clear specificity"
    action: "use BASE set version"
  
  3_remove_duplicates:
    method: "keep only one entry per package name"

validation:
  - verify_no_duplicate_package_names
  - count_unique_package_references
```

**Examples:**
```yaml
example_1:
  scenario: "Same package in BASE and BLAZOR_WEB (same version)"
  input:
    - BASE: System.Configuration.ConfigurationManager ({{VER_MS_EXTENSIONS}})
    - BLAZOR_WEB: System.Configuration.ConfigurationManager ({{VER_MS_EXTENSIONS}})
  result: System.Configuration.ConfigurationManager ({{VER_MS_EXTENSIONS}})

example_2:
  scenario: "Package only in specific set"
  input:
    - WINDOWS: DevExpress.ExpressApp.Win.Design ({{DX_PACKAGE_VERSION}})
  result: DevExpress.ExpressApp.Win.Design ({{DX_PACKAGE_VERSION}})
```

---

**END OF CONVERT_TO_NET.MD**
