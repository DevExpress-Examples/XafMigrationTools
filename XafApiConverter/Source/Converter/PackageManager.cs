using System;
using System.Collections.Generic;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Manages NuGet package references for project conversion
    /// Implements rules from Convert_to_NET.md APPENDIX A and B
    /// </summary>
    internal class PackageManager {
        private readonly ConversionConfig _config;

        public PackageManager(ConversionConfig config = null) {
            _config = config ?? ConversionConfig.Default;
        }

        /// <summary>
        /// Get all required packages based on project type
        /// </summary>
        public List<PackageReference> GetPackages(bool isWindowsProject, bool isWebProject) {
            var packages = new List<PackageReference>();

            // BASE packages (always included)
            AddBasePackages(packages);

            // WINDOWS packages
            if (isWindowsProject) {
                AddWindowsPackages(packages);
            }

            // BLAZOR_WEB packages
            if (isWebProject) {
                AddBlazorWebPackages(packages);
            }

            // Deduplicate (APPENDIX B rules)
            return DeduplicatePackages(packages);
        }

        private void AddBasePackages(List<PackageReference> packages) {
            var dx = _config.DxPackageVersion;

            // DevExpress packages
            packages.AddRange(new[] {
                new PackageReference("DevExpress.ExpressApp.CodeAnalysis", dx),
                new PackageReference("DevExpress.ExpressApp.CloneObject", dx),
                new PackageReference("DevExpress.ExpressApp.ConditionalAppearance", dx),
                new PackageReference("DevExpress.ExpressApp.TreeListEditors", dx),
                new PackageReference("DevExpress.ExpressApp.Office", dx),
                new PackageReference("DevExpress.ExpressApp.PivotChart", dx),
                new PackageReference("DevExpress.ExpressApp.ReportsV2", dx),
                new PackageReference("DevExpress.ExpressApp.Security", dx),
                new PackageReference("DevExpress.ExpressApp.Validation", dx),
                new PackageReference("DevExpress.ExpressApp.ViewVariantsModule", dx),
                new PackageReference("DevExpress.Persistent.BaseImpl.Xpo", dx)
            });

            // Microsoft packages
            packages.AddRange(new[] {
                new PackageReference("Microsoft.CodeAnalysis.CSharp", _config.PackageVersions["VER_MS_CODEANALYSIS"]),
                new PackageReference("Microsoft.Extensions.Configuration.Abstractions", _config.PackageVersions["VER_MS_EXTENSIONS"]),
                new PackageReference("Microsoft.Extensions.DependencyInjection.Abstractions", _config.PackageVersions["VER_MS_EXTENSIONS"]),
                new PackageReference("Microsoft.Extensions.DependencyInjection", _config.PackageVersions["VER_MS_EXTENSIONS"]),
                new PackageReference("Microsoft.Extensions.Options", _config.PackageVersions["VER_MS_EXTENSIONS"]),
                new PackageReference("Microsoft.Data.SqlClient", _config.PackageVersions["VER_MS_SQLCLIENT"]),
                new PackageReference("Microsoft.IdentityModel.Protocols.OpenIdConnect", _config.PackageVersions["VER_MS_IDENTITY_PROTOCOLS"])
            });

            // Azure packages
            packages.AddRange(new[] {
                new PackageReference("Azure.Identity", _config.PackageVersions["VER_AZURE_IDENTITY"]),
                new PackageReference("Microsoft.Identity.Client", _config.PackageVersions["VER_MS_IDENTITY_CLIENT"])
            });

            // System packages
            packages.AddRange(new[] {
                new PackageReference("System.Configuration.ConfigurationManager", _config.PackageVersions["VER_SYSTEM_CONFIG_MANAGER"]),
                new PackageReference("System.IdentityModel.Tokens.Jwt", _config.PackageVersions["VER_MS_IDENTITY_PROTOCOLS"]),
                new PackageReference("Microsoft.NETCore.Platforms", _config.PackageVersions["VER_NETCORE_PLATFORMS"]),
                new PackageReference("System.Security.AccessControl", _config.PackageVersions["VER_SYSTEM_SECURITY_ACCESSCONTROL"]),
                new PackageReference("System.Text.Json", _config.PackageVersions["VER_SYSTEM_TEXT_JSON"])
            });
        }

        private void AddWindowsPackages(List<PackageReference> packages) {
            var dx = _config.DxPackageVersion;

            packages.AddRange(new[] {
                new PackageReference("DevExpress.ExpressApp.Security.Xpo.Extensions.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.Win.Design", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.ReportsV2.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.Notifications.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.Office.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.Validation.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.PivotChart.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.ScriptRecorder.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.FileAttachment.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.Scheduler.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.ExpressApp.TreeListEditors.Win", dx, PackageSet.Windows),
                new PackageReference("DevExpress.Win.Demos", dx, PackageSet.Windows)
            });
        }

        private void AddBlazorWebPackages(List<PackageReference> packages) {
            var dx = _config.DxPackageVersion;

            // DevExtreme
            packages.Add(new PackageReference("DevExtreme.AspNet.Data", 
                _config.PackageVersions["VER_DEVEXTREME_ASPNET_DATA"], PackageSet.BlazorWeb));

            // Microsoft ASP.NET
            packages.AddRange(new[] {
                new PackageReference("Microsoft.AspNetCore.OData", _config.PackageVersions["VER_MS_ASPNETCORE_ODATA"], PackageSet.BlazorWeb),
                new PackageReference("Microsoft.Extensions.DependencyModel", _config.PackageVersions["VER_MS_EXTENSIONS"], PackageSet.BlazorWeb)
            });

            // Swagger
            packages.AddRange(new[] {
                new PackageReference("Swashbuckle.AspNetCore", _config.PackageVersions["VER_SWASHBUCKLE_ASPNETCORE"], PackageSet.BlazorWeb),
                new PackageReference("Swashbuckle.AspNetCore.Annotations", _config.PackageVersions["VER_SWASHBUCKLE_ASPNETCORE"], PackageSet.BlazorWeb)
            });

            // System
            packages.AddRange(new[] {
                new PackageReference("System.CodeDom", _config.PackageVersions["VER_SYSTEM_CODEDOM"], PackageSet.BlazorWeb),
                new PackageReference("System.Drawing.Common", _config.PackageVersions["VER_SYSTEM_DRAWING_COMMON"], PackageSet.BlazorWeb),
                new PackageReference("System.Reactive", _config.PackageVersions["VER_SYSTEM_REACTIVE"], PackageSet.BlazorWeb),
                new PackageReference("System.Security.Permissions", _config.PackageVersions["VER_SYSTEM_SECURITY_PERMISSIONS"], PackageSet.BlazorWeb)
            });

            // DevExpress Blazor
            packages.AddRange(new[] {
                new PackageReference("DevExpress.ExpressApp.Notifications.Blazor", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.AuditTrail.Xpo", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.ReportsV2.Blazor", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.Scheduler.Blazor", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.FileAttachment.Blazor", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.Office.Blazor", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.Validation.Blazor", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.Security.Xpo", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.ExpressApp.WebApi.Xpo", dx, PackageSet.BlazorWeb),
                new PackageReference("DevExpress.Drawing.Skia", dx, PackageSet.BlazorWeb)
            });
        }

        /// <summary>
        /// Deduplicate packages according to APPENDIX B rules:
        /// Priority: WINDOWS > BLAZOR_WEB > BASE
        /// </summary>
        private List<PackageReference> DeduplicatePackages(List<PackageReference> packages) {
            return packages
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(p => p.Set).First())
                .OrderBy(p => p.Name)
                .ToList();
        }

        /// <summary>
        /// Migrate Web packages to Blazor (TRANS-008)
        /// </summary>
        public static string MigrateWebToBlazor(string packageName) {
            if (packageName.Contains(".Web", StringComparison.OrdinalIgnoreCase) &&
                packageName.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase)) {
                return packageName.Replace(".Web", ".Blazor");
            }
            return packageName;
        }
    }

    internal enum PackageSet {
        Base = 1,
        BlazorWeb = 2,
        Windows = 3
    }

    internal class PackageReference {
        public string Name { get; }
        public string Version { get; }
        public PackageSet Set { get; }

        public PackageReference(string name, string version, PackageSet set = PackageSet.Base) {
            Name = name;
            Version = version;
            Set = set;
        }

        public override string ToString() => $"{Name} v{Version} [{Set}]";
    }
}
