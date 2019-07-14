using System;
using System.IO;
using System.Collections.Generic;

namespace TP
{
    public abstract class DataExtractor
    {
        protected string fileName;

	    public string ExtractDataAndGetFileName()
        {
            string extractedData;
            try
            {
                extractedData = ExtractData();   
            }

            catch (Exception e)
            {
                extractedData = "EXCEPTION!";
            }
            //File.WriteAllText(Path.GetTempPath() + fileName + ".html", extractedData);
            File.WriteAllText(fileName + ".html", extractedData);
            return fileName;
        }

        protected string ToHTMLFormat(string title, List<string> headers, List<List<string>> values)
        {
            string html =
                "<html>" +
                "  <head><title>" + title + "</title></head>" +
                "  <body>" +
                "    <h3>" + title + "</h3>" +
                "    <table border = '1' cellpadding = '5'>" +
                "      <thead>" +
                "        <tr bgcolor='E0E0E0'>";
            foreach (string header in headers)
                html += "  <th>" + header + "</th>";

            html += 
                "  </tr>" +
                "</thead>" +
                "<tbody>";

            foreach (List<string> rowValues in values)
            {
                html += "<tr>";
                foreach (string value in rowValues)
                    html += "<td>" + value + "</td>";
                html += "</tr>";
            }

            html +=
                "      </tbody>" +
                "    </table>" +
                "  </body>" +
                "</html>";

            return html;
        }

        protected abstract string ExtractData();

    }
}
