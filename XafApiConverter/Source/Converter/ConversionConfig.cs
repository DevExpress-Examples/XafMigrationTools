using System.Collections.Generic;


namespace XafApiConverter.Converter {
    /// <summary>
    /// Configuration for .NET Framework to .NET Core/8+ project conversion
    /// Based on Configuration.md
    /// </summary>
    internal class ConversionConfig {
        public string TargetFramework { get; set; } = "net9.0";
        public string TargetFrameworkWindows { get; set; } = "net9.0-windows";
        public string TargetFrameworkFull { get; set; } = ".NET 9.0";
        public string SourceFramework { get; set; } = "v4.8.1";
        public string SourceFrameworkFull { get; set; } = ".NET Framework 4.8.1";

        // DevExpress Versions
        public string DxPackageVersion { get; set; } = "25.1.6";
        public string DxAssemblyVersion { get; set; } = "v25.1";
        public bool UseDirectoryPackages { get; set; } = false;

        // Package Versions
        public Dictionary<string, string> PackageVersions { get; } = new Dictionary<string, string> {
            // Microsoft
            { "VER_MS_EXTENSIONS", "9.0.0" },
            { "VER_MS_CODEANALYSIS", "4.10.0" },
            { "VER_MS_SQLCLIENT", "5.2.2" },
            { "VER_MS_ASPNETCORE_ODATA", "9.3.2" },

            // Azure
            { "VER_AZURE_IDENTITY", "1.17.1" },
            { "VER_MS_IDENTITY_CLIENT", "4.78.0" },
            { "VER_MS_IDENTITY_PROTOCOLS", "8.14.0" },

            // System
            { "VER_SYSTEM_TEXT_JSON", "9.0.6" },
            { "VER_SYSTEM_CONFIG_MANAGER", "9.0.0" },
            { "VER_SYSTEM_SECURITY_ACCESSCONTROL", "6.0.1" },
            { "VER_SYSTEM_DRAWING_COMMON", "8.0.15" },
            { "VER_SYSTEM_CODEDOM", "8.0.0" },
            { "VER_SYSTEM_REACTIVE", "6.0.1" },
            { "VER_SYSTEM_SECURITY_PERMISSIONS", "8.0.0" },

            // Other
            { "VER_DEVEXTREME_ASPNET_DATA", "5.1.0" },
            { "VER_SWASHBUCKLE_ASPNETCORE", "6.9.0" },
            { "VER_NETCORE_PLATFORMS", "7.0.4" }
        };

        public static ConversionConfig Default => new ConversionConfig();

        public static ConversionConfig ForNet10() => new ConversionConfig {
            TargetFramework = "net10.0",
            TargetFrameworkWindows = "net10.0-windows",
            TargetFrameworkFull = ".NET 10.0"
        };

        public static ConversionConfig ForDevExpress26() => new ConversionConfig {
            DxPackageVersion = "26.1.6",
            DxAssemblyVersion = "v26.1"
        };
    }
}
