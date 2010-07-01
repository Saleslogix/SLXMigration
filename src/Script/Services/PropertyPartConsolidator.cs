using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Sage.SalesLogix.Migration.Script.Services
{
    /// <summary>
    /// In VBS, property getters and setters are defined seperately.
    /// This translation merges all properties with common names (case-insenstive) together.
    /// </summary>
    public sealed class PropertyPartConsolidator
    {
        public void Consolidate(CodeTypeDeclaration typeDecl)
        {
            IDictionary<string, CodeMemberProperty> properties = new Dictionary<string, CodeMemberProperty>(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < typeDecl.Members.Count; i++)
            {
                CodeTypeMember typeMember = typeDecl.Members[i];

                if (typeMember is CodeMemberProperty)
                {
                    CodeMemberProperty memberProperty = (CodeMemberProperty) typeMember;
                    string name = memberProperty.Name;
                    CodeMemberProperty existingProperty;

                    if (properties.TryGetValue(name, out existingProperty))
                    {
                        existingProperty.GetStatements.AddRange(memberProperty.GetStatements);
                        existingProperty.SetStatements.AddRange(memberProperty.SetStatements);
                        typeDecl.Members.RemoveAt(i--);
                    }
                    else
                    {
                        properties.Add(name, memberProperty);
                    }
                }
                else if (typeMember is CodeTypeDeclaration)
                {
                    Consolidate((CodeTypeDeclaration) typeMember);
                }
            }
        }
    }
}