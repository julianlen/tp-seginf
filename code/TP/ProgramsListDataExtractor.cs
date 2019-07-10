using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace TP
{
    public class ProgramsListDataExtractor : DataExtractor
    {
        
        public ProgramsListDataExtractor()
            {
                fileName = "programsList";
            }

        protected override string ExtractData()
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            var uninstallKey = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

            List<string> headers = new List<string> { "DisplayName", "DisplayVersion", "InstallDate", "Version", "Publisher", "InstallSource", "InstallLocation" };
            List<List<string>> values = getValues(headers, uninstallKey);

            string extractedData = ToHTMLFormat("Product Key List", headers, values);
            return extractedData;
            
        }

        private List<List<string>> getValues(List<string> headers, RegistryKey uninstallKey)
        {
            var programCodesList = uninstallKey?.GetSubKeyNames();
            List<List<string>> listOfPrograms = new List<List<string>>();

            foreach (string programCode in programCodesList)
            {
                List<string> programRow = new List<string>();
                object value;
                var programKey = uninstallKey.OpenSubKey(programCode);
                var has = false;
                foreach (string valueName in headers)
                {
                    value = programKey.GetValue(valueName);
                    if (value == null)
                    {
                        programRow.Add("");
                    } else
                    {
                        has = true;
                        programRow.Add(value.ToString());
                    }
                }
                if (has)
                    listOfPrograms.Add(programRow);
            }

            return listOfPrograms;
        }

        //private List<string> getHeaders(RegistryKey uninstallKey)
        //{
        //    var programCodesList = uninstallKey?.GetSubKeyNames();
        //    var names = new HashSet<string>();

        //    foreach (string programCode in programCodesList)
        //    {
        //        var programKey = uninstallKey.OpenSubKey(programCode);
        //        var valueNames = programKey.GetValueNames();
        //        foreach (string valueName in valueNames)
        //        {
        //            names.Add(valueName);
        //        }
        //    }

        //    return names.ToList();
        //}

        //public String getProgramList()
        //{
            
        //    var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem? RegistryView.Registry64 : RegistryView.Registry32);
        //    var uninstallKey = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        //    var programCodesList = uninstallKey?.GetSubKeyNames();
        //    if (programCodesList == null)
        //        return "Failed to get installed programs from registry";

        //    foreach (string programCode in programCodesList)
        //    {
        //        var programKey = uninstallKey.OpenSubKey(programCode);
        //        var valueNames = programKey.GetValueNames();
        //        foreach (string valueName in valueNames)
        //        {
        //            names.Add(valueName);
        //            string value = programKey.GetValue(valueName).ToString();
        //            fieldValue.Add((valueName, value));
        //        }
        //    }
        //    // Create a file to write to.
        //    //string createText = "Hello and Welcome" + Environment.NewLine;
        //    return ;

        //}        



    }
}
