using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using AxInterop.SLXCharts;
using AxInterop.SLXControls;
using AxInterop.SLXDialogs;

namespace Sage.SalesLogix.Migration.Forms
{
    public static class StandardControls
    {
        private static readonly IDictionary<string, Type> _controls;

        static StandardControls()
        {
            _controls = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
            BuildControls(typeof (AxAnimate).Assembly);
            BuildControls(typeof (AxColorDialog).Assembly);
            BuildControls(typeof (AxChart).Assembly);
        }

        private static void BuildControls(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass)
                {
                    object[] attributes = type.GetCustomAttributes(typeof (AxHost.ClsidAttribute), false);

                    if (attributes.Length > 0)
                    {
                        string cGuid = ((AxHost.ClsidAttribute) attributes[0]).Value;
                        _controls.Add(cGuid, type);
                    }
                }
            }
        }

        public static Type LookupType(string cGuid)
        {
            Type type;
            _controls.TryGetValue(cGuid, out type);
            return type;
        }
    }
}