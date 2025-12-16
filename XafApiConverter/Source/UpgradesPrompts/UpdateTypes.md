# Code Type & Namespace Migration Rules (LLM Optimized)

> **Version:** 3.3 | **Priority:** CRITICAL | **Apply with:** Safety_Rule.md, Configuration.md

---

## ⚠️ CRITICAL CONSTRAINT

```yaml
RULE: DO_NOT_INVENT_MAPPINGS
priority: ABSOLUTE
applies_to: ALL_TYPE_REPLACEMENTS

constraint:
  description: "LLM MUST NOT create type mappings that are not explicitly defined in this document"
  
forbidden_actions:
  - Inventing ASPx* → Dx* mappings not listed in TYPE REPLACEMENT MAP
  - Assuming Web → Blazor type equivalents without explicit mapping
  - Creating custom replacement rules based on patterns
  
required_actions:
  - ONLY use type replacements explicitly defined in TYPE REPLACEMENT MAP section
  - If type not in map → mark as NO_EQUIVALENT and comment out with note
  - Request explicit mapping addition to UpdateTypes.md if needed
  - ALWAYS add standard TODO comment when commenting out code
  - ALWAYS analyze dependencies when commenting out classes

reason: "ASPx → Dx mapping works RARELY and requires explicit verification per type"
```

---

## CRITICAL: FILE HANDLING RULES

### RULE FH-001: Pre-Open All Target Files
**When:** Before starting any migration phase
**What:** Open ALL .cs and .xafml files in IDE
**How:** Use `upgrade_open_file_in_editor` for each file
**Why:** Prevents "nothing" read errors, enables edit_file tool

### RULE FH-002: Never Edit Unopened Files
**Pattern:**
````````

## BACKUP AND RECOVERY RULES

### RULE BR-001: Regular Backups
```yaml
rule_id: BR-001
priority: HIGH
applies_to: ALL_PROJECT_FILES

description: "Perform regular backups of the entire project"

frequency: "Before any major migration step, and at least once every hour during migration"

method:
  - "Manual copy of project folder to a safe location"
  - "Automated backup script/tool, if available"

verification:
  - "Ensure backup is complete and valid"
  - "Test restore process from backup to verify integrity"

note: "Backups are critical to prevent data loss during migration"
```

### RULE BR-002: Recovery Procedure
```yaml
rule_id: BR-002
priority: HIGH
applies_to: ALL_PROJECT_FILES

description: "Procedure to recover from backup"

steps:
  step_1:
    action: "Identify latest valid backup"
    note: "Check backup location for latest timestamp"
  
  step_2:
    action: "Restore project from backup"
    method:
      - "Manual: Copy files from backup location to project folder"
      - "Automated: Use backup tool's restore function"
  
  step_3:
    action: "Validate project integrity"
    checks:
      - "Open all critical files"
      - "Ensure no files are missing or corrupted"
  
  step_4:
    action: "Continue migration or development work"
    condition: "If recovery is successful"

note: "Regularly test backup and recovery procedures to ensure reliability"
```

---

## 🔧 FILE HANDLING RULES

### RULE FH-001: File Opening Strategy
```yaml
rule_id: FH-001
priority: CRITICAL
applies_to: ALL_FILES

description: "Always open files in IDE before reading or editing to prevent 'nothing' errors"

file_types:
  cs_files:
    extensions: ["*.cs", "*.Designer.cs"]
    open_method: "upgrade_open_file_in_editor"
    note: "Standard C# files, open normally"
  
  xafml_files:
    extensions: ["*.xafml"]
    open_method: "upgrade_open_file_in_editor"
    critical_note: |
      ⚠️ XAFML files have visual designer in IDE
      ⚠️ MUST open as XML/text file, NOT through designer
      ⚠️ Designer opens slowly and is not needed for text editing
    
    reason: "XAFML designer is unnecessary for migration text replacements"
    
  other_files:
    extensions: ["*.xml", "*.config", "*.json"]
    open_method: "upgrade_open_file_in_editor"
    note: "Configuration files"

pattern:
  step_1: "Get all project files using get_files_in_project()"
  step_2: "Filter by extension (.cs, .xafml, etc)"
  step_3: "Open ALL files at START of migration (Phase 0)"
  step_4: "Validate all files readable (content != 'nothing')"
  step_5: "Then proceed with migration phases"

benefits:
  - "Prevents 'nothing' read errors"
  - "Enables edit_file tool to work"
  - "Faster subsequent file operations"
  - "Better IDE integration"
  
estimated_time: "+30-60 seconds for ~15-20 files"
prevents: "Hours of debugging 'nothing' errors and failed edits"
```

