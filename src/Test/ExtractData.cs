using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms;

namespace Sage.SalesLogix.Migration.Tests
{
    public static class ExtractData
    {
        private static StreamWriter _txtWriter;

        public static void Run()
        {
            Run(true, true, true);
        }

        private static void Run(bool extractBinary, bool extractText, bool extractScript)
        {
            string dataRoot = @"C:\Temp\SLXData\ActiveForms";
            SortedDictionary<string, long> plugins = new SortedDictionary<string, long>();

            foreach (DirectoryInfo directory in new DirectoryInfo(dataRoot).GetDirectories())
            {
                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    foreach (FileInfo file in subDirectory.GetFiles())
                    {
                        //if (file.FullName.Contains("natobook") && !file.FullName.Contains("eventix"))
                        //if (file.Length > 600000)
                        {
                            plugins.Add(file.FullName, file.Length);
                        }
                    }
                }
            }

            string[] fileNames = new string[plugins.Count];
            plugins.Keys.CopyTo(fileNames, 0);
            long[] fileSizes = new long[plugins.Count];
            plugins.Values.CopyTo(fileSizes, 0);
            Array.Sort(fileSizes, fileNames);

            foreach (string fileName in fileNames)
            {
                Console.WriteLine(fileName);

                string binFileName = Path.ChangeExtension(fileName.Replace(dataRoot, dataRoot + "_bin"), "bin");
                string txtFileName = Path.ChangeExtension(fileName.Replace(dataRoot, dataRoot + "_txt"), "txt");
                string vbsFileName = Path.ChangeExtension(fileName.Replace(dataRoot, dataRoot + "_vbs"), "vbs");

                Directory.CreateDirectory(Path.GetDirectoryName(binFileName));
                Directory.CreateDirectory(Path.GetDirectoryName(txtFileName));
                Directory.CreateDirectory(Path.GetDirectoryName(vbsFileName));

                byte[] strData = File.ReadAllBytes(fileName);
                byte[] binData = BorlandUtils.ObjectTextToBinary(strData);

                if (extractBinary)
                {
                    File.WriteAllBytes(binFileName, binData);
                }

                if (extractText || extractScript)
                {
                    if (extractText)
                    {
                        _txtWriter = new StreamWriter(txtFileName);
                    }

                    try
                    {
                        using (MemoryStream stream = new MemoryStream(binData))
                        {
                            DelphiComponent component = new DelphiBinaryReader(stream).ReadComponent(true);

                            if (extractScript)
                            {
                                object script;

                                if (component.Properties.TryGetValue("Script", out script) && script != null)
                                {
                                    File.WriteAllText(vbsFileName, script.ToString());
                                }
                            }

                            if (extractText)
                            {
                                OutputComponent(component, string.Empty);
                            }

                            Debug.Assert(stream.Position == stream.Length);
                        }
                    }
                    finally
                    {
                        if (extractText)
                        {
                            _txtWriter.Dispose();
                        }
                    }
                }
            }
        }

        private static void OutputComponent(DelphiComponent component, string indentation)
        {
            WriteLine("{0}object {1}: {2}", indentation, component.Name, component.Type);

            foreach (KeyValuePair<string, object> property in component.Properties)
            {
                OutputProperty(property.Key, property.Value, indentation + "  ");
            }

            foreach (DelphiComponent subComponent in component.Components)
            {
                OutputComponent(subComponent, indentation + "  ");
            }

            WriteLine("{0}end", indentation);
        }

