using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubHelper
{
    internal class ConvertOptions
    {
        public string Protect { get; set; }

        public string S2TAfter { get; set; }

        public string S2TBefore { get; set; }

        public string T2SAfter { get; set; }

        public string T2SBefore { get; set; }

        public OpenCCOptions OpenCCOptions { get; set; }
    }

    internal class OpenCCOptions
    {
        public string S2T { get; set; }

        public string T2S { get; set; }
    }
}
