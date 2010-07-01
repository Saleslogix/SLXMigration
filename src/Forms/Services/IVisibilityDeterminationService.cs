using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public interface IVisibilityDeterminationService
    {
        void Determine(DelphiComponent component);
    }
}