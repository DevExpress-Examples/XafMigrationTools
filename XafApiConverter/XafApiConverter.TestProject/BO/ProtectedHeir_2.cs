using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Workflow;

namespace Test.Module.BO {
    internal class ProtectedHeir_2 : ProtectedHeir_1 {
        // Removed class
        public string ActivityInformation { get; set; }
    }

    internal class UnProtectedHeir : ActivityInformation {
        // Removed class
        public string ActivityInformation { get; set; }
    }
}
