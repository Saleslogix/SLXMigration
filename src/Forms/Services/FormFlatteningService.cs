using System;
using System.Collections.Generic;
using AxInterop.SLXControls;
using Interop.SLXControls;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class FormFlatteningService : IFormFlatteningService
    {
        #region IFormFlatteningService Members

        public void Flatten(DelphiComponent component, bool isLegacy)
        {
            return;
        }

        /*
        public void Flatten(DelphiComponent component, bool isLegacy)
        {
            for (int i = 0; i < component.Components.Count; i++)
            {
                DelphiComponent childComponent = component.Components[i];

                if (childComponent.Components.Count > 0)
                {
                    List<string> offsetControls;
                    int leftOffset = 0;
                    int topOffset = 0;

                    string cGuid;
                    int size;
                    int kind;
                    string controls2;

                    if (!isLegacy &&
                        childComponent.TryGetPropertyValue("CGUID", out cGuid) &&
                        StandardControls.LookupType(cGuid) == typeof (AxSplitterPanel) &&
                        childComponent.TryGetPropertyValue("Size", out size) &&
                        childComponent.TryGetPropertyValue("Kind", out kind) &&
                        childComponent.TryGetPropertyValue("Controls2", out controls2))
                    {
                        if (((TxSplitterPanelKind) kind) == TxSplitterPanelKind.pkHorizontal)
                        {
                            topOffset = size + 3;
                        }
                        else
                        {
                            leftOffset = size + 3;
                        }

                        offsetControls = new List<string>(controls2.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries));
                        offsetControls.Sort();
                    }
                    else
                    {
                        offsetControls = null;
                    }

                    int childLeft;
                    int childTop;
                    childComponent.TryGetPropertyValue("Left", out childLeft);
                    childComponent.TryGetPropertyValue("Top", out childTop);

                    for (int j = childComponent.Components.Count - 1; j >= 0; j--)
                    {
                        DelphiComponent grandComponent = childComponent.Components[j];

                        if ((isLegacy && grandComponent.Type != "TSLQuery" && grandComponent.Type != "TSLServerQuery") ||
                            (!isLegacy && grandComponent.Properties.ContainsKey("CGUID")))
                        {
                            int value;

                            if (grandComponent.TryGetPropertyValue("Left", out value))
                            {
                                grandComponent.Properties["Left"] =
                                    childLeft +
                                    value +
                                    (offsetControls != null && offsetControls.BinarySearch(grandComponent.Name) >= 0
                                         ? leftOffset
                                         : 0);
                            }

                            if (grandComponent.TryGetPropertyValue("Top", out value))
                            {
                                grandComponent.Properties["Top"] =
                                    childTop +
                                    value +
                                    (offsetControls != null && offsetControls.BinarySearch(grandComponent.Name) >= 0
                                         ? topOffset
                                         : 0);
                            }

                            component.Components.Insert(i + 1, grandComponent);
                            childComponent.Components[j] = null;
                        }
                    }
                }
            }

            for (int i = component.Components.Count - 1; i >= 0; i--)
            {
                DelphiComponent subComponent = component.Components[i];

                if (subComponent == null)
                {
                    component.Components.RemoveAt(i);
                }
                else
                {
                    for (int j = subComponent.Components.Count - 1; j >= 0; j--)
                    {
                        if (subComponent.Components[j] == null)
                        {
                            subComponent.Components.RemoveAt(j);
                        }
                    }
                }
            }
        }
         */

        #endregion
    }
}