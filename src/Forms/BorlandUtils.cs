using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Borland.Vcl;
using Borland.Vcl.Units;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms
{
    public static class BorlandUtils
    {
        private static readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");

        public static byte[] ObjectTextToBinary(byte[] textData)
        {
            TStringStream textStream = new TStringStream(_encoding.GetString(textData));
            TStringStream binStream = new TStringStream(string.Empty, _encoding);
            Classes.ObjectTextToBinary(textStream, binStream);
            return _encoding.GetBytes(binStream.DataString);
        }

        public static byte[] ObjectBinaryToText(byte[] binData)
        {
            TStringStream binStream = new TStringStream(_encoding.GetString(binData));
            TStringStream textStream = new TStringStream(string.Empty, _encoding);
            Classes.ObjectBinaryToText(binStream, textStream);
            return _encoding.GetBytes(textStream.DataString);
        }

        public static Image ParseGlyphData(byte[] data)
        {
            Image image;

            using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(data))
            {
                image = ParseGlyphData(binaryReader);
                Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length);
            }

            return image;
        }

        public static Image ParseGlyphData(DelphiBinaryReader binaryReader)
        {
            byte[] data = binaryReader.ReadBinary();
            Bitmap bmp;

            using (Stream stream = new MemoryStream(data))
            {
                using (Image image = Image.FromStream(stream))
                {
                    bmp = new Bitmap(image);
                    bmp.MakeTransparent();
                }
            }

            return bmp;
        }
    }
}