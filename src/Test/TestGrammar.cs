using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GoldParser;
using Parser=Sage.SalesLogix.Migration.Script.VBSParser.Parser;

namespace Sage.SalesLogix.Migration.Tests
{
    public sealed class TestGrammar
    {
        public static void Run()
        {
            Process("TestScripts");
            Process("ActiveForms_vbs");
            Process("ActiveScripts");
        }

        private static void Process(string directoryName)
        {
            Grammar grammar;

            using (Stream stream = typeof (Parser).Assembly.GetManifestResourceStream(typeof (Parser), "VBScript.cgt"))
            {
                grammar = new Grammar(new BinaryReader(stream));
            }

            string dataRoot = Path.Combine(@"C:\Temp\SLXData", directoryName);
            SortedDictionary<string, long> plugins = new SortedDictionary<string, long>();

            foreach (DirectoryInfo directory in new DirectoryInfo(dataRoot).GetDirectories())
            {
                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    foreach (FileInfo file in subDirectory.GetFiles())
                    {
                        plugins.Add(file.FullName, file.Length);
                    }
                }
            }

            string[] fileNames = new string[plugins.Count];
            plugins.Keys.CopyTo(fileNames, 0);
            long[] fileSizes = new long[plugins.Count];
            plugins.Values.CopyTo(fileSizes, 0);
            Array.Sort(fileSizes, fileNames);

            Regex regex = new Regex(@"^([\w\s-]+)\:([\w\s-]+)$", RegexOptions.Compiled);

            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = fileNames[i];
                //Console.WriteLine("{0}\t{1}", fileSizes[i], fileName);
                string text;

                using (StreamReader reader = new StreamReader(fileName, Encoding.Default))
                {
                    string line;
                    long pos = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line == "|")
                        {
                            while (reader.Peek() == '|')
                            {
                                reader.ReadLine();
                            }

                            break;
                        }
                        else if (regex.Matches(line).Count == 0)
                        {
                            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                            reader.DiscardBufferedData();
                            break;
                        }

                        pos = reader.BaseStream.Position;
                    }

                    text = reader.ReadToEnd();
                }

                using (TextReader reader = new StringReader(text + Environment.NewLine))
                {
                    GoldParser.Parser parser = new GoldParser.Parser(reader, grammar);
                    parser.TrimReductions = true;
                    ParseMessage message;

                    do
                    {
                        message = parser.Parse();
                    } while (message == ParseMessage.Reduction || message == ParseMessage.TokenRead || message == ParseMessage.CommentLineRead);

                    if (message != ParseMessage.Accept)
                    {
                        //throw new Exception(parser.LineNumber.ToString());
                        Console.WriteLine("{0}\t{1}", parser.LineNumber, fileName);
                    }
                }
            }
        }
    }
}