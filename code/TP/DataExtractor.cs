using System.IO;
using System.Collections.Generic;

namespace TP
{
    public abstract class DataExtractor
    {
        protected string fileName;

	    public string ExtractDataAndGetFileName()
        {
            string extractedData = ExtractData();
            File.WriteAllText(fileName, extractedData);
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
                "        <tr>";
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
