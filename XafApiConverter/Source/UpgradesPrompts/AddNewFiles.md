# Template-Based File Generation Rules (LLM Optimized)

> **Version:** 2.0 | **Priority:** CRITICAL | **Apply with:** Safety_Rule.md, Configuration.md

---

## TEMPLATE PROCESSING RULES

### TRANS-009: Template Processing
```yaml
rule_id: TRANS-009
applies_to: projects_with_global_asax
template_source: "Templates/Startup.cs"

steps:
  1: detect_template_variables (TRANS-010)
  2: evaluate_conditional_directives (TRANS-011)
  3: include_code_from_true_branches_only
  4: remove_all_directive_lines (#if, #elseif, #else, #endif)
  5: verify_no_unprocessed_directives_remain

output_requirements:
  - no_conditional_directives
  - no_unresolved_variables
  - file_compiles_successfully

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

**Processing Flow:**
```pseudocode
function process_template_and_generate(project_path):
    if not file_exists(project_path + "/Global.asax.cs"):
        return NO_GENERATION_NEEDED
    
    variables = detect_template_variables(project_path)  // TRANS-010
    template_content = upgrade_read_file("Templates/Startup.cs")
    processed = evaluate_directives(template_content, variables)  // TRANS-011
    create_file(project_path + "/Startup.cs", processed)
    verify_generated_file(project_path + "/Startup.cs")  // SAFE-002
```

**Verification:**
```yaml
checks:
  - Startup.cs exists
  - No #if/#elseif/#else/#endif remain
  - File compiles without errors
  - File added to project (if needed)
```

---

### TRANS-010: Template Variable Detection
```yaml
rule_id: TRANS-010
category: variable_detection
applies_to: template_processing

detection_sequence:
  1: locate_global_asax_cs
  2: extract_application_type_name
     pattern: "WebApplication.SetInstance(Session, new <TypeName>())"
     example: "MySolutionWebApplication"
  3: locate_application_class_file
     search: all_cs_files_in_project
  4: detect_variables_from_class:
     UseSecurity:
       condition: "class has field with SecurityModule type"
       default: FALSE
     UseSchedulerModule:
       condition: "class has field with SchedulerAspNetModule type"
       default: FALSE
     OrmIsXpo:
       condition: "project references DevExpress.ExpressApp.Xpo"
       default: FALSE

all_defaults: FALSE
```

**Detection Logic:**
```pseudocode
function detect_template_variables(project_path):
    // Step 1: Find app type name
    global_asax = upgrade_read_file(project_path + "/Global.asax.cs")
    app_type = regex_search(r'new\s+(\w+)\s*\(\)', global_asax).group(1)
    
    // Step 2: Find app class file
    cs_files = find_all_cs_files(project_path)
    app_class_file = find_file_containing(cs_files, f'class {app_type}')
    app_class = upgrade_read_file(app_class_file)
    
    // Step 3: Detect variables
    variables = {
        UseSecurity: "SecurityModule" in app_class,
        UseSchedulerModule: "SchedulerAspNetModule" in app_class,
        OrmIsXpo: project_references_xpo(project_path)
    }
    
    return variables
```

**Examples:**
```yaml
example_1:
  app_class_contains: "SecurityModule, SchedulerAspNetModule"
  project_references: "DevExpress.ExpressApp.Xpo"
  result:
    UseSecurity: TRUE
    UseSchedulerModule: TRUE
    OrmIsXpo: TRUE

example_2:
  app_class_contains: "SecurityModule"
  project_references: "No Xpo"
  result:
    UseSecurity: TRUE
    UseSchedulerModule: FALSE
    OrmIsXpo: FALSE
```

---

### TRANS-011: Conditional Directive Evaluation
```yaml
rule_id: TRANS-011
category: template_processing
applies_to: all_template_files

directive_syntax:
  if: "#if(<Variable>)"
  elseif: "#elseif(<Condition>)"
  else: "#else"
  endif: "#endif"

evaluation:
  - evaluate_all_conditions_using_detected_variables
  - include_code_from_TRUE_branches
  - exclude_code_from_FALSE_branches
  - remove_all_directive_lines_from_output

safety: SAFE-002 (verify), SAFE-008 (idempotent)
```

**Examples:**

**Input (UseSecurity=TRUE):**
```csharp
#if(UseSecurity)
services.AddSecurity();
#endif
```
**Output:**
```csharp
services.AddSecurity();
```

**Input (UseSecurity=FALSE):**
```csharp
#if(UseSecurity)
services.AddSecurity();
#endif
```
**Output:**
```csharp
(empty - removed)
```

**Input (UseSecurity=TRUE, else branch):**
```csharp
#if(UseSecurity)
app.UseAuthentication();
#else
app.UseAnonymousAccess();
#endif
```
**Output:**
```csharp
app.UseAuthentication();
```

**Evaluation Logic:**
```pseudocode
function evaluate_directives(template_content, variables):
    lines = template_content.split_lines()
    output = []
    include_current = TRUE
    stack = []
    
    for line in lines:
        if line.starts_with("#if("):
            var = extract_variable(line)
            condition = variables.get(var, FALSE)
            stack.push({condition: condition, included: include_current})
            include_current = include_current AND condition
            // Skip directive line
            
        elif line.starts_with("#else"):
            current = stack.peek()
            include_current = current.included AND NOT current.condition
            // Skip directive line
            
        elif line.starts_with("#endif"):
            current = stack.pop()
            include_current = current.included if stack else TRUE
            // Skip directive line
            
        else:
            if include_current:
                output.append(line)
    
    return output.join("\n")
```

**Verification:**
```yaml
checks:
  - All directives processed (no #if/#endif remain)
  - Output is valid C# code
  - Expected branches included based on variables
```

---

## TEMPLATE VARIABLES REFERENCE

```yaml
template_variables:
  UseSecurity:
    type: boolean
    default: FALSE
    detection: "SecurityModule field in application class"
    affects: "Security services, authentication middleware"
    
  UseSchedulerModule:
    type: boolean
    default: FALSE
    detection: "SchedulerAspNetModule field in application class"
    affects: "Scheduler services, scheduler middleware"
    
  OrmIsXpo:
    type: boolean
    default: FALSE
    detection: "Project references DevExpress.ExpressApp.Xpo"
    affects: "ORM services, database config, permission caching"
```

---

## TEMPLATE FILE LOCATIONS

```yaml
templates:
  - name: Startup.cs
    path: "Templates/Startup.cs"
    relative_to: "AddNewFiles.md folder"
    target: "Web projects with Global.asax.cs"
    priority: CRITICAL
    
  - name: Program.cs
    path: "Templates/Program.cs"
    target: "Web projects"
    priority: MEDIUM
    
  - name: appsettings.json
    path: "Templates/appsettings.json"
    target: "All .NET 9.0 projects"
    priority: LOW

resolution:
  1: "Locate AddNewFiles.md"
  2: "Get parent directory"
  3: "Append 'Templates/<TemplateName>'"
```

---

## COMPLETE WORKFLOW

```yaml
workflow:
  1_check_needed: file_exists("Global.asax.cs")
  2_extract_app_type: parse_global_asax ? app_type_name
  3_locate_app_class: search_cs_files ? app_class_file
  4_detect_variables: analyze_app_class + project_refs ? variables
  5_load_template: upgrade_read_file("Templates/Startup.cs")
  6_evaluate_directives: process_template ? processed_content
  7_generate_file: create_file("Startup.cs", processed_content)
  8_verify: check_file_exists, no_directives, compiles
  9_cleanup: remove_global_asax_initialization
```

---

**END OF ADDNEWFILES.MD**