### RULE FH-002: File Read with Retry Logic
```yaml
rule_id: FH-002
priority: HIGH
applies_to: ALL_FILE_READS

description: "Always validate file read and retry if fails"

pattern:
  ```
  function safe_read_file(file_path, solution_path):
      max_retries = 2
      
      for attempt in 1..max_retries:
          content = upgrade_read_file(file_path)
          
          // Success
          if content != "nothing" and content != null and content.length > 0:
              return content
          
          // Retry: open file and try again
          if attempt < max_retries:
              print(f"⚠️ Retry {attempt}: Failed to read {file_path}, opening file...")
              upgrade_open_file_in_editor(file_path, solution_path, -1)
              wait(500ms)  // Give IDE time to load file
      
      // Still failed after retries
      error(f"❌ Failed to read file after {max_retries} attempts: {file_path}")
      return null
  ```

apply_to:
  - "All upgrade_read_file() calls"
  - "Before edit_file() operations"
  - "Before text replacements"
```

### RULE FH-003: Phase 0 - File Preparation (NEW)
```yaml
rule_id: FH-003
priority: CRITICAL
execution: BEFORE_ALL_OTHER_PHASES

description: "Pre-open all files before starting migration to ensure reliability"

workflow:
  phase_0:
    name: "File Discovery and Preparation"
    duration: "30-60 seconds"
    mandatory: true
    
    steps:
      step_1:
        action: "Get all project files"
        tool: "get_files_in_project(project_path)"
        output: "List of all files in project"
      
      step_2:
        action: "Filter relevant files"
        filters:
          - "*.cs (all C# files)"
          - "*.Designer.cs (designer files)"
          - "*.xafml (XAF model files)"
        output: "Filtered list of files to process"
      
      step_3:
        action: "Open all files in IDE"
        note: "⚠️ For XAFML: open as text/XML, not through designer"
        pseudo_code: |
          for file in filtered_files:
              upgrade_open_file_in_editor(file, solution_path, -1)
              print(f"✓ Opened: {file}")
      
      step_4:
        action: "Validate all files readable"
        pseudo_code: |
          failed_files = []
          for file in filtered_files:
              content = upgrade_read_file(file)
              if content == "nothing" or content == null:
                  failed_files.append(file)
                  print(f"✗ Cannot read: {file}")
              else:
                  print(f"✓ Readable: {file} ({content.length} chars)")
          
          if failed_files.length > 0:
              error("Some files not accessible: " + failed_files)
              return FAILURE
      
      step_5:
        action: "Proceed to Phase 1 (SqlClient migration)"
        condition: "All files validated successfully"

success_criteria:
  - "All .cs files opened and readable"
  - "All .xafml files opened as text/XML and readable"
  - "All .Designer.cs files opened and readable"
  - "No 'nothing' read errors"
  - "Ready to proceed with migrations"

failure_handling:
  - "Log all failed file operations"
  - "Retry failed files with 1 second delay"
  - "If still fails, stop migration and report errors"
  - "Do not proceed to Phase 1 if Phase 0 failed"
```

