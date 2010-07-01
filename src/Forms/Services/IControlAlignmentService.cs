using System.Collections.Generic;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public interface IControlAlignmentService
    {
        void Align(IList<ControlInfo> controls);
    }
}