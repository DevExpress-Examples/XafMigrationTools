using System;
using System.Globalization;
using System.Web.UI.WebControls;

using DevExpress.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using FeatureCenter.Module.PropertyEditors;
using DevExpress.ExpressApp.Model;

namespace FeatureCenter.Module.Web.PropertyEditors {
	[PropertyEditor(typeof(String), FeatureCenterEditorAliases.CustomStringEditor, false)]
	public class CustomStringEditor : ASPxPropertyEditor {
        ASPxComboBox dropDownControl = null;

        public CustomStringEditor(Type objectType, IModelMemberViewItem info) : base(objectType, info) { }

		protected override void SetupControl(WebControl control) {
			if(ViewEditMode == ViewEditMode.Edit) {
				foreach(CultureInfo culture in CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures)) {
					((ASPxComboBox)control).Items.Add(culture.EnglishName + "(" + culture.Name + ")");
				}
			}
		}
		protected override WebControl CreateEditModeControlCore() {
			dropDownControl = RenderHelper.CreateASPxComboBox();
            dropDownControl.ValueChanged += new EventHandler(EditValueChangedHandler);
			return dropDownControl;
		}
        public override void BreakLinksToControl(bool unwireEventsOnly) {
            if(dropDownControl != null) {
                dropDownControl.ValueChanged -= new EventHandler(EditValueChangedHandler);
            }
            base.BreakLinksToControl(unwireEventsOnly);
        }
	}
}
