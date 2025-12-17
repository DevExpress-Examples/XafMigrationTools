using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;

namespace FeatureCenter.Module.Web {
    // NOTE: Partial class has no XAF .NET equivalent
    //   - Type 'ChartAspNetModule' has no equivalent in XAF .NET
    //     ChartAspNetModule has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'PivotGridAspNetModule' has no equivalent in XAF .NET
    //     PivotGridAspNetModule has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'MapsAspNetModule' has no equivalent in XAF .NET
    //     MapsAspNetModule has no Blazor equivalent
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
[ToolboxItemFilter("Xaf.Platform.Web")]
    public sealed partial class FeatureCenterAspNetModule : ModuleBase {
        public FeatureCenterAspNetModule() {
            InitializeComponent();
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
            ModuleUpdater updater = new Updater(objectSpace, versionFromDB);
            return new ModuleUpdater[] { updater };
        }
    }

}
