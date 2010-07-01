using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLImage")]
    public sealed class ImageBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFImage();
        }

        protected override void OnBuild()
        {
            byte[] data;

            if (Component.TryGetPropertyValue("Picture.Data", out data))
            {
                Image image;

                using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(data))
                {
                    string type = binaryReader.ReadString();
                    Debug.Assert(type == "TBitmap");
                    image = BorlandUtils.ParseGlyphData(binaryReader);
                    Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length);
                }

                string name = Component.Name;

                if (name.StartsWith("img"))
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
                ((QFImage) QfControl).Image = string.Format("[Localization!Global_Images:{0}]", fullName);
            }
        }
    }
}