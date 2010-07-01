using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public interface IComponentSimplificationService
    {
        void Simplify(DelphiComponent component);
    }
}