using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public interface IFormFlatteningService
    {
        void Flatten(DelphiComponent component, bool isLegacy);
    }
}