using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TP
{
    public static class Extensions
    {
        public static void NirsoftToolExecAndAdd(this List<string> list, string file)
        {
            list.Add(Helpers.execNirsoftTool(file));
        }
    }
}
