using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.Text;
using XafApiConverter.Converter;
using Xunit;

namespace XafApiConverterTests {
    
    public class IntegrationTests {
        [Fact]
        [Trait("Category", "Integration")]
        public void FullPipeline_Conversion_And_TypeMigration() {
            string projectToConvert = ProjectCompareHelper.FindProjectDirectory("XafApiConverter.TestProject");
            string projectEtalon = ProjectCompareHelper.FindProjectDirectory("XafApiConverter.TestProject.Etalon");
            string projectAfterConversion = ProjectCompareHelper.CreateProjectCopy(projectToConvert);
            try {
                RunFullPipeline(projectAfterConversion);
                ProjectCompareHelper.CompareProjectFiles(projectEtalon, projectAfterConversion);
            }
            finally {
                Directory.Delete(projectAfterConversion, true);
            }
        }

        static void RunFullPipeline(string projectDir) {
            MSBuildLocator.RegisterDefaults();
            string projectPath = Directory.GetFiles(projectDir, "*.csproj", SearchOption.TopDirectoryOnly).First();
            string solutionPath = Directory.GetFiles(projectDir, "*.sln", SearchOption.TopDirectoryOnly).First();

            
            // Step 2: SDK-style conversion
            XafApiConverter.Converter.ConversionCli.Run(new string[] { "-p", projectPath });
            // Step 1: Type migration (analyze and comment out problematic classes)
            XafApiConverter.Converter.TypeMigrationCli.Run(new string[] { "-s", solutionPath });
        }
    }
}
