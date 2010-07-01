using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public delegate void CodeDomWalkerAction(ref CodeObject target, CodeObject parent, int indent);

    public sealed class CodeDomWalker
    {
        private static readonly IDictionary<Type, IEnumerable<PropertyInfo>> _properties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly PropertyInfo _switchDefaultStmtsProperty = typeof (CodeSwitchStatement).GetProperty("DefaultStatements");

        private readonly IList _rootObjects;
        private CodeDomWalkerAction _action;

        public CodeDomWalker(CodeObject rootObject)
        {
            _rootObjects = new object[]
                {
                    rootObject
                };
        }

        public CodeDomWalker(IList rootObjects)
        {
            _rootObjects = rootObjects;
        }

        public void Walk(CodeDomWalkerAction action)
        {
            _action = action;
            WalkList(_rootObjects, null, 0);
            _action = null;
        }

        private void WalkList(IList list, CodeObject parent, int indent)
        {
            for (int i = 0; i < list.Count; i++)
            {
                CodeObject obj = list[i] as CodeObject;

                if (obj != null && WalkObject(ref obj, parent, indent))
                {
                    list[i] = obj;
                }
            }
        }

        private bool WalkObject(ref CodeObject target, CodeObject parent, int indent)
        {
            Type type = target.GetType();
            IEnumerable<PropertyInfo> properties;

            if (!_properties.TryGetValue(type, out properties))
            {
                PropertyInfo[] allProperties = type.GetProperties();
                List<PropertyInfo> someProperties = new List<PropertyInfo>(allProperties.Length);

                foreach (PropertyInfo property in allProperties)
                {
                    if (property.GetIndexParameters().Length == 0 &&
                        property.GetCustomAttributes(typeof (ObsoleteAttribute), false).Length == 0 &&
                        property.CanRead &&
                        ((property.CanWrite && typeof (CodeObject).IsAssignableFrom(property.PropertyType)) ||
                         typeof (IList).IsAssignableFrom(property.PropertyType)))
                    {
                        someProperties.Add(property);
                    }
                }

                properties = someProperties;
                _properties.Add(type, properties);
            }

            foreach (PropertyInfo property in properties)
            {
                object propertyValue = property.GetValue(target, null);

                if (propertyValue != null)
                {
                    if (propertyValue is CodeObject)
                    {
                        CodeObject childObj = (CodeObject) propertyValue;

                        if (WalkObject(ref childObj, target, indent))
                        {
                            property.SetValue(target, childObj, null);
                        }
                    }
                    else
                    {
                        bool isSwitchDefaultStmts = (property == _switchDefaultStmtsProperty);

                        if (isSwitchDefaultStmts)
                        {
                            indent++;
                        }

                        WalkList((IList) propertyValue, target, indent + 1);

                        if (isSwitchDefaultStmts)
                        {
                            indent--;
                        }
                    }
                }
            }

            CodeObject newTarget = target;
            _action(ref newTarget, parent, indent);
            bool changed = (newTarget != target);

            if (changed)
            {
                target = newTarget;
            }

            return changed;
        }
    }
}