﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubConvert
{
    internal interface IConverter
    {
        public string Convert(string text);
    }
}
