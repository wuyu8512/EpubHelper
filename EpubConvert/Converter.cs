using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubConvert
{
    public abstract class Converter : IConverter
    {
        public string[]? ConvertProtect { get; set; }
        public Dictionary<string, string>? ConvertAfter { get; set; }
        public Dictionary<string, string>? ConvertBefore { get; set; }

        public abstract string Convert(string text);
    }
}