### RULE FH-004: XAFML Special Handling
```yaml
rule_id: FH-004
priority: CRITICAL
applies_to: ["*.xafml"]

description: "XAFML files require special handling - treat as XML text files, not designer files"

problem:
  - "XAFML files have visual designer in IDE (XAF Model Editor)"
  - "Designer opens slowly and is resource-intensive"
  - "Designer not needed for text-based replacements"
  - "Opening through designer may cause delays or locks"

solution:
  - "Always open XAFML as plain XML/text file"
  - "Use upgrade_open_file_in_editor() normally - it opens as text by default"
  - "Perform text-based find/replace operations"
  - "No need to interact with designer UI"

editing_approach:
  method: "Text-based XML editing"
  tools:
    - "upgrade_read_file() - reads XML as text"
    - "edit_file() - edits XML as text"
    - "multi_replace_string_in_file() - batch replacements"
  
  operations:
    - "Find/replace type names (ASPxGridListEditor → DxGridListEditor)"
    - "Find/replace namespaces (DevExpress.ExpressApp.Web → DevExpress.ExpressApp.Blazor)"
    - "Comment out XML elements (<!-- ... -->)"
    - "Validate XML well-formedness after edits"

validation:
  after_edit:
    - "Check XML is well-formed"
    - "Check no unclosed tags"
    - "Check no syntax errors"
    - "Verify file can be read again"

example:
  before: |
    <Class Name="Employee" EditorTypeName="DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor" />
  
  after: |
    <Class Name="Employee" EditorTypeName="DevExpress.ExpressApp.Blazor.Editors.DxGridListEditor" />
  
  method: "Plain text find/replace, no designer interaction needed"
```

---

## STANDARD COMMENT FORMAT

```yaml
COMMENT_STANDARD:
  rule: "When commenting out ANY code, ALWAYS add this TODO"
  
  cs_files:
    format: |
      // NOTE: [Type/Feature] has no Blazor equivalent
      // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
      // [commented out code]
  
  xafml_files:
    format: |
      <!-- NOTE: [Type/Feature] has no Blazor equivalent -->
      <!-- TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution. -->
      <!-- [commented out code] -->

examples:
  example_cs: |
    // NOTE: ScriptRecorderAspNetModule has no Blazor equivalent
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
    // this.RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ScriptRecorder.Web.ScriptRecorderAspNetModule));
  
  example_xafml: |
    <!-- NOTE: WebMapsPropertyEditor has no Blazor equivalent -->
    <!-- TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution. -->
    <!-- <PropertyEditor Id="Location" PropertyEditorType="DevExpress.ExpressApp.Maps.Web.WebMapsPropertyEditor" /> -->
```

---

## FILE-SPECIFIC RULES

```yaml
UNIVERSAL_RULES:
  description: "All type and namespace mappings apply to BOTH .cs and .xafml files"
  
  cs_files:
    format: "Short type names with using statements"
    example: "ASPxGridListEditor (with using DevExpress.ExpressApp.Web.Editors.ASPx;)"
  
  xafml_files:
    format: "Full qualified type names (namespace.type)"
    example: "DevExpress.ExpressApp.Web.Editors.ASPx.ASPxGridListEditor"
    note: "XAFML is XML, always use full type names"
  
  exceptions:
    description: "Mark in TYPE REPLACEMENT MAP if rule doesn't apply to specific file type"
    example: "applies_to: [cs_only] or [xafml_only]"
```

---

## NAMESPACE MIGRATIONS

### TRANS-006: SqlClient Namespace Migration
```yaml
rule_id: TRANS-006
applies_to: ["*.cs"]
priority: CRITICAL

detect:
  usages: [SqlConnection, SqlCommand, SqlDataAdapter, SqlParameter, SqlDataReader]
  current_namespace: "System.Data.SqlClient"

action:
  if: 'file contains "using System.Data.SqlClient;"'
  then: replace_with("using Microsoft.Data.SqlClient;")

note: "Not applicable to .xafml files (no SqlClient types in XAFML)"

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

**Migration:**
```csharp
// .cs files only
using System.Data.SqlClient; → using Microsoft.Data.SqlClient;
```

---

### TRANS-007: DevExpress Namespace Migrations
```yaml
rule_id: TRANS-007
applies_to: ["*.cs", "*.xafml"]
priority: CRITICAL

