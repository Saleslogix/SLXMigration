using Interop.SalesLogix;

namespace Sage.SalesLogix.Migration.Script
{
    public interface IAxForm
    {
        object ActiveControl { get; set; }
        int Align { get; set; }
        ISlxApplication Application { get; set; }
        int AutoScroll { get; set; }
        string BaseTable { get; set; }
        string Caption { get; set; }
        int Color { get; set; }
        int ControlCount { get; set; }
        string CurrentID { get; set; }
        int Cursor { get; set; }
        int Height { get; set; }
        int HelpContext { get; set; }
        string HelpFile { get; set; }
        int HelpType { get; set; }
        string Hint { get; set; }
        object HorzScrollBar { get; set; }
        int HWND { get; set; }
        bool IsReading { get; set; }
        bool IsValidating { get; set; }
        bool IsWriting { get; set; }
        int KeyPreview { get; set; }
        string Language { get; set; }
        int Left { get; set; }
        int ModalResult { get; set; }
        bool Modified { get; set; }
        string Name { get; set; }
        IMainView Parent { get; set; }
        int PixelsPerInch { get; set; }
        string PluginID { get; set; }
        string PluginName { get; set; }
        object PopupMenu { get; set; }
        bool Post { get; set; }
        object Script { get; set; }
        int Tag { get; set; }
        int Top { get; set; }
        int Translate { get; set; }
        bool Validate { get; set; }
        object VertScrollBar { get; set; }
        int Width { get; set; }

        IAxControls Controls { get; }
        void ClearContextList();
    }
}