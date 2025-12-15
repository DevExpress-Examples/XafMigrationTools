# Safety Rules for .NET Upgrade (LLM Optimized)

> **Version:** 2.2 | **Priority:** MANDATORY | **Override:** All other rules

---

## CRITICAL SAFETY RULES

### SAFE-001: Unload-Edit-Reload Pattern
```yaml
rule_id: SAFE-001
priority: CRITICAL
applies_to: [.csproj, .vbproj, .vcxproj]
sequence:
  1: upgrade_unload_project(projectPath)
  2: edit_file(projectPath, newContent)
  3: upgrade_reload_project(projectPath)
  4: verify_modifications (SAFE-002)
mandatory_even_if: ["Project appears unloaded", "Simple changes", "One property only"]
```

### SAFE-002: Trust But Verify
```yaml
rule_id: SAFE-002
priority: CRITICAL
applies_to: all_file_modifications
sequence:
  1: perform_modification
  2: upgrade_read_file(modified_file_path) ? actual_content
  3: verify_expected_changes(expected, actual)
  4: if verification_failed ? SAFE-003
never_trust: [tool_success_messages, api_return_codes]
always_verify_by: [reading_actual_file_content, parsing_file_structure]

verifications:
  SDK_STYLE: check_first_line('<Project Sdk="Microsoft.NET.Sdk">')
  TARGET_FRAMEWORK: xpath("/Project/PropertyGroup/TargetFramework") == "{{TARGET_FRAMEWORK}}"
  PACKAGE_ADDITION: count(xpath("//PackageReference")) >= expected_count
  ASSEMBLY_REMOVAL: count('<Reference Include="DevExpress') == 0
  NAMESPACE_MIGRATION: file_contains('using Microsoft.Data.SqlClient;')
```

### SAFE-003: Fallback Strategy
```yaml
rule_id: SAFE-003
priority: CRITICAL
applies_to: all_automated_tools
execution_flow:
  1_primary: attempt_automated_tool
  2_verify: read_and_verify_changes (SAFE-002)
  3_decision: if verification_failed ? execute_fallback
  4_fallback:
    - upgrade_unload_project
    - edit_file(explicit_changes)
    - upgrade_reload_project
    - verify_again (SAFE-002)
  5_final: if still_fails ? request_user_intervention
```

### SAFE-004: Accurate Reporting
```yaml
rule_id: SAFE-004
priority: CRITICAL
applies_to: all_upgrade_reports
before_report:
  - re_read_all_modified_files (upgrade_read_file)
  - compare: original_state vs current_state
  - list_only: changes_actually_present_on_disk
  - mark_incomplete: "[PLANNED]" or "[NOT COMPLETED]"
never_report: [tool_success_messages_alone, assumptions]

report_verification_checklist:
  - "Target framework changed?" ? Read .csproj, verify <TargetFramework>
  - "SDK-style conversion?" ? Check <Project Sdk="Microsoft.NET.Sdk">
  - "Packages added?" ? Count <PackageReference> elements
  - "Assemblies removed?" ? Search for <Reference Include="DevExpress">
  - "Code fixes applied?" ? Search for 'using Microsoft.Data.SqlClient;'
  - "Startup.cs generated?" ? file_search or file_exists_check
```

---

## MANDATORY OPERATIONAL RULES

### SAFE-005: Atomic Project Upgrade
```yaml
rule_id: SAFE-005
priority: MANDATORY
applies_to: all_projects
requirements:
  scope: one_project_at_a_time
  steps: [sdk_conversion, tfm_update, assembly_removal, package_addition, 
          code_fixes, template_generation, verification, git_commit]
strict: "Complete ALL 8 steps for Project A before starting Project B"
forbidden: "Do NOT interleave steps across multiple projects"
```

### SAFE-006: Post-Modification Verification
```yaml
rule_id: SAFE-006
priority: MANDATORY
trigger: after_every_modification_step
steps:
  1: upgrade_read_file(filePath)
  2: parse_content (xml/text/regex)
  3: compare_with_checklist(required_changes)
  4: log_discrepancies("Expected: X, Actual: Y")
  5: if verification_fails ? do_not_proceed
rollback:
  trigger: fails_after_3_attempts
  actions: [restore_backup, log_detailed_error, request_user_intervention]
```

### SAFE-008: Idempotency Rule
```yaml
rule_id: SAFE-008
priority: MANDATORY
applies_to: all_modification_operations
requirements: [safe_to_run_multiple_times, no_duplicate_creation, check_before_apply]

pattern:
  1: read_current_state (upgrade_read_file)
  2: check_if_already_present
  3: if present ? skip, else ? apply
  4: verify (SAFE-002)

examples:
  package_addition: check_if_exists ? add_only_if_not_present
  assembly_removal: check_if_exists ? remove_only_if_present
  namespace_replacement: check_if_old_present ? replace_only_if_found
```

### SAFE-010: Continuous Execution Rule
```yaml
rule_id: SAFE-010
priority: CRITICAL
requirements:
  - never_pause_for_confirmation: true
  - execute_until_completion: true
  - only_stop_on: [unrecoverable_error, plan_completed, explicit_user_request]

forbidden_phrases:
  - "Would you like me to continue?"
  - "Should I proceed with remaining projects?"
  - "Which approach would you prefer?"
  - intermediate_status_reports_asking_confirmation

allowed:
  - brief_inline_progress: "Completed 5 of 19, continuing..."
  - error_reporting_with_automatic_retry
  - completion_summary_at_end
```