namespace_migrations:
  NS-001:
    old: "DevExpress.ExpressApp.Web"
    new: "DevExpress.ExpressApp.Blazor"
    
  NS-002:
    old: "DevExpress.ExpressApp.Web.Editors"
    new: "DevExpress.ExpressApp.Blazor.Editors"
  
  NS-003:
    old: "DevExpress.ExpressApp.Web.SystemModule"
    new: "DevExpress.ExpressApp.Blazor.SystemModule"
  
  NS-004:
    old: "DevExpress.ExpressApp.Validation.Web"
    new: "DevExpress.ExpressApp.Validation.Blazor"
  
  NS-005:
    old: "DevExpress.ExpressApp.Scheduler.Web"
    new: "DevExpress.ExpressApp.Scheduler.Blazor"
  
  NS-006:
    old: "DevExpress.ExpressApp.Office.Web"
    new: "DevExpress.ExpressApp.Office.Blazor"
  
  NS-007:
    old: "DevExpress.ExpressApp.ReportsV2.Web"
    new: "DevExpress.ExpressApp.ReportsV2.Blazor"
  
  NS-008:
    old: "DevExpress.ExpressApp.PivotChart.Web"
    new: NO_EQuiVALENT
    action: comment_out
    note: "// NOTE: PivotChart.Web has no Blazor equivalent"
  
  NS-009:
    old: "DevExpress.ExpressApp.Maps.Web"
    new: NO_EQuiVALENT
    action: comment_out
    note: "// NOTE: Maps.Web has no Blazor equivalent"
  
  NS-010:
    old: "DevExpress.ExpressApp.ScriptRecorder.Web"
    new: NO_EQuiVALENT
    action: comment_out
    note: "// NOTE: ScriptRecorder.Web has no Blazor equivalent"

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

---

## TYPE REPLACEMENTS

### TRANS-008: DevExpress Type Replacements
```yaml
rule_id: TRANS-008
applies_to: ["*.cs", "*.xafml"]
priority: CRITICAL

constraint: "ONLY use mappings defined in TYPE REPLACEMENT MAP"

type_replacements:
  TYPE-001:
    old: "WebApplication"
    new: "BlazorApplication"
    namespace_old: "DevExpress.ExpressApp.Web"
    namespace_new: "DevExpress.ExpressApp.Blazor"
  
  TYPE-002:
    old: "AnalysisControlWeb"
    new: NO_EQUIVALENT
    action: comment_out
    namespace_old: "DevExpress.ExpressApp.PivotChart.Web"
  
  TYPE-003:
    old: "ASPxGridListEditor"
    new: "DxGridListEditor"
    namespace_old: "DevExpress.ExpressApp.Web.Editors.ASPx"
    namespace_new: "DevExpress.ExpressApp.Blazor.Editors"
  
  TYPE-004:
    old: "ASPxLookupPropertyEditor"
    new: "LookupPropertyEditor"
    namespace_old: "DevExpress.ExpressApp.Web.Editors.ASPx"
    namespace_new: "DevExpress.ExpressApp.Blazor.Editors"
  
  TYPE-005:
    old: "SystemAspNetModule"
    new: "SystemBlazorModule"
    namespace_old: "DevExpress.ExpressApp.Web.SystemModule"
    namespace_new: "DevExpress.ExpressApp.Blazor.SystemModule"
  
  TYPE-006:
    old: "ValidationAspNetModule"
    new: "ValidationBlazorModule"
    namespace_old: "DevExpress.ExpressApp.Validation.Web"
    namespace_new: "DevExpress.ExpressApp.Validation.Blazor"
  
  TYPE-007:
    old: "SchedulerAspNetModule"
    new: "SchedulerBlazorModule"
    namespace_old: "DevExpress.ExpressApp.Scheduler.Web"
    namespace_new: "DevExpress.ExpressApp.Scheduler.Blazor"
  
  TYPE-008:
    old: "OfficeAspNetModule"
    new: "OfficeBlazorModule"
    namespace_old: "DevExpress.ExpressApp.Office.Web"
    namespace_new: "DevExpress.ExpressApp.Office.Blazor"
  
  TYPE-009:
    old: "ReportsAspNetModuleV2"
    new: "ReportsBlazorModuleV2"
    namespace_old: "DevExpress.ExpressApp.ReportsV2.Web"
    namespace_new: "DevExpress.ExpressApp.ReportsV2.Blazor"
  
  TYPE-010:
    old: "MapsAspNetModule"
    new: NO_EQUIVALENT
    action: comment_out
    namespace_old: "DevExpress.ExpressApp.Maps.Web"
    note: "// NOTE: MapsAspNetModule has no Blazor equivalent"
  
  TYPE-011:
    old: "WebMapsPropertyEditor"
    new: NO_EQUIVALENT
    action: remove_attribute
    namespace_old: "DevExpress.ExpressApp.Maps.Web"
    note: "<!-- NOTE: WebMapsPropertyEditor has no Blazor equivalent -->"
  
  TYPE-012:
    old: "WebMapsListEditor"
    new: NO_EQUIVALENT
    action: remove_attribute
    namespace_old: "DevExpress.ExpressApp.Maps.Web"
    note: "<!-- NOTE: WebMapsListEditor has no Blazor equivalent -->"
  
  TYPE-013:
    old: "ASPxRichTextPropertyEditor"
    new: NO_EQUIVALENT
    action: comment_out
    namespace_old: "DevExpress.ExpressApp.Office.Web"
    note: "<!-- NOTE: ASPxRichTextPropertyEditor has no direct Blazor equivalent -->"
  
  TYPE-014:
    old: "ScriptRecorderAspNetModule"
    new: NO_EQUIVALENT
    action: comment_out
    namespace_old: "DevExpress.ExpressApp.ScriptRecorder.Web"
    note: "// NOTE: ScriptRecorderAspNetModule has no Blazor equivalent"
  
  TYPE-015:
    old: "ScriptRecorderModuleBase"
    new: NO_EQUIVALENT
    action: comment_out
    namespace_old: "DevExpress.ExpressApp.ScriptRecorder"
    note: "// NOTE: ScriptRecorderModuleBase has no Blazor equivalent"

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

---

## ENUM & SPECIAL TYPE RULES

### TRANS-009: Enum and Complex Type Handling
```yaml
rule_id: TRANS-009
applies_to: ["*.cs"]
priority: CRITICAL

