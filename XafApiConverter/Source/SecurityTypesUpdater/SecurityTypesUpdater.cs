using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;
using System.Xml.Linq;

namespace XafApiConverter {
    public static class SecurityTypesUpdater {
        public static bool ProcessDocument(Document doc) {
            var syntaxRoot = doc.GetSyntaxRootAsync().Result;
            var oldSyntaxRoot = syntaxRoot;
            bool isProjectReferencesEf = IsProjectReferencesEF6(doc.Project);

            var featureTogglesRemover = new FeatureToggleRemoveRewriter(new string[] {
                "SecuritySystemRole.AutoAssociationPermissions",
                "PasswordCryptographer.EnableRfc2898",
                "PasswordCryptographer.SupportLegacySha512"
            });
            syntaxRoot = featureTogglesRemover.Visit(syntaxRoot);

            var typeReplacements = new Dictionary<string, string>();
            if(isProjectReferencesEf) {
                // EF6
                typeReplacements.Add("DevExpress.Persistent.BaseImpl.EF.User", "DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyUser");
                typeReplacements.Add("DevExpress.Persistent.BaseImpl.EF.Role", "DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole");
                typeReplacements.Add("DevExpress.Persistent.BaseImpl.EF.TypePermissionObject", "DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyTypePermissionObject");
                typeReplacements.Add("DevExpress.Persistent.BaseImpl.EF.SecuritySystemObjectPermissionsObject", "DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyObjectPermissionsObject");
                typeReplacements.Add("DevExpress.Persistent.BaseImpl.EF.SecuritySystemMemberPermissionsObject", "DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyMemberPermissionsObject");
            } else {
                // XPO
                typeReplacements.Add("DevExpress.ExpressApp.Security.Strategy.SecuritySystemUser", "DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyUser");
                typeReplacements.Add("DevExpress.ExpressApp.Security.Strategy.SecuritySystemRole", "DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole");
                typeReplacements.Add("DevExpress.ExpressApp.Security.Strategy.SecuritySystemTypePermissionObject", "DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyTypePermissionObject");
                typeReplacements.Add("DevExpress.ExpressApp.Security.Strategy.SecuritySystemObjectPermissionsObject", "DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyObjectPermissionsObject");
                typeReplacements.Add("DevExpress.ExpressApp.Security.Strategy.SecuritySystemMemberPermissionsObject", "DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyMemberPermissionsObject");
            }
            foreach(var typeReplacement in typeReplacements) {
                var replacer = new TypeReplaceRewriter(typeReplacement.Key, typeReplacement.Value);
                syntaxRoot = replacer.Visit(syntaxRoot);
            }

            var roleInvocationFinder = new MemberInvocationFinder(new string[] {
                "EnsureTypePermissions",
                "EnsureTypePermissionObject",
                "SetTypePermissions",
                "AddObjectAccessPermission",
                "AddMemberAccessPermission",
                "SetTypePermissionsRecursively",
                "FindTypePermissionObject"});
            roleInvocationFinder.Visit(syntaxRoot);
            if(roleInvocationFinder.HasInvocation) {
                if(isProjectReferencesEf) {
                    // EF6
                    syntaxRoot = UsingsRewriter.AddUsingNamespaces(syntaxRoot, new string[] { "DevExpress.Persistent.BaseImpl.EF.PermissionPolicy" });
                    CreateFileFromResource(doc.Project, "PermissionPolicyRoleExtensions.cs", "XafApiConverter.PermissionPolicyRoleExtensions_EF_cs");
                } else {
                    // XPO
                    syntaxRoot = UsingsRewriter.AddUsingNamespaces(syntaxRoot, new string[] { "DevExpress.Persistent.BaseImpl.PermissionPolicy" });
                    CreateFileFromResource(doc.Project, "PermissionPolicyRoleExtensions.cs", "XafApiConverter.PermissionPolicyRoleExtensions_XPO_cs");
                }
            }

            if(syntaxRoot != oldSyntaxRoot) {
                doc = doc.WithText(syntaxRoot.GetText());
                syntaxRoot = doc.GetSyntaxRootAsync().Result;
                var semanticModel = doc.GetSemanticModelAsync().Result;
                var permissionStateReplacer = new PermissionStateSetterRewriter(semanticModel);
                syntaxRoot = permissionStateReplacer.Visit(syntaxRoot);
            }

            if(syntaxRoot != oldSyntaxRoot) {
                File.WriteAllText(doc.FilePath, syntaxRoot.ToFullString());
                Console.WriteLine($"[CHANGED] {doc.FilePath}");
                return true;
            }
            return false;
        }

        static readonly HashSet<string> filesAddedToProjects = new HashSet<string>();

        static void CreateFileFromResource(Project project, string fileName, string sourceResourceName) {
            string key = $"{project.FilePath}|{fileName}";
            if(filesAddedToProjects.Contains(key)) {
                return;
            }
            filesAddedToProjects.Add(key);
            string filePath = Path.Combine(Path.GetDirectoryName(project.FilePath), fileName);
            using(var stream = typeof(Program).Assembly.GetManifestResourceStream(sourceResourceName)) {
                byte[] data = new byte[stream.Length];
                stream.ReadExactly(data, 0, data.Length);
                if(IsProjectUsesDefaultCompileItems(project)) {
                    File.WriteAllBytes(filePath, data);
                } else {
                    using(var tempWorkspace = MSBuildWorkspace.Create()) {
                        var tempProject = tempWorkspace.OpenProjectAsync(project.FilePath).Result;
                        var doc = tempProject.AddDocument(fileName, Encoding.UTF8.GetString(data));
                        tempWorkspace.TryApplyChanges(doc.Project.Solution);
                    }
                }
                Console.WriteLine($"[ADDED] {filePath}");
            }
        }

        static bool IsProjectUsesDefaultCompileItems(Project project) {
            var xml = XDocument.Load(project.FilePath);
            return !xml.Descendants().Any(t => t.Name == "Compile" || t.Name.LocalName == "Compile");
        }

        static bool IsProjectReferencesEF6(Project project) {
            var efReferences = new string[] {
                "DevExpress.ExpressApp.EF6",
                "DevExpress.Persistent.BaseImpl.EF6",
                "DevExpress.ExpressApp.Security.EF6"
            };
            var xml = XDocument.Load(project.FilePath);
            var allRefs = xml.Descendants().Where(t => t.Name == "Reference" || t.Name.LocalName == "Reference" || t.Name == "PackageReference" || t.Name.LocalName == "PackageReference");
            return allRefs.Any(t => t.Attributes().Any(x => (x.Name == "Include" || x.Name.LocalName == "Include") && efReferences.Any(r => x.Value.Contains(r))));
        }
    }
}
