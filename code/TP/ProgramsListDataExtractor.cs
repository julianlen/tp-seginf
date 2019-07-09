using System;
using Microsoft.Win32;

namespace TP
{
    class ProgramsListDataExtractor : DataExtractor
    {
        fileName = "programsList";
    }

    protected override string ExtractData()
    {
        var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem? RegistryView.Registry64 : RegistryView.Registry32);
        var uninstallKey = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        var programCodesList = uninstallKey?.GetSubKeyNames();
        if (programCodesList == null)
            return "Failed to get installed programs from registry";

        var csvList = "";
        foreach (string programCode in programCodesList)
        {
            var programKey = uninstallKey.OpenSubKey(programCode);
            var valueNames = programKey.GetValueNames();
            foreach (string valueName in valueNames)
            {
                var value = programKey.GetValue(valueName);
                csvList += valueName + ": " + value + "\n";
            }
            csvList += "--------------------------------------------------------------------\n";
        }
        // Create a file to write to.
        //string createText = "Hello and Welcome" + Environment.NewLine;
        var path = @"D:\Matias\Facultad\Seguridad\TP\tp-seginf\code\TP\programList.txt";
        File.WriteAllText(path, csvList);
        return;
    }

}
