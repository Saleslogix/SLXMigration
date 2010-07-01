using System.Drawing;
using System.Drawing.Imaging;
using Sage.Platform.Controls;
using Sage.Platform.QuickForms.Controls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxButton")]
    public sealed class ButtonBuilder : ControlBuilder
    {
        private string _kind;

        protected override void OnInitialize()
        {
            if (Component.TryGetPropertyValue("Kind", out _kind))
            {
                Control.IsExcluded = (_kind == "bkOK" || _kind == "bkCancel");
                if (Control.IsExcluded) LogWarning("Button \"{0}\" is excluded - its type is {1}", new object[] {_component.Name, _kind});
                Control.IsTool = (_kind == "bkHelp");
                if (Control.IsTool) LogWarning("Button \"{0}\" is moved to the toolbar - its type is {1}", new object[] { _component.Name, _kind });
            }
        }

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFButton();
        }

        protected override void OnBuild()
        {
            QFButton button = (QFButton) QfControl;

            if (Control.IsTool)
            {
                button.Caption = "Help";
                button.ToolTip = "Help";
                button.ButtonType = ButtonType.Icon;
                button.Image = "[Localization!Global_Images:Help_16x16]";
            }

            byte[] data;

            if (Component.TryGetPropertyValue("Glyph.Data", out data) && string.IsNullOrEmpty(Control.Caption))
            {
                button.ButtonType = ButtonType.Icon;
                Image image = BorlandUtils.ParseGlyphData(data);
                string name = Component.Name;

                if (name.StartsWith("btn") || name.StartsWith("cmd"))
                {
                    name = name.Substring(3);
                }

                string extension;

                if (image is Metafile) //vector
                {
                    extension = "emf";
                }
                else if (image.GetFrameCount(FrameDimension.Time) > 1 || //animated
                         (image.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed) //indexed
                {
                    extension = "gif";
                }
                else if ((image.PixelFormat & PixelFormat.Alpha) == PixelFormat.Alpha) //transparency
                {
                    extension = "png";
                }
                else
                {
                    extension = "jpg";
                }

                string fullName = string.Format("{0}_{1}x{2}.{3}", name, image.Width, image.Height, extension);
                Context.GlobalImageResourceManager.AddUpdateResource(fullName, image);
                button.Image = string.Format("[Localization!Global_Images:{0}]", fullName);
            }
        }
    }
}