description: "When certain enums or types are used, comment out ENTIRE class/method"

enum_rules:
  ENUM-001:
    enum: "TemplateType"
    values: ["TemplateType.Horizontal", "TemplateType.Vertical"]
    action: comment_out_entire_class
    reason: "TemplateType enum has no Blazor equivalent"
  
  ENUM-002:
    type: "Page"
    namespace: "System.Web.UI"
    action: comment_out_entire_class
    reason: "System.Web.UI.Page is Web Forms specific"
    note: |
      // NOTE: System.Web.UI.Page is Web Forms specific, no Blazor equivalent
      // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.

type_rules:
  TYPE-RULE-001:
    type: "PopupShowingEventArgs"
    namespace: "DevExpress.ExpressApp.Web"
    action: comment_out_entire_class
    reason: "PopupShowingEventArgs is Web Forms specific"

detection:
  scan_for:
    - "TemplateType.Horizontal"
    - "TemplateType.Vertical"
    - "PopupShowingEventArgs"
    - "System.Web.UI.Page"
    - ": Page"  # inheritance from Page
  
  action: "Comment out entire class containing these usages"

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

**Examples:**

**ENUM-002: Page Type Usage**
```csharp
// OLD - Class derives from Page
public partial class Default : Page {
    protected void Page_Load(object sender, EventArgs e) {
        // logic
    }
}

// NEW - Entire class commented out
// NOTE: System.Web.UI.Page is Web Forms specific, no Blazor equivalent
// TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
/*
public partial class Default : Page {
    protected void Page_Load(object sender, EventArgs e) {
        // logic
    }
}
*/
```

---

## ITERATIVE BUILD & FIX PROCESS

