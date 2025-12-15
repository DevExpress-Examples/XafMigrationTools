# Step 1: Project Conversion to .NET 9.0 (LLM Optimized)

> **Scope:** ONLY .csproj modifications | NO code changes | NO new files

---

## RULES TO APPLY

### INCLUDE (TRANS):
- **TRANS-001** - SDK-Style Conversion
- **TRANS-002** - Target Framework Selection
- **TRANS-003** - Windows Desktop Properties (if applicable)
- **TRANS-004** - Assembly Reference Removal
- **TRANS-005** - NuGet Package Addition

### EXCLUDE (TRANS):
- **TRANS-006 to TRANS-011** (code changes, file generation - Step 2/3)

### MANDATORY (SAFE):
- **SAFE-001** - Unload-Edit-Reload Pattern
- **SAFE-002** - Trust But Verify
- **SAFE-005** - Atomic Project Upgrade
- **SAFE-006** - Post-Modification Verification
- **SAFE-008** - Idempotency Rule
- **SAFE-010** - Continuous Execution Rule
- **SAFE-011** - Batch Operations
- **SAFE-012** - Read Documentation First Rule
- **SAFE-013** - Plan File Generation Rule
---

## PLAN OUTPUT FORMAT (SAFE-013)
**See `Safety_Rule.md` for full SAFE-013 definition**

### File Generation:
```yaml
method: create_file()
filename: "dotnet-upgrade-plan-step1.md"
location: ".github/upgrades/"
```

### Console Output (Brief Summary ONLY):
```yaml
show_ONLY:
  - "Plan saved to: .github/upgrades/dotnet-upgrade-plan-step1.md"
  - "Projects: [N], Target: {{TARGET_FRAMEWORK}}/{{TARGET_FRAMEWORK_WINDOWS}}"
  - "Package sets: BASE, WINDOWS, BLAZOR_WEB"
  - "⚠️ Projects will NOT compile after Step 1 (expected)"

do_NOT_show:
  - Full plan markdown
  - Package lists
  - Transformation details
```

### Plan Structure Requirements:

**Header:**
- Solution name, project count, target frameworks
- DevExpress version from {{DX_PACKAGE_VERSION}}
- Source framework from {{SOURCE_FRAMEWORK_FULL}}
- Critical warnings (will not compile after Step 1)

**Per Project:**
- Project path, target framework ({{TARGET_FRAMEWORK}} or {{TARGET_FRAMEWORK_WINDOWS}})
- Package sets: BASE | BASE+WINDOWS | BASE+BLAZOR_WEB (from Convert_to_NET.md Appendix A)
- TRANS-001: SDK-Style Conversion details
- TRANS-002: Target Framework Selection
- TRANS-003: Windows Desktop Properties (if Windows)
- TRANS-004: Assembly Reference Removal (all DevExpress, System.Web)
- TRANS-005: NuGet Package Addition (from Appendix A, use exact versions from Configuration.md)
- Verification checklist

**Execution Sequence:**
```yaml
CRITICAL_DECISION (SAFE-011):
  if: project_count >= 5 AND step == 1 AND only_csproj_changes
  then: BATCH_MODE
    Phase 1: upgrade_unload_project(all)
    Phase 2: edit_file(all)
    Phase 3: upgrade_reload_project(all)
    Phase 4: verify(all)
  else: SEQUENTIAL_MODE
    Per project: unload → edit → reload → verify
```

**Validation:**
- Expected: [N] projects converted, 0 assembly references, will NOT compile

---

## EXECUTION INSTRUCTIONS

**Before plan generation (SAFE-012):**
1. Read Configuration.md → {{DX_PACKAGE_VERSION}}, {{TARGET_FRAMEWORK}}, {{TARGET_FRAMEWORK_WINDOWS}}
2. Read Convert_to_NET.md → Appendix A (BASE, WINDOWS, BLAZOR_WEB packages)

**During plan generation:**
1. Use {{variables}} from Configuration.md (NO hardcoded versions)
2. Reference Appendix A for packages (NO hardcoded counts)
3. Determine BATCH vs SEQUENTIAL (SAFE-011)
4. Save to file, show only brief summary in console

**Critical:**
- NO hardcoded: net9.0, 25.1.6, 25 packages, etc.
- Use: {{TARGET_FRAMEWORK}}, {{DX_PACKAGE_VERSION}}, Appendix A references

---

## EXECUTION SEQUENCE

### 0. Read Documentation (SAFE-012)

**BEFORE gathering solution info, read ALL referenced files:**

