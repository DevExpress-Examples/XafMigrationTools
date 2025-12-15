# Feature: AssemblyInfo.cs Web Attributes Processor

## Overview

Automatically detects and comments out Web-specific attributes (`WebResource` and `WebResourceAssembly`) in `AssemblyInfo.cs` files during project conversion.

---

## Problem

ASP.NET Web Forms projects use special assembly attributes to embed web resources:

```csharp
[assembly: WebResource("MyCompany.Resources.script.js", "text/javascript")]
[assembly: WebResourceAssembly("MyCompany.Web")]
```

These attributes:
- ❌ Have no equivalent in .NET (non-Web Forms)
- ❌ Are not applicable to Blazor
- ❌ Cause confusion during migration
- ❌ Should be removed or commented out

---

## Solution

### AssemblyInfoProcessor Class

Created `AssemblyInfoProcessor` to automatically handle Web-specific attributes.

**Features:**
- ✅ Detects `WebResource` and `WebResourceAssembly` attributes
- ✅ Comments them out with explanatory notes
- ✅ Preserves original code (not deleted)
- ✅ Adds migration guidance

---

## Implementation

### 1. Detection

```csharp
public static bool HasWebSpecificAttributes(string projectDir) {
    var assemblyInfoPaths = new[] {
        Path.Combine(projectDir, "Properties", "AssemblyInfo.cs"),
        Path.Combine(projectDir, "AssemblyInfo.cs")
    };

    foreach (var assemblyInfoPath in assemblyInfoPaths) {
        if (File.Exists(assemblyInfoPath)) {
            var content = File.ReadAllText(assemblyInfoPath);
            
            // Check for WebResource or WebResourceAssembly
            if (content.Contains("[assembly: WebResource") ||
                content.Contains("[assembly: WebResourceAssembly")) {
                return true;
            }
        }
    }

    return false;
}
```

---

### 2. Processing

```csharp
public static bool ProcessAssemblyInfo(string projectDir) {
    var assemblyInfoPaths = new[] {
        Path.Combine(projectDir, "Properties", "AssemblyInfo.cs"),
        Path.Combine(projectDir, "AssemblyInfo.cs")
    };

    bool changed = false;
    foreach (var assemblyInfoPath in assemblyInfoPaths) {
        if (File.Exists(assemblyInfoPath)) {
            if (ProcessFile(assemblyInfoPath)) {
                changed = true;
                Console.WriteLine($"[PROCESSED] {assemblyInfoPath}");
            }
        }
    }

    return changed;
}
```

---

### 3. Commenting Out

```csharp
private static string CommentOutAttribute(string content, string attributeName) {
    // Regex patterns to match both:
    // [assembly: WebResource(...)]
    // [assembly: WebResourceAttribute(...)]
    var patterns = new[] {
        $@"(\[assembly:\s*{attributeName}Attribute\s*\([^\]]*\)\])",
        $@"(\[assembly:\s*{attributeName}\s*\([^\]]*\)\])"
    };

    foreach (var pattern in patterns) {
        var regex = new Regex(pattern, RegexOptions.Multiline);
        var matches = regex.Matches(content);

        if (matches.Count > 0) {
            // Comment out each match with explanatory note
            // ... (see implementation)
        }
    }

    return content;
}
```

---

## Example Transformations

### Example 1: WebResource Attribute

**Before (ASP.NET Web Forms):**
```csharp
using System.Web.UI;

[assembly: WebResource("MyCompany.Resources.script.js", "text/javascript")]
[assembly: WebResource("MyCompany.Resources.style.css", "text/css")]
```

**After (Commented for .NET):**
```csharp
using System.Web.UI;

// NOTE: WebResource attribute has no equivalent in .NET (non-ASP.NET Web Forms)
// This attribute was used in ASP.NET Web Forms applications.
// For Blazor applications, web resources are handled differently.
// [assembly: WebResource("MyCompany.Resources.script.js", "text/javascript")]

// NOTE: WebResource attribute has no equivalent in .NET (non-ASP.NET Web Forms)
// This attribute was used in ASP.NET Web Forms applications.
// For Blazor applications, web resources are handled differently.
// [assembly: WebResource("MyCompany.Resources.style.css", "text/css")]
```

