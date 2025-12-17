using System;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.ExpressApp.MiddleTier;

namespace FeatureCenter.Module.Notifications {
    [NavigationItem(false)]
    [ImageName("Notifications.Sheduler_with_notifications")]
    public class CustomLogger : Logger {
        public Task<String> SelectDataAsync(CancellationToken cancellationToken) {
            return Task.FromResult("SelectData(selects)");
        }
    }
}
