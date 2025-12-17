using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace FeatureCenter.Module.Notifications {
    [NavigationItem(false)]
    [ImageName("Notifications.Sheduler_with_notifications")]
    public class SchedulerNotifications : Event {
        public SchedulerNotifications(Session session) : base(session) { }
    }
}
