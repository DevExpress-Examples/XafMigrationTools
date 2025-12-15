using System;
using System.Collections.Generic;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Type and namespace replacement mappings from UpdateTypes.md
    /// TRANS-006, TRANS-007, TRANS-008, TRANS-009
    /// </summary>
    internal class TypeReplacementMap {
        /// <summary>
        /// Namespace replacements (TRANS-006, TRANS-007)
        /// </summary>
        public static readonly Dictionary<string, NamespaceReplacement> NamespaceReplacements = new() {
            // TRANS-006: SqlClient
            { "System.Data.SqlClient", new NamespaceReplacement(
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "SqlClient namespace migration",
                new[] { ".cs" }) },


            // TRANS-007: DevExpress Web ? Blazor
            { "DevExpress.ExpressApp.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web",
                "DevExpress.ExpressApp.Blazor",
                "Web to Blazor migration",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Editors", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "Web editors to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Templates", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Templates",
                "DevExpress.ExpressApp.Blazor.Templates",
                "Web templates to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Layout", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Layout",
                "DevExpress.ExpressApp.Blazor.Layout",
                "Web layout to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Editors.ASPx", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Editors.ASPx",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPx editors to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.SystemModule", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.Blazor.SystemModule",
                "Web system module to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Validation.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Validation.Web",
                "DevExpress.ExpressApp.Validation.Blazor",
                "Validation Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Scheduler.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Scheduler.Web",
                "DevExpress.ExpressApp.Scheduler.Blazor",
                "Scheduler Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Office.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Office.Web",
                "DevExpress.ExpressApp.Office.Blazor",
                "Office Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.ReportsV2.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.ReportsV2.Web",
                "DevExpress.ExpressApp.ReportsV2.Blazor",
                "Reports Web to Blazor",
                new[] { ".cs", ".xafml" }) }
        };

        /// <summary>
        /// Namespace that have NO Blazor equivalent and should be commented out
        /// </summary>
        public static readonly Dictionary<string, NamespaceReplacement> NoEquivalentNamespaces = new() {
            { "DevExpress.ExpressApp.PivotChart.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.PivotChart.Web",
                null,
                "PivotChart.Web has no Blazor equivalent",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Maps.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "Maps.Web has no Blazor equivalent",
                new[] { ".cs", ".xafml" }) },

            //TODO: Revisit if Kpi gets a .NET equivalent in the future
            //{ "DevExpress.ExpressApp.Kpi", new NamespaceReplacement(
            //    "DevExpress.ExpressApp.Kpi",
            //    null,
            //    "DevExpress.ExpressApp.Kpi has no .NET equivalent",
            //    new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.ScriptRecorder.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.ScriptRecorder.Web",
                null,
                "ScriptRecorder.Web has no Blazor equivalent",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.ScriptRecorder", new NamespaceReplacement(
                "DevExpress.ExpressApp.ScriptRecorder",
                null,
                "ScriptRecorder has no .NET equivalent",
                new[] { ".cs", ".xafml" }) },

            // NEW: Additional no-equivalent namespaces
            { "System.Web.UI.WebControls", new NamespaceReplacement(
                "System.Web.UI.WebControls",
                null,
                "System.Web.UI.WebControls has no equivalent in .NET (Web Forms specific)",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Web.TestScripts", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.TestScripts",
                null,
                "DevExpress.ExpressApp.Web.TestScripts has no Blazor equivalent",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Web.Templates.ActionContainers", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Templates.ActionContainers",
                null,
                "DevExpress.ExpressApp.Web.Templates.ActionContainers has no Blazor equivalent",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Web.Templates.ActionContainers.Menu", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Templates.ActionContainers.Menu",
                null,
                "DevExpress.ExpressApp.Web.Templates.ActionContainers.Menu has no Blazor equivalent",
                new[] { ".cs" }) }
        };

        /// <summary>
        /// Type replacements (TRANS-008)
        /// </summary>
        public static readonly Dictionary<string, TypeReplacement> TypeReplacements = new() {
            // Application Types
            { "WebApplication", new TypeReplacement(
                "WebApplication",
                "BlazorApplication",
                "DevExpress.ExpressApp.Web",
                "DevExpress.ExpressApp.Blazor",
                "Web to Blazor application") },

            // Editor Types
            { "ASPxGridListEditor", new TypeReplacement(
                "ASPxGridListEditor",
                "DxGridListEditor",
                "DevExpress.ExpressApp.Web.Editors.ASPx",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPx grid to Dx grid") },

            { "ASPxLookupPropertyEditor", new TypeReplacement(
                "ASPxLookupPropertyEditor",
                "LookupPropertyEditor",
                "DevExpress.ExpressApp.Web.Editors.ASPx",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPx lookup to Blazor lookup") },

            // Controller Types
            { "WebModificationsController", new TypeReplacement(
                "WebModificationsController",
                "BlazorModificationsController",
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.Blazor.SystemModule",
                "Web modifications controller to Blazor") },

            // NEW: ListView Controller replacement
            { "ListViewController", new TypeReplacement(
                "ListViewController",
                "ListViewControllerBase",
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.SystemModule",
                "Web ListViewController to base ListViewControllerBase") },

            // Module Types
            { "SystemAspNetModule", new TypeReplacement(
                "SystemAspNetModule",
                "SystemBlazorModule",
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.Blazor.SystemModule",
                "System ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "ValidationAspNetModule", new TypeReplacement(
                "ValidationAspNetModule",
                "ValidationBlazorModule",
                "DevExpress.ExpressApp.Validation.Web",
                "DevExpress.ExpressApp.Validation.Blazor",
                "Validation ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "SchedulerAspNetModule", new TypeReplacement(
                "SchedulerAspNetModule",
                "SchedulerBlazorModule",
                "DevExpress.ExpressApp.Scheduler.Web",
                "DevExpress.ExpressApp.Scheduler.Blazor",
                "Scheduler ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "OfficeAspNetModule", new TypeReplacement(
                "OfficeAspNetModule",
                "OfficeBlazorModule",
                "DevExpress.ExpressApp.Office.Web",
                "DevExpress.ExpressApp.Office.Blazor",
                "Office ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "ReportsAspNetModuleV2", new TypeReplacement(
                "ReportsAspNetModuleV2",
                "ReportsBlazorModuleV2",
                "DevExpress.ExpressApp.ReportsV2.Web",
                "DevExpress.ExpressApp.ReportsV2.Blazor",
                "Reports ASP.NET to Blazor",
                new[] { ".cs" }) }
        };

        /// <summary>
        /// Types with NO XAF .NET equivalent (TRANS-009)
        /// These types have no equivalents in XAF .NET and require commenting out entire classes
        /// </summary>
        public static readonly Dictionary<string, TypeReplacement> NoEquivalentTypes = new() {
            { "AnalysisControlWeb", new TypeReplacement(
                "AnalysisControlWeb",
                null,
                "DevExpress.ExpressApp.PivotChart.Web",
                null,
                "AnalysisControlWeb has no Blazor equivalent") },

            { "MapsAspNetModule", new TypeReplacement(
                "MapsAspNetModule",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "MapsAspNetModule has no Blazor equivalent") },

            { "ScriptRecorderAspNetModule", new TypeReplacement(
                "ScriptRecorderAspNetModule",
                null,
                "DevExpress.ExpressApp.ScriptRecorder.Web",
                null,
                "ScriptRecorderAspNetModule has no Blazor equivalent") },

            { "ScriptRecorderModuleBase", new TypeReplacement(
                "ScriptRecorderModuleBase",
                null,
                "DevExpress.ExpressApp.ScriptRecorder",
                null,
                "ScriptRecorderModuleBase has no .NET equivalent") },

            { "KpiModule", new TypeReplacement(
                "KpiModule",
                null,
                "DevExpress.ExpressApp.Kpi",
                null,
                "KpiModule has no .NET equivalent") },

            { "WebMapsPropertyEditor", new TypeReplacement(
                "WebMapsPropertyEditor",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "WebMapsPropertyEditor has no Blazor equivalent") },

            { "WebMapsListEditor", new TypeReplacement(
                "WebMapsListEditor",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "WebMapsListEditor has no Blazor equivalent") },

            { "WebVectorMapsListEditor", new TypeReplacement(
                "WebVectorMapsListEditor",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "WebVectorMapsListEditor has no Blazor equivalent") },

            { "ASPxRichTextPropertyEditor", new TypeReplacement(
                "ASPxRichTextPropertyEditor",
                null,
                "DevExpress.ExpressApp.Office.Web",
                null,
                "ASPxRichTextPropertyEditor has no direct Blazor equivalent") },

            // NEW: Additional no-equivalent types
            { "ImageResourceHttpHandler", new TypeReplacement(
                "ImageResourceHttpHandler",
                null,
                "DevExpress.ExpressApp.Web",
                null,
                "ImageResourceHttpHandler has no Blazor equivalent (Web Forms specific HTTP handler)") },

            { "IXafHttpHandler", new TypeReplacement(
                "IXafHttpHandler",
                null,
                "DevExpress.ExpressApp.Web",
                null,
                "IXafHttpHandler has no Blazor equivalent (Web Forms specific interface)") },

            { "IJScriptTestControl", new TypeReplacement(
                "IJScriptTestControl",
                null,
                "DevExpress.ExpressApp.Web.TestScripts",
                null,
                "IJScriptTestControl has no Blazor equivalent (Test framework specific)") },

            { "LayoutItemTemplate", new TypeReplacement(
                "LayoutItemTemplate",
                null,
                "DevExpress.ExpressApp.Web.Layout",
                null,
                "LayoutItemTemplate has no Blazor equivalent (Web Forms layout specific)") },

            { "LayoutGroupTemplate", new TypeReplacement(
                "LayoutGroupTemplate",
                null,
                "DevExpress.ExpressApp.Web.Layout",
                null,
                "LayoutGroupTemplate has no Blazor equivalent (Web Forms layout specific)") },

            { "TabbedGroupTemplate", new TypeReplacement(
                "TabbedGroupTemplate",
                null,
                "DevExpress.ExpressApp.Web.Layout",
                null,
                "TabbedGroupTemplate has no Blazor equivalent (Web Forms layout specific)") },

            // Web Forms specific types (TRANS-009)
            { "Page", new TypeReplacement(
                "Page",
                null,
                "System.Web.UI",
                null,
                "System.Web.UI.Page is Web Forms specific",
                new[] { ".cs" },
                commentOutEntireClass: true) },

            { "PopupShowingEventArgs", new TypeReplacement(
                "PopupShowingEventArgs",
                null,
                "DevExpress.ExpressApp.Web",
                null,
                "PopupShowingEventArgs is Web Forms specific",
                new[] { ".cs" },
                commentOutEntireClass: true) }
        };

        /// <summary>
        /// Types that have XAF .NET equivalents but require manual conversion (TRANS-010)
        /// These types cannot be automatically converted and require LLM analysis
        /// </summary>
        public static readonly Dictionary<string, TypeReplacement> ManualConversionRequiredTypes = new() {
            { "WebPropertyEditor", new TypeReplacement(
                "WebPropertyEditor",
                "BlazorPropertyEditorBase",
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "WebPropertyEditor has Blazor equivalent (BlazorPropertyEditorBase) but automatic conversion is not possible. Manual refactoring required.",
                new[] { ".cs" },
                commentOutEntireClass: false) },

            { "ASPxPropertyEditor", new TypeReplacement(
                "ASPxPropertyEditor",
                "BlazorPropertyEditorBase",
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPxPropertyEditor has Blazor equivalent (BlazorPropertyEditorBase) but automatic conversion is not possible. Manual refactoring required.",
                new[] { ".cs" },
                commentOutEntireClass: false) },

            { "ASPxDateTimePropertyEditor", new TypeReplacement(
                "ASPxDateTimePropertyEditor",
                "DateTimePropertyEditor",
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPxDateTimePropertyEditor has Blazor equivalent (DateTimePropertyEditor) but automatic conversion is not possible. Manual refactoring required.",
                new[] { ".cs" },
                commentOutEntireClass: false) }
        };

        /// <summary>
        /// Enum types that require commenting out entire class (TRANS-009)
        /// </summary>
        public static readonly Dictionary<string, EnumReplacement> ProblematicEnums = new() {
            { "TemplateType", new EnumReplacement(
                "TemplateType",
                new[] { "TemplateType.Horizontal", "TemplateType.Vertical" },
                "DevExpress.ExpressApp.Web.Templates",
                "TemplateType enum has no Blazor equivalent") }
        };

        /// <summary>
        /// Get all namespace replacements (both normal and NO_EQUIVALENT)
        /// </summary>
        public static IEnumerable<NamespaceReplacement> GetAllNamespaceReplacements() {
            return NamespaceReplacements.Values.Concat(NoEquivalentNamespaces.Values);
        }

        /// <summary>
        /// Get all type replacements (normal, NO_EQUIVALENT, and MANUAL_CONVERSION_REQUIRED)
        /// </summary>
        public static IEnumerable<TypeReplacement> GetAllTypeReplacements() {
            return TypeReplacements.Values
                .Concat(NoEquivalentTypes.Values)
                .Concat(ManualConversionRequiredTypes.Values);
        }

        /// <summary>
        /// Check if a type requires commenting out entire class
        /// </summary>
        public static bool RequiresCommentOutClass(string typeName) {
            return NoEquivalentTypes.TryGetValue(typeName, out var replacement) && 
                   replacement.CommentOutEntireClass;
        }

        /// <summary>
        /// Check if a type requires manual conversion
        /// </summary>
        public static bool RequiresManualConversion(string typeName) {
            return ManualConversionRequiredTypes.ContainsKey(typeName);
        }

        /// <summary>
        /// Check if an enum usage requires commenting out entire class
        /// </summary>
        public static bool IsProblematicEnum(string enumName) {
            return ProblematicEnums.ContainsKey(enumName);
        }
    }

    /// <summary>
    /// Namespace replacement information
    /// </summary>
    internal class NamespaceReplacement {
        public string OldNamespace { get; }
        public string NewNamespace { get; }
        public string Description { get; }
        public string[] ApplicableFileTypes { get; }
        public bool HasEquivalent => NewNamespace != null;

        public NamespaceReplacement(
            string oldNamespace,
            string newNamespace,
            string description,
            string[] applicableFileTypes = null) {
            OldNamespace = oldNamespace;
            NewNamespace = newNamespace;
            Description = description;
            ApplicableFileTypes = applicableFileTypes ?? new[] { ".cs", ".xafml" };
        }

        public bool AppliesToFileType(string fileExtension) {
            return ApplicableFileTypes.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Type replacement information
    /// </summary>
    internal class TypeReplacement {
        public string OldType { get; }
        public string NewType { get; }
        public string OldNamespace { get; }
        public string NewNamespace { get; }
        public string Description { get; }
        public string[] ApplicableFileTypes { get; }
        public bool CommentOutEntireClass { get; }
        public bool HasEquivalent => NewType != null;

        public TypeReplacement(
            string oldType,
            string newType,
            string oldNamespace,
            string newNamespace,
            string description,
            string[] applicableFileTypes = null,
            bool commentOutEntireClass = false) {
            OldType = oldType;
            NewType = newType;
            OldNamespace = oldNamespace;
            NewNamespace = newNamespace;
            Description = description;
            ApplicableFileTypes = applicableFileTypes ?? new[] { ".cs", ".xafml" };
            CommentOutEntireClass = commentOutEntireClass;
        }

        public bool AppliesToFileType(string fileExtension) {
            return ApplicableFileTypes.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }

        public string GetFullOldTypeName() {
            return string.IsNullOrEmpty(OldNamespace) ? OldType : $"{OldNamespace}.{OldType}";
        }

        public string GetFullNewTypeName() {
            return string.IsNullOrEmpty(NewNamespace) ? NewType : $"{NewNamespace}.{NewType}";
        }
    }

    /// <summary>
    /// Enum replacement information
    /// </summary>
    internal class EnumReplacement {
        public string EnumName { get; }
        public string[] ProblematicValues { get; }
        public string Namespace { get; }
        public string Description { get; }

        public EnumReplacement(
            string enumName,
            string[] problematicValues,
            string @namespace,
            string description) {
            EnumName = enumName;
            ProblematicValues = problematicValues;
            Namespace = @namespace;
            Description = description;
        }
    }
}
