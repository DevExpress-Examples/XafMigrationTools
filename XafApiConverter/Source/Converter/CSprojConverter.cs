using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Converts legacy .NET Framework .csproj files to SDK-style format
    /// Implements rules from Convert_to_NET.md
    /// </summary>
    internal class CSprojConverter {
        private readonly ConversionConfig _config;
        private readonly PackageManager _packageManager;

        public CSprojConverter(ConversionConfig config = null) {
            _config = config ?? ConversionConfig.Default;
            _packageManager = new PackageManager(_config);
        }

        ///// <summary>
        ///// Convert a Roslyn Project to SDK-style format
        ///// </summary>
        //public static void Convert(Project project) {
        //    var converter = new CSprojConverter();
        //    converter.ConvertProject(project.FilePath);
        //}

        /// <summary>
        /// Convert a project file by path
        /// </summary>
        public void ConvertProject(string projectPath, bool createBackup) {
            if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath)) {
                Console.WriteLine($"Project file not found: {projectPath}");
                return;
            }

            Console.WriteLine($"Converting project: {projectPath}");

            try {
                // Step 1: Read the original project file
                var originalContent = File.ReadAllText(projectPath);
                var projectDir = Path.GetDirectoryName(projectPath);

                // Step 2: Check if already SDK-style
                if (IsSdkStyleProject(originalContent)) {
                    Console.WriteLine($"Project is already SDK-style: {projectPath}");
                    return;
                }

                // Step 3: Parse the project file
                var doc = XDocument.Parse(originalContent);

                // Step 4: Analyze project type
                var projectInfo = AnalyzeProject(doc, projectDir);

                // Step 5: Create new SDK-style project
                var newDoc = CreateSdkStyleProject(doc, projectInfo, projectPath);

                var backupPath = projectPath + ".backup";
                // Step 6: Backup original file
                if (createBackup) {
                    File.Copy(projectPath, backupPath, true);
                    Console.WriteLine($"Backup created at: {backupPath}");
                }

                // Step 7: Save the new project file
                SaveProject(newDoc, projectPath);

                Console.WriteLine($"✓ Successfully converted: {projectPath}");
                if(createBackup) {
                    Console.WriteLine($"  Backup saved to: {backupPath}");
                }
                Console.WriteLine($"  Project type: {GetProjectTypeDescription(projectInfo)}");
            }
            catch (Exception ex) {
                Console.WriteLine($"✗ Error converting project {projectPath}: {ex.Message}");
                throw;
            }
        }

        private bool IsSdkStyleProject(string content) {
            return content.Contains("<Project Sdk=", StringComparison.OrdinalIgnoreCase);
        }

        private ProjectInfo AnalyzeProject(XDocument doc, string projectDir) {
            var info = new ProjectInfo {
                IsWindowsProject = DetectWindowsProject(doc),
                IsWebProject = DetectWebProject(doc, projectDir),
                RootNamespace = ExtractProperty(doc, "RootNamespace"),
                AssemblyName = ExtractProperty(doc, "AssemblyName"),
                HasManualAssemblyInfo = HasManualAssemblyInfo(projectDir)
            };

            return info;
        }

        private bool DetectWindowsProject(XDocument doc) {
            // TRANS-002: Check for Windows-specific DevExpress references
            var references = doc.Descendants()
                .Where(e => e.Name.LocalName == "Reference" || e.Name.LocalName == "PackageReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v != null);

            return references.Any(r => 
                r.Contains("DevExpress.ExpressApp.Win") || 
                r.Contains($"DevExpress.ExpressApp.Win.{_config.DxAssemblyVersion}"));
        }

        private bool DetectWebProject(XDocument doc, string projectDir) {
            // Check for Global.asax.cs
            if (File.Exists(Path.Combine(projectDir, "Global.asax.cs"))) {
                return true;
            }

            // Check project name
            var projectName = Path.GetFileName(projectDir);
            return projectName != null && 
                   (projectName.Contains(".Web") || projectName.Contains(".Blazor"));
        }

        private XDocument CreateSdkStyleProject(XDocument originalDoc, ProjectInfo info, string projectPath) {
            // TRANS-001: SDK-Style Conversion
            var project = new XElement("Project");
            project.SetAttributeValue("Sdk", "Microsoft.NET.Sdk");

            // Add main PropertyGroup
            var propertyGroup = CreateMainPropertyGroup(info);
            project.Add(propertyGroup);

            // TRANS-005: Add NuGet packages
            AddPackageReferences(project, info);

            // TRANS-007: Add custom embedded resources
            AddCustomEmbeddedResources(project, originalDoc, Path.GetDirectoryName(projectPath));

            // Add other custom items
            AddCustomItems(project, originalDoc);

            return new XDocument(new XDeclaration("1.0", "utf-8", null), project);
        }

        private XElement CreateMainPropertyGroup(ProjectInfo info) {
            var propertyGroup = new XElement("PropertyGroup");

            // TRANS-002: Target Framework Selection
            var targetFramework = info.IsWindowsProject 
                ? _config.TargetFrameworkWindows 
                : _config.TargetFramework;
            propertyGroup.Add(new XElement("TargetFramework", targetFramework));

            // Add namespace and assembly name if present
            if (!string.IsNullOrEmpty(info.RootNamespace)) {
                propertyGroup.Add(new XElement("RootNamespace", info.RootNamespace));
            }
            if (!string.IsNullOrEmpty(info.AssemblyName)) {
                propertyGroup.Add(new XElement("AssemblyName", info.AssemblyName));
            }

            // TRANS-003: Windows Desktop Properties
            if (info.IsWindowsProject) {
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
                propertyGroup.Add(new XElement("ImportWindowsDesktopTargets", "true"));
            }

            // TRANS-006: AssemblyInfo.cs Handling
            if (info.HasManualAssemblyInfo) {
                propertyGroup.Add(new XElement("GenerateAssemblyInfo", "false"));
            }

            return propertyGroup;
        }


        private void AddPackageReferences(XElement project, ProjectInfo info) {
            var packages = _packageManager.GetPackages(info.IsWindowsProject, info.IsWebProject);

            if (packages.Any()) {
                var itemGroup = new XElement("ItemGroup");
                
                foreach (var package in packages) {
                    var packageRef = new XElement("PackageReference");
                    packageRef.SetAttributeValue("Include", package.Name);
                    
                    if (!_config.UseDirectoryPackages) {
                        packageRef.SetAttributeValue("Version", package.Version);
                    }
                    
                    itemGroup.Add(packageRef);
                }

                project.Add(itemGroup);
            }
        }

        private void AddCustomEmbeddedResources(XElement project, XDocument originalDoc, string projectDir) {
            // TRANS-007: Keep only non-.resx EmbeddedResources and standalone .resx files
            var embeddedResources = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "EmbeddedResource")
                .ToList();

            var customResources = new List<XElement>();

            foreach (var resource in embeddedResources) {
                var include = resource.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(include)) continue;

                // Keep non-.resx files (xml, pdf, svg, etc)
                if (!include.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)) {
                    customResources.Add(new XElement(resource));
                    continue;
                }

                // Skip .resx files with DependentUpon (auto-included by SDK)
                var hasDependentUpon = resource.Descendants()
                    .Any(d => d.Name.LocalName == "DependentUpon");
                
                if (!hasDependentUpon) {
                    // Keep standalone .resx files
                    customResources.Add(new XElement(resource));
                }
            }

            if (customResources.Any()) {
                var itemGroup = new XElement("ItemGroup");
                foreach (var resource in customResources) {
                    itemGroup.Add(resource);
                }
                project.Add(itemGroup);
            }
        }

        private void AddCustomItems(XElement project, XDocument originalDoc) {
            // Add other custom items that SDK doesn't auto-include
            var customItems = new List<XElement>();

            // Get None items with special metadata
            var noneItems = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "None" && e.HasElements)
                .ToList();

            // Get Content items with special properties
            var contentItems = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "Content" && e.HasElements)
                .ToList();

            customItems.AddRange(noneItems);
            customItems.AddRange(contentItems);

            if (customItems.Any()) {
                var itemGroup = new XElement("ItemGroup");
                foreach (var item in customItems) {
                    itemGroup.Add(new XElement(item));
                }
                project.Add(itemGroup);
            }
        }

        private string ExtractProperty(XDocument doc, string propertyName) {
            return doc.Descendants()
                .Where(e => e.Name.LocalName == propertyName)
                .Select(e => e.Value)
                .FirstOrDefault();
        }

        private bool HasManualAssemblyInfo(string projectDir) {
            // TRANS-006: Check for manual AssemblyInfo.cs
            var assemblyInfoPath1 = Path.Combine(projectDir, "Properties", "AssemblyInfo.cs");
            var assemblyInfoPath2 = Path.Combine(projectDir, "AssemblyInfo.cs");
            return File.Exists(assemblyInfoPath1) || File.Exists(assemblyInfoPath2);
        }

        private void SaveProject(XDocument doc, string projectPath) {
            var settings = new System.Xml.XmlWriterSettings {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false
            };

            using (var writer = System.Xml.XmlWriter.Create(projectPath, settings)) {
                doc.Save(writer);
            }
        }

        private string GetProjectTypeDescription(ProjectInfo info) {
            var types = new List<string>();
            if (info.IsWindowsProject) types.Add("Windows");
            if (info.IsWebProject) types.Add("Web/Blazor");
            return types.Any() ? string.Join(", ", types) : "Console/Library";
        }

        private class ProjectInfo {
            public bool IsWindowsProject { get; set; }
            public bool IsWebProject { get; set; }
            public string RootNamespace { get; set; }
            public string AssemblyName { get; set; }
            public bool HasManualAssemblyInfo { get; set; }
        }
    }
}
