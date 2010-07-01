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
    [BuilderMapping("AxGroupBox")]
    class GroupBoxBuilder : PanelBuilder
    {
        protected override void OnPostBuild()
        {
            base.OnPostBuild();
            //todo: use the Shape and Style properties
        }

    }
}
