# XafApiConverterTests

This project contains unit tests for the XafApiConverter tool using xUnit framework.

## Structure

```
XafApiConverterTests/
├── ClassCommenterTests.cs                  # Tests for ClassCommenter functionality
└── TestFiles/
    └── ClassCommenterTests/                # Test data files
        ├── CustomIntegerEditor.cs
        ├── CustomIntegerEditor_commented.cs
        ├── CustomLayoutTemplates.cs
        ├── CustomLayoutTemplates_commented.cs
        ├── CustomStringEditor.cs
        ├── CustomStringEditor_commented.cs
        ├── WebModule.cs
        ├── WebModule.Designer.cs
        ├── WebModule_commented.cs
        └── WebModule_commented.Designer.cs
```

## Running Tests

### Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Click "Run All" or select specific tests to run

### Command Line
```bash
dotnet test
```

### Run with coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

Tests are organized using `[Trait]` attributes:

```csharp
[Fact]
[Trait("Category", "ClassCommenter")]
public void TestName() { }
```

### Run specific category
```bash
dotnet test --filter "Category=ClassCommenter"
```

## Test Scenarios

### ClassCommenter Tests

1. **TestCommentOutSingleClass_ASPxPropertyEditor**
   - Tests commenting out a single class inheriting from ASPxPropertyEditor
   - Input: `CustomStringEditor.cs`
   - Expected: `CustomStringEditor_commented.cs`

2. **TestCommentOutMultipleClasses_AllCommented**
   - Tests commenting out multiple classes (all layout templates)
   - Input: `CustomLayoutTemplates.cs`
   - Expected: `CustomLayoutTemplates_commented.cs`

3. **TestCommentOutMixedClasses_SelectiveCommented**
   - Tests selective commenting (some classes safe, some problematic)
   - Input: `CustomIntegerEditor.cs`
   - Expected: `CustomIntegerEditor_commented.cs`

4. **TestPartialClass_ProtectedBaseClass_WarningCommentOnly**
   - Tests partial class with protected base class (ModuleBase)
   - Should add warning comment only, not comment out the class
   - Input: `WebModule.cs`
   - Expected: `WebModule_commented.cs`

5. **TestProtectedBaseClass_NotCommented**
   - Verifies protected base classes are not commented out
   - Only warning comments should be added

6. **TestSafeClass_NotCommented**
   - Verifies safe classes remain untouched
   - WelcomeObject should not be commented

7. **TestCommentFormat_CorrectStructure**
   - Validates comment structure and markers
   - Checks for NOTE, TODO, COMMENTED OUT CLASS markers

8. **TestCommentIncludesDescription**
   - Ensures Description from TypeReplacement is included in comments

9. **TestFileExists_BeforeProcessing** (Theory)
   - Validates test files exist before processing
   - Uses `[Theory]` and `[InlineData]` for multiple test cases

## Adding New Tests

### 1. Add Test Method
```csharp
[Fact]
[Trait("Category", "YourCategory")]
public void YourTestMethod() {
    // Arrange
    var input = "...";
    
    // Act
    var result = ProcessFile(input);
    
    // Assert
    Assert.Equal(expected, result);
}
```

### 2. Add Test Data Files
Place test files in `TestFiles/ClassCommenterTests/`:
- `YourTest.cs` - Input file
- `YourTest_commented.cs` - Expected output

### 3. Theory Tests for Multiple Cases
```csharp
[Theory]
[InlineData("input1.cs", "ExpectedClass1")]
[InlineData("input2.cs", "ExpectedClass2")]
[Trait("Category", "YourCategory")]
public void TestWithMultipleCases(string fileName, string className) {
    // Test logic
}
```

## NuGet Packages

- **xunit** (2.9.3) - xUnit testing framework
- **xunit.runner.visualstudio** (3.1.4) - Visual Studio test runner
- **Microsoft.NET.Test.Sdk** (17.14.1) - .NET test SDK
- **coverlet.collector** (6.0.4) - Code coverage collector

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Test
  run: dotnet test --no-build --verbosity normal
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
```

## Troubleshooting

### Test files not found
- Ensure `TestFiles` directory is copied to output: `CopyToOutputDirectory="PreserveNewest"`
- Check `.csproj` has: `<None Include="TestFiles\**\*.cs" CopyToOutputDirectory="PreserveNewest" />`

### Tests fail on different machines
- Use `Path.Combine()` for file paths
- Normalize line endings in assertions
- Use `NormalizeWhitespace()` helper method

## Best Practices

1. **Arrange-Act-Assert** pattern for clarity
2. **Descriptive test names** indicating what is tested
3. **Use traits** for test categorization
4. **Theory tests** for similar scenarios with different data
5. **Helper methods** to reduce code duplication
6. **Temp files** to avoid modifying test data
7. **Cleanup** in finally blocks or using statements
