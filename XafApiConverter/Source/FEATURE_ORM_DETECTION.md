# Feature: ORM Detection and ORM-Specific Package Management

## Overview

Added automatic detection of ORM framework (Entity Framework vs XPO) and ORM-specific package selection during project conversion.

---

## Problem

Previously, `PackageManager` assumed all projects use **XPO** and added XPO-specific packages:
- `DevExpress.Persistent.BaseImpl.Xpo`
- `DevExpress.ExpressApp.Security.Xpo`
- `DevExpress.ExpressApp.WebApi.Xpo`
- etc.

This broke **EF Core** projects which need different packages:
- `DevExpress.Persistent.BaseImpl.EFCore`
- `DevExpress.ExpressApp.Security.EFCore`
- `DevExpress.ExpressApp.WebApi.EFCore`

---

## Solution

### 1. ORM Detection

Added `PackageManager.IsProjectReferencesEF()` method:

```csharp
public static bool IsProjectReferencesEF(string projectPath) {
    if (!File.Exists(projectPath)) {
        return false;
    }

    var efReferences = new[] {
        "DevExpress.ExpressApp.EF6",
        "DevExpress.Persistent.BaseImpl.EF6",
        "DevExpress.ExpressApp.Security.EF6",
        "DevExpress.ExpressApp.EFCore",
        "DevExpress.Persistent.BaseImpl.EFCore",
        "DevExpress.ExpressApp.Security.EFCore"
    };

    var content = File.ReadAllText(projectPath);
    return efReferences.Any(efRef => 
        content.Contains(efRef, StringComparison.OrdinalIgnoreCase));
}
```

**Logic:**
- Searches project file for EF-specific references
- Returns `true` if any EF reference found
- Returns `false` if no EF references (assumes XPO)

---

### 2. ORM-Specific Package Lists

#### Base Packages (Added to all projects)

**XPO (Default):**
```xml
<PackageReference Include="DevExpress.Persistent.BaseImpl.Xpo" Version="25.1.6" />
```

**EF Core:**
```xml
<PackageReference Include="DevExpress.Persistent.BaseImpl.EFCore" Version="25.1.6" />
```

#### Blazor Web Packages (Added to Web projects)

**XPO:**
```xml
<PackageReference Include="DevExpress.ExpressApp.AuditTrail.Xpo" Version="25.1.6" />
<PackageReference Include="DevExpress.ExpressApp.Security.Xpo" Version="25.1.6" />
<PackageReference Include="DevExpress.ExpressApp.WebApi.Xpo" Version="25.1.6" />
```

**EF Core:**
```xml
<PackageReference Include="DevExpress.ExpressApp.AuditTrail.EFCore" Version="25.1.6" />
<PackageReference Include="DevExpress.ExpressApp.Security.EFCore" Version="25.1.6" />
<PackageReference Include="DevExpress.ExpressApp.WebApi.EFCore" Version="25.1.6" />
```

---

## Implementation Details

### Modified Methods

#### 1. PackageManager.GetPackages()

**Before:**
```csharp
public List<PackageReference> GetPackages(
    bool isWindowsProject, 
    bool isWebProject) {
    // Always used XPO packages
}
```

**After:**
```csharp
public List<PackageReference> GetPackages(
    bool isWindowsProject, 
    bool isWebProject, 
    bool isEfProject = false) {  // New parameter
    
    var packages = new List<PackageReference>();
    
    // Pass ORM type to package methods
    AddBasePackages(packages, isEfProject);
    
    if (isWebProject) {
        AddBlazorWebPackages(packages, isEfProject);
    }
    
    return DeduplicatePackages(packages);
}
```

---

#### 2. PackageManager.AddBasePackages()

**Before:**
```csharp
private void AddBasePackages(List<PackageReference> packages) {
    // Always added XPO
    packages.Add(new PackageReference(
        "DevExpress.Persistent.BaseImpl.Xpo", dx));
}
```

**After:**
```csharp
private void AddBasePackages(
    List<PackageReference> packages, 
    bool isEfProject) {
    
    // ORM-specific base packages
    if (isEfProject) {
        // EF Core packages
        packages.Add(new PackageReference(
            "DevExpress.Persistent.BaseImpl.EFCore", dx));
    }
    else {
        // XPO packages (default)
        packages.Add(new PackageReference(
            "DevExpress.Persistent.BaseImpl.Xpo", dx));
    }
}
```

---

#### 3. PackageManager.AddBlazorWebPackages()

**Before:**
```csharp
private void AddBlazorWebPackages(List<PackageReference> packages) {
    // Always added XPO packages
    packages.Add("DevExpress.ExpressApp.Security.Xpo");
    packages.Add("DevExpress.ExpressApp.WebApi.Xpo");
}
```

