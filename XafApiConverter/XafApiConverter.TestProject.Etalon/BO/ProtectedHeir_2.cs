using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Module.BO {
    internal class ProtectedHeir_2 : ProtectedHeir_1 {
        // Removed class
        public string ActivityInformation { get; set; }
    }

    // NOTE: Class commented out due to types having no XAF .NET equivalent
    //   - Base class 'ActivityInformation' has no equivalent (inferred from using DevExpress.ExpressApp.Workflow)
    //     ActivityInformation has no equivalent in XAF .NET (loaded from removed-api.txt)
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
    // ========== COMMENTED OUT CLASS ==========
    // internal class UnProtectedHeir : ActivityInformation {
    //         // Removed class
    //         public string ActivityInformation { get; set; }
    //     }
    // ========================================

    // NOTE: Class commented out due to types having no XAF .NET equivalent
    //   - Base class 'ActivityInformation' has no equivalent (inferred from using DevExpress.ExpressApp.Workflow)
    //     ActivityInformation has no equivalent in XAF .NET (loaded from removed-api.txt)
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
    // ========== COMMENTED OUT CLASS ==========
    // internal class UnProtectedHeir2 {
    //         // Removed class
    //         public ActivityInformation ActivityInformation { get; set; }
    //     }
    // ========================================
}