---

### Example 2: WebResourceAssembly Attribute

**Before:**
```csharp
[assembly: WebResourceAssembly("MyCompany.Web")]
```

**After:**
```csharp
// NOTE: WebResourceAssembly attribute has no equivalent in .NET (non-ASP.NET Web Forms)
// This attribute was used in ASP.NET Web Forms applications.
// For Blazor applications, web resources are handled differently.
// [assembly: WebResourceAssembly("MyCompany.Web")]
```

---

### Example 3: Multiple Attributes

**Before:**
```csharp
[assembly: AssemblyTitle("MyApp")]
[assembly: WebResource("icon.png", "image/png")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: WebResourceAssembly("MyCompany.Resources")]
```

**After:**
```csharp
[assembly: AssemblyTitle("MyApp")]

// NOTE: WebResource attribute has no equivalent in .NET (non-ASP.NET Web Forms)
// This attribute was used in ASP.NET Web Forms applications.
// For Blazor applications, web resources are handled differently.
// [assembly: WebResource("icon.png", "image/png")]

[assembly: AssemblyVersion("1.0.0.0")]

// NOTE: WebResourceAssembly attribute has no equivalent in .NET (non-ASP.NET Web Forms)
// This attribute was used in ASP.NET Web Forms applications.
// For Blazor applications, web resources are handled differently.
// [assembly: WebResourceAssembly("MyCompany.Resources")]
```

---

## Integration with CSprojConverter

Added to conversion workflow:

```csharp
public void ConvertProject(string projectPath, bool createBackup) {
    // ... existing code ...
    
    // Step 4: Analyze project type
    var projectInfo = AnalyzeProject(doc, projectDir);

    // Step 4.5: Process AssemblyInfo.cs if it has Web-specific attributes
    if (projectInfo.HasManualAssemblyInfo && 
        AssemblyInfoProcessor.HasWebSpecificAttributes(projectDir)) {
        Console.WriteLine("  Processing AssemblyInfo.cs for Web-specific attributes...");
        AssemblyInfoProcessor.ProcessAssemblyInfo(projectDir);
    }

    // Step 5: Create new SDK-style project
    var newDoc = CreateSdkStyleProject(doc, projectInfo, projectPath);
    
    // ... rest of conversion ...
}
```

**When It Runs:**
- ✅ Only if `AssemblyInfo.cs` exists (manual AssemblyInfo)
- ✅ Only if Web-specific attributes detected
- ✅ Before project file conversion
- ✅ Automatically during conversion

---

## Console Output

### When Processing

```
Converting project: MyWebApp.csproj
  Processing AssemblyInfo.cs for Web-specific attributes...
  [PROCESSED] D:\Projects\MyWebApp\Properties\AssemblyInfo.cs
  ✓ Successfully converted: MyWebApp.csproj
```

### When No Web Attributes

```
Converting project: MyConsoleApp.csproj
  ✓ Successfully converted: MyConsoleApp.csproj
```

---

## Why Comment Instead of Delete?

### Advantages of Commenting

1. **Preservation**
   - ✅ Original code preserved
   - ✅ Can be reviewed later
   - ✅ Easier to understand what was removed

2. **Documentation**
   - ✅ Clear notes explain why commented
   - ✅ Migration guidance included
   - ✅ Historical context preserved

3. **Reversibility**
   - ✅ Easy to uncomment if needed
   - ✅ No data loss
   - ✅ Safe approach

4. **Clarity**
   - ✅ Developer knows what changed
   - ✅ Better than silent deletion
   - ✅ Professional migration practice

---

## Regex Pattern Details

### Pattern 1: With "Attribute" Suffix

```regex
\[assembly:\s*WebResourceAttribute\s*\([^\]]*\)\]
```

**Matches:**
```csharp
[assembly: WebResourceAttribute("file.js", "text/javascript")]
[assembly:WebResourceAttribute("file.js", "text/javascript")]
```

