using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public interface IFormSimplificationService
    {
        void Simplify(DelphiComponent component);
    }
}