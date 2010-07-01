using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Services;

namespace Sage.SalesLogix.Migration.Tests
{
    public static class TestFlattener
    {
        public static void Run()
        {
            string dataRoot = @"C:\Temp\SLXData\ActiveForms_bin";
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

            IFormFlatteningService flattener = new FormFlatteningService();

            foreach (string fileName in fileNames)
            {
                Console.WriteLine(fileName);
                byte[] data = File.ReadAllBytes(fileName);
                DelphiComponent component;

                using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(data))
                {
                    component = binaryReader.ReadComponent(true);
                }

                IList<string> beforeNames = new List<string>();
                GatherControlNames(component, beforeNames);
                flattener.Flatten(component, false);
                IList<string> afterNames = new List<string>();
                GatherControlNames(component, afterNames);
                Debug.Assert(beforeNames.Count == afterNames.Count);

                for (int i = 0; i < beforeNames.Count; i++)
                {
                    Debug.Assert(beforeNames[i] == afterNames[i]);
                }

                foreach (DelphiComponent childComponent in component.Components)
                {
                    foreach (DelphiComponent grandComponent in childComponent.Components)
                    {
                        Debug.Assert(grandComponent != null);
                        Debug.Assert(!grandComponent.Properties.ContainsKey("CLSID"));
                    }
                }
            }
        }

        private static void GatherControlNames(DelphiComponent component, IList<string> names)
        {
            foreach (DelphiComponent childComponent in component.Components)
            {
                if (childComponent.Properties.ContainsKey("CGUID"))
                {
                    names.Add(childComponent.Name);
                }

                GatherControlNames(childComponent, names);
            }
        }
    }
}