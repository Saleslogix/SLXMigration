using System;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Builders
{

    public sealed class HiddenControlBuilder : ControlBuilder
    {

        public HiddenControlBuilder(DelphiComponent component)
        {
            _component = component;
        }

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFHidden();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (_component != null)
            {
                LogWarning("Control {0} of type {1} is invisible and will be mapped to the QFHiddenText control", new object[] { _component.Name, _component.Type });
            }
        }

        protected override void OnBuild()
        {
            try
            {
                if ((Control.Bindings != null) && (Control.Bindings.Count > 0))
                {
                    string propertyString = null;
                    //DataPathTranslator.TranslateField(Control.Bindings.Values[0]);
                    foreach (DataPath sp in Control.Bindings.Values)
                    {
                        propertyString = DataPathTranslator.TranslateField(sp);
                        break;
                    }

                    if (Control.Bindings.Count > 1)
                    {
                        LogWarning("Control {0}: more than one propertty is bound, will only map the first binding {1}", new object[] { _component.Name, propertyString });
                    }
                    
                    QfControl.DataBindings.Clear();
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, "Value"));
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }


    }
}
