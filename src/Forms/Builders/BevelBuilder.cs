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
    [BuilderMapping("AxBevel")]
    class BevelBuilder : PanelBuilder
    {
        protected override void OnPostBuild()
        {
            base.OnPostBuild();
            //todo: use the Shape and Style properties
        }

        protected override void OnBuild()
        {
            base.OnBuild();
            ((QFPanel)QfControl).StyleScheme = "Bevel";
        }

    }
}
