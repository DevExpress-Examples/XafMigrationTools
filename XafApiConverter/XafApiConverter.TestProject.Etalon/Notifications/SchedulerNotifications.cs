using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Filtering;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule.Notifications;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafApiConverter.TestProject.Notifications {
    // NOTE: Class commented out due to types having no XAF .NET equivalent
    //   - Base class 'Event' has no equivalent in XAF .NET
    //     Event has no equivalent in XAF .NET (loaded from removed-api.txt)
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
    // ========== COMMENTED OUT CLASS ==========
    // [NavigationItem(false)]
    //     [ImageName("Notifications.Sheduler_with_notifications")]
    //     public class SchedulerNotifications : Event {
    //         public SchedulerNotifications(Session session) : base(session) { }
    //     }
    // ========================================


    // NOTE: Class commented out due to types having no XAF .NET equivalent
    //   - Type 'TaskImpl' has no equivalent in XAF .NET
    //     TaskImpl has no equivalent in XAF .NET (loaded from removed-api.txt)
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
    // ========== COMMENTED OUT CLASS ==========
    // [NavigationItem(false)]
    //     [ImageName("Notifications.Task_with_notifications")]
    //     public class TaskWithNotifications : BaseObject, ISupportNotifications {
    //         private TaskImpl task = new TaskImpl();
    //         private DateTime? alarmTime;
    //         private TimeSpan? remindIn;
    //         private IList<PostponeTime> postponeTimes;
    //
    //         public TaskWithNotifications(Session session) : base(session) {
    //         }
    //
    //         public string Subject {
    //             get { return task.Subject; }
    //             set { task.Subject = value; }
    //         }
    //
    //         [Browsable(false)]
    //         public TimeSpan? RemindIn {
    //             get { return remindIn; }
    //             set { remindIn = value; }
    //         }
    //
    //         [Browsable(false)]
    //         public DateTime? AlarmTime {
    //             get { return alarmTime; }
    //             set { alarmTime = value; }
    //         }
    //
    //         [Browsable(false)]
    //         public string NotificationMessage {
    //             get { return Subject; }
    //         }
    //
    //         [Browsable(false)]
    //         public object UniqueId {
    //             get { return Oid; }
    //         }
    //
    //         [Browsable(false)]
    //         public bool IsPostponed { get; set; }
    //     }
    // ========================================


    public class TaskWithNotificationsController : ViewController {
        private SimpleAction markCompletedAction;

        public TaskWithNotificationsController() {
            TargetObjectType = typeof(TaskWithNotifications);
            markCompletedAction = new SimpleAction(this, "MarkCompleted", PredefinedCategory.Edit);
            markCompletedAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
            markCompletedAction.ImageName = "State_Task_Completed";
        }
    }
}