        private static void OutputProperty(string name, object value, string indentation)
        {
            Write("{0}{1}", indentation, name);
            byte[] data = value as byte[];
            indentation += "  ";

            if (data != null)
            {
                WriteLine();

                switch (name)
                {
                    case "ControlData":
                        OutputControlData(data, indentation);
                        break;
                    case "ChildControls":
                        OutputChildControls(data, indentation);
                        break;
                    case "Licence":
                        OutputLicense(data, indentation);
                        break;
                    case "_PopupMenu":
                        Output_PopupMenu(data, indentation);
                        break;
                    case "Events":
                        OutputEvents(data, indentation);
                        break;
                    case "DataBindings":
                        OutputDataBindings(data, indentation);
                        break;
                    case "Filter.Criteria":
                        OutputFilterCriteria(data, indentation);
                        break;
                    case "Glyph.Data":
                        OutputGlyphData(data, indentation);
                        break;
                    case "Picture.Data":
                    case "BackImage.Data":
                    case "Title.Brush.Image.Data":
                        OutputPictureData(data, indentation);
                        break;
                    case "Bitmap":
                        OutputBitmapData(data, indentation);
                        break;
                    case "_Series":
                        Output_Series(data, indentation);
                        break;
                    case "Items.Data":
                        Output_Items_Data(data, indentation);
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            else if (name == "QueryBuilderCookie")
            {
                OutputQueryBuilderCookie((string) value, indentation);
            }
            else
            {
                Write(" = ");
                OutputValue(value, indentation);
            }
        }

        private static void OutputValue(object value, string indentation)
        {
            if (value is string || value is DelphiUTF8String)
            {
                string str = value.ToString().Replace("\r", "#13").Replace("\n", "#10").Replace("\0", "#00");

                if (str.Length > 50)
                {
                    str = str.Substring(0, 50);
                }

                WriteLine("'{0}'", str);
            }
            else if (value is DelphiCollection)
            {
                DelphiCollection items = (DelphiCollection) value;
                Write("<");

                if (items.Count > 0)
                {
                    WriteLine();

                    for (int i = 0; i < items.Count; i++)
                    {
                        WriteLine("{0}[{1}]", indentation, i);

                        foreach (KeyValuePair<string, object> item in items[i])
                        {
                            OutputProperty(item.Key, item.Value, indentation + "  ");
                        }
                    }

                    Write(indentation);
                }

                WriteLine(">");
            }
            else if (value is DelphiList)
            {
                DelphiList items = (DelphiList) value;
                Write("(");

                if (items.Count > 0)
                {
                    WriteLine();

                    foreach (object obj in (DelphiList) value)
                    {
                        Write(indentation + "  ");
                        OutputValue(obj, indentation + "    ");
                    }

                    Write(indentation);
                }

                WriteLine(")");
            }
            else if (value is DelphiSet)
            {
                DelphiSet items = (DelphiSet) value;
                Write("[");

                if (items.Count > 0)
                {
                    WriteLine();

                    foreach (string str in items)
                    {
                        WriteLine("{0}{1}", indentation + "  ", str);
                    }

                    Write(indentation);
                }

                WriteLine("]");
            }
            else
            {
                WriteLine(value);
            }
        }

        private static void Output_Items_Data(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                int firstInt = reader.ReadInt32();

                if (firstInt == data.Length)
                {
                    int rootCount = reader.ReadInt32();
                    int totalChildren = 0;

                    for (int i = 0; i < rootCount; i++)
                    {
                        WriteLine("{0}[{1}]", indentation, i);
                        OutputProperty("SmallImageIndex", reader.ReadInt32(), indentation + "  ");
                        OutputProperty("StateImageIndex", reader.ReadInt32(), indentation + "  ");
                        OutputProperty("LargeImageIndex", reader.ReadInt32(), indentation + "  ");
                        int childCount = reader.ReadInt32();
                        OutputProperty("Indent", reader.ReadInt32(), indentation + "  ");
                        OutputProperty("Caption", reader.ReadString(), indentation + "  ");
                        totalChildren += childCount;

                        for (int j = 0; j < childCount; j++)
                        {
                            WriteLine("{0}[{1}]", indentation + "    ", j);
                            OutputProperty("Caption", reader.ReadString(), indentation + "      ");
                        }
                    }

                    WriteLine("{0}SubItemProperties", indentation);

                    for (int i = 0; i < totalChildren; i++)
                    {
                        WriteLine("{0}[{1}]", indentation + "  ", i);
                        OutputProperty("SmallImageIndex", reader.ReadInt16(), indentation + "    ");
                    }
                }
                else
                {
                    for (int i = 0; i < firstInt; i++)
                    {
                        WriteLine("{0}[{1}]", indentation, i);
                        OutputTreeNode(reader, 0, indentation + "  ");
                    }
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void Output_Series(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    int len = reader.ReadInt32();
                    string type = reader.ReadString(len);
                    WriteLine("{0}{1}", indentation, type);
                    OutputComponent(reader.ReadComponent(true), indentation + "  ");
                    OutputProperty("SQL", reader.ReadString(reader.ReadInt32()), indentation + "  ");
                    //TODO: figure out what these numbers mean
                    Debug.Assert(reader.ReadInt32() == 0);
                    Debug.Assert(reader.ReadInt32() == 0);
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputBitmapData(byte[] data, string indentation)
        {
            //TODO: figure out how to read the ImageList stream
            WriteLine("{0}{{Length={1}}}", indentation, data.Length);
        }

        private static void OutputPictureData(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                string type = reader.ReadString();

                if (type == "TBitmap")
                {
                    byte[] imageData = reader.ReadBinary();

                    if (imageData.Length > 0)
                    {
                        using (Stream imageStream = new MemoryStream(imageData))
                        {
                            Image image = Image.FromStream(imageStream);
                            WriteLine("{0}{1}", indentation, image.Size);
                        }
                    }
                }
                else if (type == "TIcon")
                {
                    byte[] imageData = reader.ReadBytes(data.Length - 6);

                    using (Stream imageStream = new MemoryStream(imageData))
                    {
                        Icon image = new Icon(imageStream);
                        WriteLine("{0}{1}", indentation, image.Size);
                    }
                }
                else if (type == "TMetafile")
                {
                    int len = reader.ReadInt32();
                    byte[] imageData = reader.ReadBytes(len - 4);

                    using (Stream imageStream = new MemoryStream(imageData))
                    {
                        Image image = Image.FromStream(imageStream);
                        WriteLine("{0}{1}", indentation, image.Size);
                    }
                }
                else
                {
                    Debug.Assert(false);
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputGlyphData(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                byte[] imageData = reader.ReadBinary();

                using (Stream imageStream = new MemoryStream(imageData))
                {
                    Image image = Image.FromStream(imageStream);
                    WriteLine("{0}{1}", indentation, image.Size);
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputFilterCriteria(byte[] data, string indentation)
        {
            //TODO: figure out what these numbers mean
            Debug.Assert(data[0] == 0);
            Debug.Assert(data[1] == 0);
            Debug.Assert(data[2] == 0);
            Debug.Assert(data[3] == 0);
            Debug.Assert(data.Length == 4);
        }

        private static void OutputDataBindings(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                int count = reader.ReadInteger();

                for (int i = 0; i < count; i++)
                {
                    int propertyIndex = reader.ReadInteger();
                    Debug.Assert(
                        propertyIndex == -518 ||
                        propertyIndex == -517 ||
                        (propertyIndex >= 1 && propertyIndex <= 9) ||
                        propertyIndex == 11 ||
                        propertyIndex == 12 ||
                        propertyIndex == 14 ||
                        propertyIndex == 16 ||
                        propertyIndex == 17 ||
                        propertyIndex == 19 ||
                        propertyIndex == 21 ||
                        (propertyIndex >= 29 && propertyIndex <= 33) ||
                        propertyIndex == 36 ||
                        propertyIndex == 39 ||
                        propertyIndex == 43 ||
                        (propertyIndex >= 101 && propertyIndex <= 105) ||
                        propertyIndex == 222 ||
                        propertyIndex == 0x60020010 ||
                        propertyIndex == 0x60020012 ||
                        propertyIndex == 0x60020016 ||
                        propertyIndex == 0x60020018 ||
                        propertyIndex == 0x6002001A);
                    string str = (string) reader.ReadValue();
                    WriteLine("{0}{1} ({2})", indentation, str, propertyIndex);
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputEvents(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                int count = reader.ReadInteger();

                for (int i = 0; i < count; i++)
                {
                    int eventIndex = reader.ReadInteger();
                    Debug.Assert((eventIndex >= 1 && eventIndex <= 23) ||
                                 (eventIndex >= 201 && eventIndex <= 206) || eventIndex == 0x60000017 || eventIndex == 0x60000018);
                    string str = (string) reader.ReadValue();
                    WriteLine("{0}{1} ({2})", indentation, str, eventIndex);
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void Output_PopupMenu(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                OutputComponent(new DelphiBinaryReader(stream).ReadComponent(true), indentation);
                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputLicense(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                int size = reader.ReadInteger();

                if (size > 0)
                {
                    //TODO: figure out what this number means
                    Debug.Assert(reader.ReadByte() == 6);
                    Debug.Assert(reader.ReadByte() == size);
                    //TODO: figure out what this data means
                    byte[] licenseData = reader.ReadBytes(size);
                    WriteLine("{0}{1}", indentation, Encoding.Unicode.GetString(licenseData));
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputQueryBuilderCookie(string value, string indentation)
        {
            WriteLine();
            int len = value.Length/2;
            byte[] data = new byte[len];

            for (int i = 0; i < len; i++)
            {
                data[i] = Convert.ToByte(value.Substring(i*2, 2), 16);
            }

            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiComponent component = new DelphiBinaryReader(stream).ReadComponent(true);
                OutputComponent(component, indentation);
                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputControlData(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                string prefix = Encoding.Default.GetString(reader.ReadBytes(4));

                if (prefix == "\x10\a\0\0")
                {
                    //TODO: figure out CRViewer.ControlData
                    return;
                }
                else if (prefix == "\u201C\xB2\0\0")
                {
                    //TODO: figure out DirDialog.ControlData
                    return;
                }
                else if (prefix == "!C4\x12")
                {
                    //TODO: figure out CommonDialog.ControlData
                    return;
                }
                else if (prefix == "\xFF\xFE<\0")
                {
                    //TODO: figure out ChartSpace2.ControlData
                    return;
                }
                else if (prefix == "\x17\0\x02\0")
                {
                    //TODO: figure out SigPlus.ControlData
                    return;
                }
                else if (prefix != "TPF0")
                {
                    throw new DelphiException("Invalid component prefix: " + prefix);
                }

                OutputComponent(reader, indentation);
                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputChildControls(byte[] data, string indentation)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                DelphiBinaryReader reader = new DelphiBinaryReader(stream);
                int count = reader.ReadInteger();

                for (int i = 0; i < count; i++)
                {
                    string name = (string) reader.ReadValue();
                    WriteLine("{0}{1}", indentation, name);
                    //TODO: figure out what this number means
                    Debug.Assert(reader.ReadByte() == 1);

                    while (reader.PeekChar() != 0)
                    {
                        OutputProperty(reader, indentation + "  ");
                    }

                    Debug.Assert(reader.ReadByte() == 0);
                }

                Debug.Assert(stream.Position == stream.Length);
            }
        }

        private static void OutputComponent(DelphiBinaryReader reader, string indentation)
        {
            string type = reader.ReadString();
            string name = reader.ReadString();
            WriteLine("{0}object {1}: {2}", indentation, name, type);

            while (reader.PeekChar() != 0)
            {
                OutputProperty(reader, indentation + "  ");
            }

            Debug.Assert(reader.ReadByte() == 0);

            while (reader.PeekChar() != 0)
            {
                OutputComponent(reader, indentation + "  ");
            }

            Debug.Assert(reader.ReadByte() == 0);
            WriteLine("{0}end", indentation);
        }

        private static void OutputProperty(DelphiBinaryReader reader, string indentation)
        {
            string key = reader.ReadString();

            switch (key)
            {
                case "PopupMenu":
                case "PopupMenuX":
                case "ImageList":
                case "ImagesList":
                case "LargeImagesList":
                case "SmallImagesList":
                case "StateImagesList":
                case "_ImageList":
                    WriteLine("{0}{1}", indentation, key);
                    OutputComponent(reader.ReadComponent(false), indentation + "  ");
                    break;
                default:
                    object value = reader.ReadValue();
                    OutputProperty(key, value, indentation);
                    break;
            }
        }

        private static void OutputTreeNode(DelphiBinaryReader reader, int level, string indentation)
        {
            //TODO: figure out what this number means
            Debug.Assert(reader.ReadInt32() == level + 26);
            OutputProperty("SmallImageIndex", reader.ReadInt32(), indentation);
            OutputProperty("SelectedImageIndex", reader.ReadInt32(), indentation);
            OutputProperty("StateImageIndex", reader.ReadInt32(), indentation);
            OutputProperty("LargeImageIndex", reader.ReadInt32(), indentation);
            OutputProperty("Indent", reader.ReadInt32(), indentation);
            int childCount = reader.ReadInt32();
            OutputProperty("Caption", reader.ReadString(), indentation);

            for (int i = 0; i < childCount; i++)
            {
                WriteLine("{0}[{1}]", indentation + "  ", i);
                OutputTreeNode(reader, level + 1, indentation + "    ");
            }
        }

        //private static void OutputText(byte[] data)
        //{
        //    File.WriteAllBytes("c:/out.bin", data);

        //    using (StreamWriter writer = new StreamWriter("c:/out.txt"))
        //    {
        //        for (int i = 0; i < data.Length; i++)
        //        {
        //            byte b1 = data[i];
        //            bool isB1Text = (b1 >= 0x20 && b1 <= 0x7F);

        //            if (i < data.Length - 1)
        //            {
        //                byte b2 = data[i + 1];
        //                bool isB2Text = (b2 >= 0x20 && b2 <= 0x7F);

        //                if (isB1Text)
        //                {
        //                    writer.Write((char) b1);

        //                    if (!isB2Text)
        //                    {
        //                        writer.Write("\r\n");
        //                    }
        //                }
        //                else
        //                {
        //                    if (isB2Text)
        //                    {
        //                        writer.Write("\r\n");
        //                    }

        //                    writer.Write(b1.ToString("X2") + " ");
        //                }
        //            }
        //            else
        //            {
        //                if (isB1Text)
        //                {
        //                    writer.Write((char) b1);
        //                }
        //                else
        //                {
        //                    writer.Write(b1.ToString("X2") + " ");
        //                }
        //            }
        //        }
        //    }
        //}

        private static void WriteLine()
        {
            if (_txtWriter != null)
            {
                _txtWriter.WriteLine();
            }
        }

        private static void WriteLine(object value)
        {
            if (_txtWriter != null)
            {
                _txtWriter.WriteLine(value);
            }
        }

        private static void WriteLine(string format, params object[] arg)
        {
            if (_txtWriter != null)
            {
                _txtWriter.WriteLine(format, arg);
            }
        }

        private static void Write(string format, params object[] arg)
        {
            if (_txtWriter != null)
            {
                _txtWriter.Write(format, arg);
            }
        }
    }
}