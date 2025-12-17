using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafApiConverter.TestProject.BO {
    // NOTE: Class has no XAF .NET equivalent
    //   - Base class 'Note' has no equivalent (inferred from using DevExpress.Persistent.BaseImpl)
    //     Note has no equivalent in XAF .NET (loaded from removed-api.txt)
    // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
public class Message : Note {
        public Message(Session session) : base(session) { }
    }
}
