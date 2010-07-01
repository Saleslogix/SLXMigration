using System;
using System.Collections.Generic;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxTabControl")]
    class MultiTabBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFMultiTab();
        }

        protected override void OnInitialize() 
        {
        }

        protected override void OnBuild()
        {
            

        }

        protected override void OnPostBuild()
        {
            QFMultiTab MultiTab = (QFMultiTab)Control.QfControl;

            //for each tab (they are in the _component.Components collection), figure out the list of controls
            //and add them to a dictioary
            var tabControlOwners = new Dictionary<string, QFPanel>(StringComparer.InvariantCultureIgnoreCase);
            foreach (DelphiComponent subComponent in _component.Components)
            {
                if (string.Compare(subComponent.Type, "TTabSheetEx") == 0)
                {
                    QFPanel tab = MultiTab.AddNewTab();
                    string TabCaption;
                    if (subComponent.TryGetPropertyValue("Caption", out TabCaption))
                    {
                        tab.Caption = TabCaption;
                    }

                    string controlNames;

                    if (subComponent.TryGetPropertyValue("ControlNames", out controlNames))
                    {
                        foreach (string controlName in controlNames.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!tabControlOwners.ContainsKey(controlName))
                            {
                                tabControlOwners.Add(controlName, tab);
                            }
                        }
                    }
                }
            }

            //need to have at least one tab
            if (MultiTab.Tabs.Count == 0)
            {
                QFPanel tab = MultiTab.AddNewTab();
                tab.Caption = "Tab 1";
            }

            //now move controls that we have in teh Control.Controls collection
            //to each tab's Contols collection and remove them from the quickForm.Elements collection
            foreach (ControlInfo subControl in Control.Controls)
            {
                if ((!subControl.IsExcluded) && (!subControl.IsTool))
                {
                    string controlId = subControl.QfControl.ControlId;
                    foreach (var qfe in _form.QuickForm.Elements)
                    {
                        //if (String.Compare(qfe.ElementId, controlId) == 0)
                        if (qfe.Control == subControl.QfControl)
                        {
                            QFPanel ownerTab = null;
                            tabControlOwners.TryGetValue(controlId, out ownerTab);
                            if (ownerTab == null) ownerTab = MultiTab.Tabs[0]; //we have at least on tab
                            ownerTab.Controls.Add(qfe);
                            _form.QuickForm.Elements.Remove(qfe);
                            break;
                        }
                    }
                }
            }
            int ActiveTabIndex = 1;
            _component.TryGetPropertyValue("ActiveTabIndex", out ActiveTabIndex);
            MultiTab.ActivePageIndex = ActiveTabIndex; //in case it was reset
        }
    }
}