### SAFE-011: Batch Operations
```yaml
rule_id: SAFE-011
priority: MANDATORY
applies_to: mass_project_conversions

when_applicable:
  - 5+ projects with similar changes
  - Only .csproj modifications (no code changes)
  - Step 1 (SDK-style conversion) only
  - No interdependencies

when_NOT_applicable:
  - Build dependencies between projects
  - Code changes requiring compilation
  - Step 2/3 (code migration, file generation)
  - Less than 5 projects

batch_pattern:
  phase_1_unload_all: upgrade_unload_project(all_projects)
  phase_2_edit_all: edit_file(all_projects)
  phase_3_reload_all: upgrade_reload_project(all_projects)
  phase_4_verify_all: upgrade_read_file + verify (SAFE-002) for each project

fallback: if any_verification_fails ? retry_failed_individually (SAFE-001)

decision_tree:
  Q1: "Step 1 (SDK conversion)?" ? YES: Q2, NO: SEQUENTIAL
  Q2: "5+ projects?" ? YES: Q3, NO: SEQUENTIAL
  Q3: "All independent?" ? YES: BATCH, NO: SEQUENTIAL
```

**SAFE-011 vs SAFE-005 Conflict Resolution:**
```yaml
SAFE-005: "Complete all 8 steps for Project A before Project B" ? ENTIRE upgrade
SAFE-011: "Batch convert all projects" ? STEP 1 ONLY (SDK conversion)

interpretation:
  Step 1 (SDK conversion): BATCH MODE (SAFE-011) for all projects
  Step 2 (code migration): SEQUENTIAL (SAFE-005) project by project
  Step 3 (file generation): SEQUENTIAL (SAFE-005) project by project
```

### SAFE-012: Read Documentation First Rule
```yaml
rule_id: SAFE-012
priority: MANDATORY
applies_to: all_scenarios_with_external_references
core: NEVER improvise values from documentation. ALWAYS read BEFORE execution.

execution_sequence:
  0_identify: extract_documentation_references(scenario_file)
  1_read_all:
    - upgrade_read_file("Convert_to_NET.md") ? TRANS rules + Appendix A (package sets)
    - upgrade_read_file("Configuration.md") ? {{DX_VERSION}}, {{TARGET_FRAMEWORK}}, {{TARGET_FRAMEWORK_WINDOWS}}
  2_extract:
    - package_names_and_versions (exact names, not placeholders)
    - target_framework_values (e.g., "net9.0", not "{{TFM}}")
    - transformation_rules (TRANS-001 to TRANS-011)
  3_verify:
    - no_placeholders: package_sets have concrete names
    - no_assumed_values: versions are specific (e.g., "25.1.6")
  4_if_not_found:
    - file_search() ? if still_not_found ? request_user
  5_forbidden:
    - proceed_with_assumptions
    - invent_package_names
    - invent_version_numbers

verification_checklist:
  - "Read ALL referenced files?" ? YES (mandatory)
  - "Extracted ALL required values?" ? YES (package names, versions, rules)
  - "Using ANY assumed values?" ? NO (must be from documentation)
  - "Have concrete package lists?" ? YES (from Appendix A)

common_violations:
  violation_1:
    description: "Adding packages NOT in documentation"
    example: "DevExpress.ExpressApp.Maps.Blazor (NOT in Appendix A)"
    correct: "Read Convert_to_NET.md Appendix A ? use ONLY listed packages"
  
  violation_2:
    description: "Using assumed version numbers"
    example: "{{DX_VERSION}} = '25.1.6' (assumed from old references)"
    correct: "Read Configuration.md ? extract exact {{DX_VERSION}} value"
  
  violation_3:
    description: "Creating plan without reading docs"
    example: "Get solution info ? Create plan ? Execute"
    correct: "Read docs (SAFE-012) ? Get solution info ? Create plan ? Execute"

fallback_when_missing:
  if: "file_search() fails"
  actions:
    1: "Log: 'Documentation file X not found'"
    2: "file_search with broader patterns"
    3: "If still not found: request user for file path or package list"
    4: "DO NOT proceed with assumptions"
```

### SAFE-013: Plan File Generation Rule
```yaml
rule_id: SAFE-013
priority: MANDATORY
applies_to: all_upgrade_scenarios_requiring_plans
core: ALWAYS save upgrade plans to files. NEVER output full plans to console.

plan_file_generation:
  method: create_file()
  filename_pattern: "dotnet-upgrade-plan-{step}.md"
  location: ".github/upgrades/"
  
  required_actions:
    - Create .github/upgrades/ folder if not exists
    - Generate plan content using appropriate template
    - Save to file using create_file()
    - Display brief summary to console (NOT full plan)
  
  console_output:
    allowed: [file_path_link, project_count, target_frameworks, brief_summary]
    forbidden: [full_plan_content, detailed_steps, complete_package_lists]
  
  user_interaction:
    - Show file path and brief summary
    - Ask: "Plan saved. Ready to execute? (yes/no)"
    - Wait for confirmation before execution
```
---

## DECISION MAKING FRAMEWORK

```yaml
when_uncertain:
  question_1:
    q: "Relevant Safety Rule exists?"
    yes: "Follow Safety Rule (highest priority)"
    no: "Proceed to question 2"
  
  question_2:
    q: "Specific Transformation Rule exists (TRANS-*)?"
    yes: "Follow Transformation Rule + apply Safety Rules"
    no: "Proceed to question 3"
  
  question_3:
    q: "Workflow step covering this?"
    yes: "Follow workflow step"
    no: "Proceed to question 4"
  
  question_4:
    q: "Can infer from similar rule?"
    yes: "Apply similar rule + Safety Rules + verify (SAFE-002)"
    no: "Request user guidance with specific details"

always_after_any_action:
  verify: SAFE-002 (upgrade_read_file + verify_expected_changes)
```

---

**END OF SAFETY_RULE.MD**