```yaml
read_convert_to_net_md:
  file: "Convert_to_NET.md"
  extract:
    - TRANS-001 to TRANS-005 rule definitions
    - Appendix A: package lists (BASE, WINDOWS, BLAZOR_WEB)
    - Exact package names (not placeholders)
  method: upgrade_read_file() or file_search() + upgrade_read_file()

read_configuration_md:
  file: "Configuration.md"
  extract:
    - {{DX_VERSION}} (e.g., "25.1.6")
    - {{TARGET_FRAMEWORK}} (e.g., "net9.0")
    - {{TARGET_FRAMEWORK_WINDOWS}} (e.g., "net9.0-windows")
  method: upgrade_read_file() or file_search() + upgrade_read_file()

verify_extracted:
  - Package sets have concrete names (not "{{PACKAGE}}")
  - Versions are specific (not "{{VERSION}}")
  - Target frameworks defined (not "{{TFM}}")

if_not_found:
  - file_search() with broader patterns
  - If still not found: request user for file paths
  - DO NOT proceed with assumptions or invented values
```

### 1. Gather Solution Info

```yaml
steps:
  1: upgrade_get_solution_path()
  2: upgrade_get_projects_info()
  3: For each project: upgrade_get_project_dependencies()
```

### 2. Determine Parameters

```yaml
target_framework (TRANS-002):
  condition: "Project references DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}"
  if_true: "{{TARGET_FRAMEWORK_WINDOWS}}"
  if_false: "{{TARGET_FRAMEWORK}}"

package_sets (TRANS-005):
  BASE: "ALL projects"
  WINDOWS: "If references DevExpress.ExpressApp.Win"
  BLAZOR_WEB: "If Global.asax.cs exists OR project name contains '.Web' OR '.Blazor'"
```

---

## PLAN STRUCTURE (PER PROJECT)

```markdown
## Project N: [ProjectName]

Path: [FullPath]
Current TFM: [CurrentFramework]
Target TFM: [TargetFramework]
Package Sets: [BASE / BASE+WINDOWS / BASE+BLAZOR_WEB]
Expected Packages: [Number]

### Step N.1: Convert to .NET 9.0

SAFE-001 (Unload-Edit-Reload):
1. upgrade_unload_project([ProjectPath])
2. upgrade_read_file([ProjectPath])
3. Transform (TRANS-001 to TRANS-005):
   TRANS-001:
     - <Project Sdk="Microsoft.NET.Sdk"> (or Sdk.Web for web apps)
     - Remove <Import> statements
     - Remove verbose <PropertyGroup>
     - Keep: TargetFramework, RootNamespace, AssemblyName
   
   TRANS-002:
     - <TargetFramework>[target TFM]</TargetFramework>
   
   TRANS-003 (if Windows):
     - <UseWindowsForms>true</UseWindowsForms>
     - <ImportWindowsDesktopTargets>true</ImportWindowsDesktopTargets>
   
   TRANS-004:
     - Remove ALL <Reference Include="DevExpress.*">
     - Remove ALL <Reference Include="System.Web*">
     - Remove other obsolete references
     - Remove empty <ItemGroup> blocks
   
   TRANS-005:
     - Add <PackageReference> from applicable sets (Appendix A)

4. edit_file([ProjectPath], [transformed_content])
5. upgrade_reload_project([ProjectPath])
6. SAFE-002 (Verify):
   - upgrade_read_file([ProjectPath])
   - Check: <Project Sdk="Microsoft.NET.Sdk"> on first line
   - Check: <TargetFramework> = [target TFM]
   - Check (if Windows): Windows properties present
   - Check: No <Reference Include="DevExpress"> or <Reference Include="System.Web">
   - Check: <PackageReference> count = [expected count]
   - Check: All package versions match Configuration.md

Expected: Project converted to .NET 9.0, does NOT compile (normal)
```

**Note:** All transformations (TRANS-001 to TRANS-005) in single unload-edit-reload cycle.

---

## PLAN VERIFICATION CHECKLIST

```yaml
after_generating_plan:
  1: "All projects included?"
  2: "Projects ordered by dependencies (independent first)?"
  3: "Correct target TFM per project (TRANS-002)?"
  4: "Correct package sets per project (TRANS-005)?"
  5: "All package versions from Configuration.md?"
  6: "Plan contains ONLY .csproj changes?"
  7: "Plan does NOT contain .cs file changes?"
  8: "Plan does NOT contain new file generation?"
```

---

**Projects will NOT compile after Step 1 (expected). Code fixes in Step 2.**

---

**END OF STEP1.MD**
