using System;
using System.Collections.Generic;
using System.Text;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.Platform.Application;
using Sage.Platform.QuickForms.Elements;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxPanel")]
    class PanelBuilder : ControlBuilder
    {

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFPanel();
        }

        protected override void OnPostBuild()
        {
            //this panel's elements were added to the quickForm.Elements collection
            //move them to the panel
            QFPanel panel = (QFPanel)QfControl;
            //foreach (var subcontrol in _control.Controls)
            for (int i = _control.Controls.Count-1; i >= 0; i--)
            {
                var subcontrol = _control.Controls[i];
                if (subcontrol.QfControl is QFHidden) 
                {
                    //leave hidden controls in the _form.QuickForm.Elements collection
                    //and remove them from thsi control
                    _control.Controls.RemoveAt(i);
                    continue;
                }
                else if ((!subcontrol.IsExcluded) && (!subcontrol.IsTool))
                {
                    string controlId = subcontrol.QfControl.ControlId;
                    foreach (var qfe in _form.QuickForm.Elements)
                    {
                        if (String.Compare(qfe.ElementId, controlId) == 0)
                        {
                            _form.QuickForm.Elements.Remove(qfe);
                            panel.Controls.Add(qfe);
                            break;
                        }
                    }
                }
            }
        }
    }
}