### TRANS-010: Build-Fix-Comment Iteration
```yaml
rule_id: TRANS-010
applies_to: ["*.cs"]
priority: CRITICAL
execution_phase: POST_MIGRATION

description: "After all type replacements, iteratively build and fix/comment errors"

process:
  max_iterations: 3
  
  iteration_steps:
    step_1: "Build project"
    step_2: "Analyze compilation errors"
    step_3: "Attempt automatic fixes for fixable errors"
    step_4: "If errors remain, analyze dependencies"
    step_5: "Comment out classes with unfixable errors + their dependents"
    step_6: "Rebuild and repeat until success or max iterations"

fixable_errors:
  - "Missing using statement (can add using)"
  - "Namespace mismatch (can fix namespace)"
  - "Simple type name errors (can apply mapping)"

unfixable_errors:
  - "Type has no equivalent (must comment out)"
  - "API breaking changes (needs manual review)"
  - "Missing method/property (needs manual implementation)"

dependency_analysis:
  when: "Commenting out a class"
  action: "Find all usages of this class"
  cascade: "Comment out all dependent classes recursively"
  
  example: |
    If commenting out ClassA:
      1. Find all classes that use ClassA
      2. Comment out those classes too
      3. Find usages of those classes
      4. Repeat until no more dependencies

comment_format:
  for_build_errors: |
    // NOTE: Class commented out due to build errors after migration
    // Original error: [error message]
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
  
  for_dependency: |
    // NOTE: Class commented out because it depends on [DependencyClass] which has no Blazor equivalent
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

**Process Flow:**

```pseudocode
function iterative_build_and_fix(project_path):
    max_iterations = 3
    iteration = 0
    
    while iteration < max_iterations:
        iteration++
        print(f"Build iteration {iteration}/{max_iterations}")
        
        // Step 1: Build
        build_result = upgrade_build_project(project_path)
        
        if build_result.success:
            print("✅ Build successful!")
            return SUCCESS
        
        // Step 2: Analyze errors
        errors = upgrade_get_current_dotnet_build_errors(project_path)
        
        // Step 3: Separate fixable and unfixable errors
        fixable_errors = filter_fixable_errors(errors)
        unfixable_errors = filter_unfixable_errors(errors)
        
        // Step 4: Try to fix fixable errors
        if fixable_errors:
            for error in fixable_errors:
                attempt_automatic_fix(error)
        
        // Step 5: Comment out classes with unfixable errors
        if unfixable_errors:
            classes_to_comment = extract_classes_from_errors(unfixable_errors)
            
            for class_info in classes_to_comment:
                // Step 6: Analyze dependencies
                dependent_classes = analyze_dependencies(class_info)
                
                // Comment out main class
                comment_out_class_with_note(
                    class_info.file_path,
                    class_info.class_name,
                    f"Build error: {class_info.error_message}"
                )
                
                // Step 7: Comment out dependent classes
                for dependent in dependent_classes:
                    comment_out_class_with_note(
                        dependent.file_path,
                        dependent.class_name,
                        f"Depends on {class_info.class_name}"
                    )
        
        // Continue to next iteration
    
    // Max iterations reached
    print(f"❌ Failed to build after {max_iterations} iterations")
    print("Manual intervention required for remaining errors")
    return FAILURE

function analyze_dependencies(class_info):
    // Find all files that use this class
    all_cs_files = get_all_cs_files(solution_path)
    dependent_files = []
    
    for file in all_cs_files:
        content = upgrade_read_file(file)
        if contains_usage_of_class(content, class_info.class_name):
            dependent_files.append(file)
    
    // Extract classes from dependent files
    dependent_classes = []
    for file in dependent_files:
        classes = extract_classes(file)
        dependent_classes.extend(classes)
    
    return dependent_classes

function comment_out_class_with_note(file_path, class_name, reason):
    content = upgrade_read_file(file_path)
    
    // Find class definition
    class_start = find_class_start(content, class_name)
    class_end = find_class_end(content, class_start)
    
    // Extract class code
    class_code = content[class_start:class_end]
    
    // Create comment
    comment = f"""
// NOTE: {class_name} commented out due to migration issues
// Reason: {reason}
// TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
/*
{class_code}
*/
"""
    
    // Replace in file
    new_content = content.replace(class_code, comment)
    edit_file(file_path, new_content)
```

**Example Iteration:**

```
Iteration 1:
  ✅ Build
  ❌ 15 errors found
  ✅ Fixed 5 errors (missing usings)
  ❌ 10 unfixable errors remain
  → Comment out 3 classes + 2 dependents
  
Iteration 2:
  ✅ Build
  ❌ 8 errors found
  ✅ Fixed 3 errors
  ❌ 5 unfixable errors remain
  → Comment out 2 classes + 1 dependent
  
Iteration 3:
  ✅ Build
  ✅ SUCCESS! Project compiles
```

---

## TYPE REPLACEMENT MAP

```yaml
# UNIVERSAL RULES (apply to both .cs and .xafml unless marked otherwise)

# Application Types
WebApplication → BlazorApplication

# Editor Types
ASPxGridListEditor → DxGridListEditor
ASPxLookupPropertyEditor → LookupPropertyEditor