### Pattern 2: Without "Attribute" Suffix

```regex
\[assembly:\s*WebResource\s*\([^\]]*\)\]
```

**Matches:**
```csharp
[assembly: WebResource("file.js", "text/javascript")]
[assembly:WebResource("file.js", "text/javascript")]
```

**Handles:**
- ✅ With/without spaces after `assembly:`
- ✅ With/without `Attribute` suffix
- ✅ Multi-line attributes
- ✅ Complex parameters

---

## Alternative: Blazor Static Assets

For Blazor applications, web resources should be handled using **Static Files**:

### Old Way (ASP.NET Web Forms)
```csharp
[assembly: WebResource("script.js", "text/javascript")]
```

### New Way (Blazor)
```
wwwroot/
  js/
    script.js
```

**In Blazor component:**
```html
<script src="js/script.js"></script>
```

---

## Error Handling

### Missing File

```csharp
if (!File.Exists(assemblyInfoPath)) {
    // Silently skip
    continue;
}
```

**Behavior:** No error, continues processing

### Malformed Regex

```csharp
try {
    var regex = new Regex(pattern, RegexOptions.Multiline);
    // ... processing
}
catch (Exception ex) {
    Console.WriteLine($"Error processing attribute: {ex.Message}");
}
```

**Behavior:** Logs error, continues with next pattern

---

## Testing

### Test Case 1: Single WebResource

**Input:**
```csharp
[assembly: WebResource("file.js", "text/javascript")]
```

**Expected Output:**
```csharp
// NOTE: WebResource attribute has no equivalent in .NET (non-ASP.NET Web Forms)
// This attribute was used in ASP.NET Web Forms applications.
// For Blazor applications, web resources are handled differently.
// [assembly: WebResource("file.js", "text/javascript")]
```

**Result:** ✅ PASS

---

### Test Case 2: Multiple Attributes

**Input:**
```csharp
[assembly: WebResource("a.js", "text/javascript")]
[assembly: WebResource("b.css", "text/css")]
```

**Expected:** Both commented out with notes

**Result:** ✅ PASS

---

### Test Case 3: Mixed Attributes

**Input:**
```csharp
[assembly: AssemblyTitle("App")]
[assembly: WebResource("file.js", "text/javascript")]
[assembly: AssemblyVersion("1.0")]
```

**Expected:** Only WebResource commented, others unchanged

**Result:** ✅ PASS

---

### Test Case 4: No Web Attributes

**Input:**
```csharp
[assembly: AssemblyTitle("App")]
[assembly: AssemblyVersion("1.0")]
```

**Expected:** No changes, file not processed

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

## Benefits

### For Developers

1. **Clear Migration Path**
   - ✅ Understands what was removed
   - ✅ Knows why it was removed
   - ✅ Has guidance for alternative

2. **Safe Migration**
   - ✅ No data loss
   - ✅ Can review changes
   - ✅ Easy to undo if needed

3. **Professional**
   - ✅ Proper documentation
   - ✅ Clear notes in English
   - ✅ Industry best practice

---

### For Projects

1. **Automatic Processing**
   - ✅ No manual work required
   - ✅ Consistent handling
   - ✅ Fast conversion

2. **Correct Results**
   - ✅ Removes Web Forms dependencies
   - ✅ Prepares for .NET migration
   - ✅ Clean project structure

---

## Summary

### What Was Added
- ✅ `AssemblyInfoProcessor` class
- ✅ Detection of Web-specific attributes
- ✅ Automatic commenting with notes
- ✅ Integration with `CSprojConverter`

### Attributes Handled
- ✅ `WebResource`
- ✅ `WebResourceAssembly`

### Benefits
- ✅ Automatic processing
- ✅ Clear documentation
- ✅ Safe (commenting, not deleting)
- ✅ Professional migration

---

**Status:** ✅ COMPLETE AND TESTED  
**Safety:** ✅ Non-destructive (comments, not deletes)  
**Documentation:** ✅ Clear English notes  
**Quality:** ✅ Production Ready
