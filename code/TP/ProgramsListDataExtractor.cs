using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace TP
{
    public class ProgramsListDataExtractor : DataExtractor
    {

        public ProgramsListDataExtractor(string outputFileName)
        {
            fileName = outputFileName;
        }

        protected override string ExtractData()
        {
            List<string> headers = new List<string> { "Display Name", "Display Version", "Install Date", "Version", "Publisher", "Install Source", "Install Location" };
            List<List<string>> values = GetProgramsData();
            string extractedData = ToHTMLFormat("Programs List", headers, values);
            return extractedData;
        }

        protected List<List<string>> GetProgramsData()
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            var uninstallKey = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

            var programCodesList = uninstallKey?.GetSubKeyNames();
            List<List<string>> listOfPrograms = new List<List<string>>();
            List<string> valueNames = new List<string> { "DisplayName", "DisplayVersion", "InstallDate", "Version", "Publisher", "InstallSource", "InstallLocation" };

            foreach (string programCode in programCodesList)
            {
                List<string> programRow = new List<string>();
                object value;
                var programKey = uninstallKey.OpenSubKey(programCode);
                var has = false;
                foreach (string valueName in valueNames)
                {
                    value = programKey.GetValue(valueName);
                    if (value == null)
                    {
                        programRow.Add("");
                    }
                    else
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

    }
}
