using System;
using System.Globalization;

using DevExpress.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Blazor.Editors;
using FeatureCenter.Module.PropertyEditors;
using DevExpress.ExpressApp.Model;

namespace FeatureCenter.Module.Web.PropertyEditors {
	// NOTE: Class commented out due to types having no XAF .NET equivalent
	//   - Base class 'ASPxPropertyEditor' has equivalent in XAF .NET (BlazorPropertyEditorBase) but automatic conversion is not possible. See: DevExpress.ExpressApp.Blazor.Editors.BlazorPropertyEditorBase
	//     ASPxPropertyEditor has Blazor equivalent (BlazorPropertyEditorBase) but automatic conversion is not possible. Manual refactoring required.
	// TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
	// ========== COMMENTED OUT CLASS ==========
	// [PropertyEditor(typeof(String), FeatureCenterEditorAliases.CustomStringEditor, false)]
	// 	public class CustomStringEditor : ASPxPropertyEditor {
	//         ASPxComboBox dropDownControl = null;
	//
	//         public CustomStringEditor(Type objectType, IModelMemberViewItem info) : base(objectType, info) { }
	//
	// 		protected override void SetupControl(WebControl control) {
	// 			if(ViewEditMode == ViewEditMode.Edit) {
	// 				foreach(CultureInfo culture in CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures)) {
	// 					((ASPxComboBox)control).Items.Add(culture.EnglishName + "(" + culture.Name + ")");
	// 				}
	// 			}
	// 		}
	// 		protected override WebControl CreateEditModeControlCore() {
	// 			dropDownControl = RenderHelper.CreateASPxComboBox();
	//             dropDownControl.ValueChanged += new EventHandler(EditValueChangedHandler);
	// 			return dropDownControl;
	// 		}
	//         public override void BreakLinksToControl(bool unwireEventsOnly) {
	//             if(dropDownControl != null) {
	//                 dropDownControl.ValueChanged -= new EventHandler(EditValueChangedHandler);
	//             }
	//             base.BreakLinksToControl(unwireEventsOnly);
	//         }
	// 	}
	// ========================================

}
