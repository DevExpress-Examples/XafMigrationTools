# Upgrade Configuration (LLM Optimized)

> **Version:** 1.0 | **Purpose:** Central configuration variables

---

## CONFIGURATION VARIABLES

### Target Framework
```yaml
TARGET_FRAMEWORK: "net9.0"
TARGET_FRAMEWORK_WINDOWS: "net9.0-windows"
TARGET_FRAMEWORK_FULL: ".NET 9.0"
SOURCE_FRAMEWORK: "v4.8.1"
SOURCE_FRAMEWORK_FULL: ".NET Framework 4.8.1"
```

### DevExpress Versions
```yaml
DX_PACKAGE_VERSION: "25.1.6"
DX_ASSEMBLY_VERSION: "v25.1"

use_directory_packages: false

# When use_directory_packages = false:
DX_VERSION_ATTRIBUTE: 'Version="25.1.6"'

# When use_directory_packages = true:
DX_VERSION_ATTRIBUTE: ''
```

**Package Reference Examples:**
```xml
<!-- use_directory_packages = false -->
<PackageReference Include="DevExpress.ExpressApp" Version="25.1.6" />

<!-- use_directory_packages = true -->
<PackageReference Include="DevExpress.ExpressApp" />
```

### Example Project Names
```yaml
SOLUTION_NAME: "MySolution"
MODULE_PROJECT: "MySolution.Module"
MODULE_WEB_PROJECT: "MySolution.Module.Blazor"
WEB_PROJECT: "MySolution.Blazor"
WIN_PROJECT: "MySolution.Win"
CONSOLE_PROJECT: "MySolution.Console"
```

### Package Versions
```yaml
# Microsoft
VER_MS_EXTENSIONS: "9.0.0"
VER_MS_CODEANALYSIS: "4.10.0"
VER_MS_SQLCLIENT: "5.2.2"
VER_MS_ASPNETCORE_ODATA: "9.3.2"

# Azure
VER_AZURE_IDENTITY: "1.17.1"
VER_MS_IDENTITY_CLIENT: "4.78.0"
VER_MS_IDENTITY_PROTOCOLS: "8.14.0"

# System
VER_SYSTEM_TEXT_JSON: "9.0.6"
VER_SYSTEM_CONFIG_MANAGER: "9.0.0"
VER_SYSTEM_SECURITY_ACCESSCONTROL: "6.0.1"
VER_SYSTEM_DRAWING_COMMON: "8.0.15"
VER_SYSTEM_CODEDOM: "8.0.0"
VER_SYSTEM_REACTIVE: "6.0.1"
VER_SYSTEM_SECURITY_PERMISSIONS: "8.0.0"

# Other
VER_DEVEXTREME_ASPNET_DATA: "5.1.0"
VER_SWASHBUCKLE_ASPNETCORE: "6.9.0"
VER_NETCORE_PLATFORMS: "7.0.4"
```

### Detection Rules
```yaml
windows_detection:
  assembly_reference: "DevExpress.ExpressApp.Win.{{DX_ASSEMBLY_VERSION}}"
  package_reference: "DevExpress.ExpressApp.Win"

web_detection:
  file_exists: "Global.asax.cs"
  project_name_contains: ".Web"
  project_name_contains_alt: ".Blazor"

xpo_detection:
  package_reference: "DevExpress.ExpressApp.Xpo"
  package_reference_alt: "DevExpress.Persistent.BaseImpl.Xpo"
```

### Template Paths
```yaml
templates_folder: "Templates"
startup_template: "Templates/Startup.cs"
program_template: "Templates/Program.cs"
appsettings_template: "Templates/appsettings.json"
```

---

## QUICK REFERENCE

| Variable | Value | Used In |
|----------|-------|---------|
| `{{TARGET_FRAMEWORK}}` | `net9.0` | All documents |
| `{{TARGET_FRAMEWORK_WINDOWS}}` | `net9.0-windows` | Convert_to_NET.md |
| `{{DX_PACKAGE_VERSION}}` | `25.1.6` | Convert_to_NET.md |
| `{{DX_ASSEMBLY_VERSION}}` | `v25.1` | Convert_to_NET.md |
| `{{VER_MS_EXTENSIONS}}` | `9.0.0` | Convert_to_NET.md |

---

## UPDATE SCENARIOS

**Upgrade to .NET 10.0:**
```yaml
TARGET_FRAMEWORK: "net10.0"
TARGET_FRAMEWORK_WINDOWS: "net10.0-windows"
TARGET_FRAMEWORK_FULL: ".NET 10.0"
```

**Upgrade to DevExpress 26.1:**
```yaml
DX_PACKAGE_VERSION: "26.1.6"
DX_ASSEMBLY_VERSION: "v26.1"
```

**Enable Directory.Packages.props:**
```yaml
use_directory_packages: true
```

---

**END OF CONFIGURATION.MD**