**After:**
```csharp
private void AddBlazorWebPackages(
    List<PackageReference> packages, 
    bool isEfProject) {
    
    // Common Blazor packages...
    
    // ORM-specific Blazor packages
    if (isEfProject) {
        // EF Core packages
        packages.AddRange(new[] {
            new PackageReference("DevExpress.ExpressApp.AuditTrail.EFCore", dx),
            new PackageReference("DevExpress.ExpressApp.Security.EFCore", dx),
            new PackageReference("DevExpress.ExpressApp.WebApi.EFCore", dx)
        });
    }
    else {
        // XPO packages (default)
        packages.AddRange(new[] {
            new PackageReference("DevExpress.ExpressApp.AuditTrail.Xpo", dx),
            new PackageReference("DevExpress.ExpressApp.Security.Xpo", dx),
            new PackageReference("DevExpress.ExpressApp.WebApi.Xpo", dx)
        });
    }
}
```

---

### CSprojConverter Integration

#### 1. ProjectInfo Extended

```csharp
private class ProjectInfo {
    public bool IsWindowsProject { get; set; }
    public bool IsWebProject { get; set; }
    public bool IsEfProject { get; set; }  // NEW!
    public string RootNamespace { get; set; }
    public string AssemblyName { get; set; }
    public bool HasManualAssemblyInfo { get; set; }
}
```

#### 2. AnalyzeProject() Updated

```csharp
private ProjectInfo AnalyzeProject(XDocument doc, string projectDir) {
    var projectPath = Path.Combine(projectDir, 
        Path.GetFileName(projectDir) + ".csproj");
    
    if (!File.Exists(projectPath)) {
        var csprojFiles = Directory.GetFiles(projectDir, "*.csproj");
        if (csprojFiles.Length > 0) {
            projectPath = csprojFiles[0];
        }
    }

    var info = new ProjectInfo {
        IsWindowsProject = DetectWindowsProject(doc),
        IsWebProject = DetectWebProject(doc, projectDir),
        IsEfProject = PackageManager.IsProjectReferencesEF(projectPath), // NEW!
        RootNamespace = ExtractProperty(doc, "RootNamespace"),
        AssemblyName = ExtractProperty(doc, "AssemblyName"),
        HasManualAssemblyInfo = HasManualAssemblyInfo(projectDir)
    };

    return info;
}
```

#### 3. AddPackageReferences() Updated

```csharp
private void AddPackageReferences(XElement project, ProjectInfo info) {
    var packages = _packageManager.GetPackages(
        info.IsWindowsProject, 
        info.IsWebProject, 
        info.IsEfProject);  // Pass ORM type
    
    // ... add packages to project
}
```

---

## Detection Logic

### EF References Checked

```csharp
var efReferences = new[] {
    // EF6 (legacy)
    "DevExpress.ExpressApp.EF6",
    "DevExpress.Persistent.BaseImpl.EF6",
    "DevExpress.ExpressApp.Security.EF6",
    
    // EF Core (modern)
    "DevExpress.ExpressApp.EFCore",
    "DevExpress.Persistent.BaseImpl.EFCore",
    "DevExpress.ExpressApp.Security.EFCore"
};
```

### Example Project Detection

**XPO Project:**
```xml
<ItemGroup>
  <Reference Include="DevExpress.ExpressApp.Xpo" />
  <Reference Include="DevExpress.Persistent.BaseImpl.Xpo" />
</ItemGroup>
```
**Result:** `isEfProject = false` → XPO packages added

**EF Core Project:**
```xml
<ItemGroup>
  <PackageReference Include="DevExpress.Persistent.BaseImpl.EFCore" />
  <PackageReference Include="DevExpress.ExpressApp.EFCore" />
</ItemGroup>
```
**Result:** `isEfProject = true` → EF Core packages added

---

## Package Selection Matrix

| Project Type | ORM | Base Package | Security Package | WebApi Package |
|--------------|-----|--------------|------------------|----------------|
| Console/Module | XPO | `BaseImpl.Xpo` | - | - |
| Console/Module | EF | `BaseImpl.EFCore` | - | - |
| Blazor Web | XPO | `BaseImpl.Xpo` | `Security.Xpo` | `WebApi.Xpo` |
| Blazor Web | EF | `BaseImpl.EFCore` | `Security.EFCore` | `WebApi.EFCore` |

---

## Benefits

### 1. Correct Package Selection
- ✅ XPO projects get XPO packages
- ✅ EF projects get EF packages
- ✅ No more manual corrections needed

### 2. Automatic Detection
- ✅ No user input required
- ✅ Fast and reliable
- ✅ Works with both EF6 and EF Core

### 3. Prevents Errors
- ✅ No mixing XPO and EF packages
- ✅ No runtime errors from wrong ORM
- ✅ Clean project file

### 4. Supports Both ORMs
- ✅ XPO (default)
- ✅ EF6 (legacy)
- ✅ EF Core (modern)

---

## Usage Examples

### Example 1: XPO Module Project

**Input (.NET Framework):**
```xml
<ItemGroup>
  <Reference Include="DevExpress.Persistent.Base" />
  <Reference Include="DevExpress.ExpressApp.Xpo" />
</ItemGroup>
```

**Output (.NET 9 SDK-style):**
```xml
<ItemGroup>
  <PackageReference Include="DevExpress.Persistent.BaseImpl.Xpo" Version="25.1.6" />
  <!-- Other XPO packages -->
</ItemGroup>
```

