using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AxInterop.SLXControls;
using Sage.Platform.Application;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class FormSimplificationService : IFormSimplificationService
    {
        private IComponentSimplificationService _componentSimplifier;

        [ServiceDependency]
        public IComponentSimplificationService ComponentSimplifier
        {
            set { _componentSimplifier = value; }
        }

        #region IFormSimplificationService Members

        public void Simplify(DelphiComponent component)
        {
            if (component.Type == "TAXForm" || component.Type == "TSupportAXForm")
            {
                for (int i = component.Components.Count - 1; i >= 0; i--)
                {
                    DelphiComponent subComponent = component.Components[i];

                    if (subComponent.Type == "TDreamDesigner" || subComponent.Type == "TActiveXTranslator" || subComponent.Type == "TDCScripter")
                    {
                        component.Components.RemoveAt(i);
                    }
                    else
                    {
                        Simplify(subComponent);
                    }
                }
            }

            InternalSimplify(component);
        }

        #endregion

        private void InternalSimplify(DelphiComponent component)
        {
            string cGuid;
            byte[] data;
            IDictionary<string, DelphiComponent> tabControlOwners = null;

            if (component.TryGetPropertyValue("CGUID", out cGuid))
            {
                Type type = StandardControls.LookupType(cGuid);

                if (type != null && component.TryGetPropertyValue("ControlData", out data))
                {
                    DelphiComponent controlData = ParseControlData(data);
                    component.Type = controlData.Type;

                    foreach (KeyValuePair<string, object> property in controlData.Properties)
                    {
                        if (!component.Properties.ContainsKey(property.Key))
                        {
                            component.Properties.Add(property);
                        }
                    }

                    /*if (type == typeof (AxTabControl))
                    {
                        tabControlOwners = new Dictionary<string, DelphiComponent>(StringComparer.InvariantCultureIgnoreCase);

                        foreach (DelphiComponent subComponent in controlData.Components)
                        {
                            InternalSimplify(subComponent);
                            string controlNames;

                            if (subComponent.TryGetPropertyValue("ControlNames", out controlNames))
                            {
                                foreach (string controlName in controlNames.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (!tabControlOwners.ContainsKey(controlName))
                                    {
                                        tabControlOwners.Add(controlName, subComponent);
                                    }
                                }

                                //subComponent.Properties.Remove("ControlNames");
                            }

                            component.Components.Add(subComponent);
                        }
                    }
                    else if (type == typeof (AxDataGrid))
                    {
                        foreach (DelphiComponent subComponent in controlData.Components)
                        {
                            InternalSimplify(subComponent);
                            component.Components.Add(subComponent);
                        }
                    }
                    else if (type == typeof(AxPanel))
                    {
                        foreach (DelphiComponent subComponent in controlData.Components)
                        {
                            InternalSimplify(subComponent);
                            component.Components.Add(subComponent);
                        }
                    }
                    else
                    {
                        Debug.Assert(controlData.Components.Count == 0);
                    }
                     */

                    //component.Properties.Remove("ControlData");

                    foreach (DelphiComponent subComponent in controlData.Components)
                    {
                        InternalSimplify(subComponent);
                        component.Components.Add(subComponent);
                    }
                }
            }

            _componentSimplifier.Simplify(component);

            if (component.TryGetPropertyValue("ChildControls", out data))
            {
                ICollection<DelphiComponent> childControls = ParseChildControls(data);

                foreach (DelphiComponent childControl in childControls)
                {
                    InternalSimplify(childControl);

                    if (tabControlOwners == null)
                    {
                        component.Components.Add(childControl);
                    }
                    else
                    {
                        DelphiComponent ownerTab;

                        if (tabControlOwners.TryGetValue(childControl.Name, out ownerTab))
                        {
                            ownerTab.Components.Add(childControl);
                        }
                    }
                }

                component.Properties.Remove("ChildControls");
            }
        }

        private DelphiComponent ParseControlData(byte[] data)
        {
            using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(data))
            {
                string prefix = Encoding.GetEncoding("iso-8859-1").GetString(binaryReader.ReadBytes(4));

                if (prefix != "TPF0")
                {
                    throw new DelphiException("Invalid component prefix: " + prefix);
                }

                DelphiComponent controlData = ParseComponent(binaryReader);
                Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length);
                return controlData;
            }
        }

        private DelphiComponent ParseComponent(DelphiBinaryReader reader)
        {
            DelphiComponent component = new DelphiComponent();
            component.Type = reader.ReadString();
            component.Name = reader.ReadString();

            while (reader.PeekChar() != 0)
            {
                ParseProperty(reader, component.Properties);
            }

            byte b = reader.ReadByte();
            Debug.Assert(b == 0);

            while (reader.PeekChar() != 0)
            {
                component.Components.Add(ParseComponent(reader));
            }

            b = reader.ReadByte();
            Debug.Assert(b == 0);
            return component;
        }

        private void ParseProperty(DelphiBinaryReader reader, IDictionary<string, object> properties)
        {
            string key = reader.ReadString();
            object value;

            switch (key)
            {
                case "PopupMenu":
                case "PopupMenuX":
                case "ImageList":
                case "ImagesList":
                case "LargeImagesList":
                case "SmallImagesList":
                case "StateImagesList":
                case "_ImageList":
                    DelphiComponent component = reader.ReadComponent(false);
                    _componentSimplifier.Simplify(component);
                    value = component;
                    break;
                default:
                    value = reader.ReadValue();
                    break;
            }

            properties.Add(key, value);
        }

        private ICollection<DelphiComponent> ParseChildControls(byte[] data)
        {
            using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(new MemoryStream(data)))
            {
                int count = binaryReader.ReadInteger();
                ICollection<DelphiComponent> childControls = new List<DelphiComponent>();

                for (int i = 0; i < count; i++)
                {
                    DelphiComponent component = new DelphiComponent();
                    component.Name = (string) binaryReader.ReadValue();
                    //TODO: figure out what this number means
                    byte b = binaryReader.ReadByte();
                    Debug.Assert(b == 1);

                    while (binaryReader.PeekChar() != 0)
                    {
                        ParseProperty(binaryReader, component.Properties);
                    }

                    b = binaryReader.ReadByte();
                    Debug.Assert(b == 0);
                    childControls.Add(component);
                }

                Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length);
                return childControls;
            }
        }
    }
}