# Module Types (cs_only)
SystemAspNetModule → SystemBlazorModule
ValidationAspNetModule → ValidationBlazorModule
SchedulerAspNetModule → SchedulerBlazorModule
OfficeAspNetModule → OfficeBlazorModule
ReportsAspNetModuleV2 → ReportsBlazorModuleV2

# No Equivalent (comment out)
AnalysisControlWeb → NO_EQUIVALENT
MapsAspNetModule → NO_EQUIVALENT
ScriptRecorderAspNetModule → NO_EQUIVALENT
ScriptRecorderModuleBase → NO_EQUIVALENT
WebMapsPropertyEditor → NO_EQUIVALENT
WebMapsListEditor → NO_EQUIVALENT
ASPxRichTextPropertyEditor → NO_EQUIVALENT

# Web Forms Types (comment out entire class)
Page → NO_EQUIVALENT  # [comment entire class]
TemplateType → NO_EQUIVALENT  # [comment entire class]
PopupShowingEventArgs → NO_EQUIVALENT  # [comment entire class]

# NOT MAPPED (comment out if found)
# - ASPxStringPropertyEditor → NO_MAPPING
# - Any other ASPx* → NO_MAPPING
```

---

## MIGRATION WORKFLOW (UPDATED v3.3)

```yaml
workflow:
  step_0:
    name: "File Discovery and Preparation"
    rule: FH-003
    priority: CRITICAL
    duration: "30-60 seconds"
    files: ["**/*.cs", "**/*.Designer.cs", "**/*.xafml"]
    mandatory: true
    description: "Pre-open all target files to prevent 'nothing' read errors"
    
    actions:
      - Get all project files via get_files_in_project()
      - Filter .cs, .Designer.cs, .xafml files
      - Open ALL files using upgrade_open_file_in_editor()
      - Validate all files readable (content != "nothing")
      - Stop if any files fail validation
    
    critical_notes:
      - "⚠️ XAFML files: open as XML/text, NOT through designer"
      - "⚠️ All files MUST be opened before proceeding"
      - "⚠️ Prevents 90% of file access issues"
    
    success_criteria:
      - All files opened successfully
      - All files return content when read
      - No "nothing" errors
      - Ready for Phase 1
  
  step_1:
    name: "SqlClient namespace migration"
    rule: TRANS-006
    files: "**/*.cs"
    requires: "step_0 completed successfully"
    file_handling: "Use safe_read_file() with retry logic (FH-002)"
    
  step_2:
    name: "DevExpress namespace migrations"
    rule: TRANS-007
    files: ["**/*.cs", "**/*.xafml"]
    requires: "step_0 completed successfully"
    file_handling: "Files already open from step_0, direct reads OK"
    xafml_note: "Edit XAFML as text/XML, not through designer"
    
  step_3:
    name: "DevExpress type replacements"
    rule: TRANS-008
    files: ["**/*.cs", "**/*.xafml"]
    requires: "step_0 completed successfully"
    file_handling: "Files already open, use edit_file() directly"
  
  step_4:
    name: "Enum and special type handling"
    rule: TRANS-009
    files: "**/*.cs"
    requires: "step_0 completed successfully"
    
  step_5:
    name: "Iterative build and fix"
    rule: TRANS-010
    files: "**/*.cs"
    description: "Build, analyze, fix/comment, rebuild (max 3 iterations)"
    critical: true
    requires: "step_0 completed successfully"
    
  step_6:
    name: "Final verification"
    action: run_build
    expected: success
    requires: "step_5 completed"

execution_order:
  - "step_0 MUST execute first"
  - "steps 1-6 execute sequentially"
  - "If step_0 fails, STOP entire workflow"
  - "If any step 1-5 fails, report and continue to next"
  - "step_6 is final validation"

estimated_time:
  step_0: "30-60 seconds"
  step_1: "30 seconds"
  step_2: "2 minutes"
  step_3: "2 minutes"
  step_4: "1 minute"
  step_5: "5 minutes"
  step_6: "1 minute"
  total: "~12-15 minutes with Phase 0"
  
comparison:
  without_step_0: "3-4 minutes (with file access issues and retries)"
  with_step_0: "12-15 minutes (но без ошибок и ретраев)"
  recommendation: "ALWAYS use step_0 for reliability"
```

---

**END OF UPDATETYPES.MD v3.3**