---

### Example 2: EF Core Module Project

**Input (.NET Framework):**
```xml
<ItemGroup>
  <PackageReference Include="DevExpress.ExpressApp.EFCore" Version="24.1.3" />
  <PackageReference Include="DevExpress.Persistent.BaseImpl.EFCore" Version="24.1.3" />
</ItemGroup>
```

**Output (.NET 9 SDK-style):**
```xml
<ItemGroup>
  <PackageReference Include="DevExpress.Persistent.BaseImpl.EFCore" Version="25.1.6" />
  <!-- Other EF Core packages -->
</ItemGroup>
```

---

### Example 3: Blazor Web Project (XPO)

**Detected:** XPO (no EF references)

**Packages Added:**
```xml
<ItemGroup>
  <!-- Base XPO -->
  <PackageReference Include="DevExpress.Persistent.BaseImpl.Xpo" Version="25.1.6" />
  
  <!-- Blazor XPO -->
  <PackageReference Include="DevExpress.ExpressApp.AuditTrail.Xpo" Version="25.1.6" />
  <PackageReference Include="DevExpress.ExpressApp.Security.Xpo" Version="25.1.6" />
  <PackageReference Include="DevExpress.ExpressApp.WebApi.Xpo" Version="25.1.6" />
</ItemGroup>
```

---

### Example 4: Blazor Web Project (EF Core)

**Detected:** EF Core (has `DevExpress.ExpressApp.EFCore` reference)

**Packages Added:**
```xml
<ItemGroup>
  <!-- Base EF Core -->
  <PackageReference Include="DevExpress.Persistent.BaseImpl.EFCore" Version="25.1.6" />
  
  <!-- Blazor EF Core -->
  <PackageReference Include="DevExpress.ExpressApp.AuditTrail.EFCore" Version="25.1.6" />
  <PackageReference Include="DevExpress.ExpressApp.Security.EFCore" Version="25.1.6" />
  <PackageReference Include="DevExpress.ExpressApp.WebApi.EFCore" Version="25.1.6" />
</ItemGroup>
```

---

## Edge Cases

### 1. No ORM References

**Scenario:** Project has no XPO or EF references

**Behavior:** Assumes XPO (default)

**Reasoning:** XPO is more common, safer default

---

### 2. Mixed References (Rare)

**Scenario:** Project has both XPO and EF references

**Behavior:** Detected as EF (first match wins)

**Reasoning:** EF detection is explicit, migration likely in progress

---

### 3. EF6 vs EF Core

**Both Detected:** Method checks for both:
- `DevExpress.ExpressApp.EF6`
- `DevExpress.ExpressApp.EFCore`

**Packages Added:** Always EF Core packages (modern)

**Reasoning:** Migration to .NET 9+ implies EF Core

---

## Future Enhancements

### 1. Explicit EF Version Detection

```csharp
public enum OrmType {
    Xpo,
    EF6,
    EFCore
}

public static OrmType DetectOrmType(string projectPath) {
    // Distinguish between EF6 and EF Core
}
```

### 2. Console Output

```csharp
Console.WriteLine($"  ORM: {(info.IsEfProject ? "EF Core" : "XPO")}");
```

### 3. Validation

```csharp
if (info.IsEfProject && info.IsWindowsProject) {
    Console.WriteLine("  Warning: EF Core with Windows Forms is rare");
}
```

---

## Testing

### Test Case 1: XPO Detection

**Input:**
```xml
<PackageReference Include="DevExpress.Persistent.Base.v24.1" />
```

**Expected:** `isEfProject = false`  
**Result:** ✅ PASS

---

### Test Case 2: EF Core Detection

**Input:**
```xml
<PackageReference Include="DevExpress.Persistent.BaseImpl.EFCore" />
```

**Expected:** `isEfProject = true`  
**Result:** ✅ PASS

---

### Test Case 3: EF6 Detection

**Input:**
```xml
<Reference Include="DevExpress.ExpressApp.EF6" />
```

**Expected:** `isEfProject = true`  
**Result:** ✅ PASS

---

## Build Verification

```bash
dotnet build
```

**Result:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

✅ **Status:** PASSED

---

## Summary

### What Was Added
- ✅ ORM detection (`IsProjectReferencesEF()`)
- ✅ ORM parameter to `GetPackages()`
- ✅ ORM-specific base packages
- ✅ ORM-specific Blazor packages
- ✅ `IsEfProject` to `ProjectInfo`

### Why It Matters
- ✅ **Correct packages** for each ORM
- ✅ **Automatic detection** - no user input
- ✅ **Prevents errors** - no package conflicts
- ✅ **Supports both** XPO and EF

### Impact
- ✅ No breaking changes
- ✅ Backward compatible (XPO default)
- ✅ Works with existing code
- ✅ Production ready

---

**Status:** ✅ COMPLETE AND TESTED  
**ORM Support:** ✅ XPO + EF6 + EF Core  
**Build:** ✅ Successful  
**Quality:** ✅ Production Ready
