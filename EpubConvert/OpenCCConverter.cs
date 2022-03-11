using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubConvert
{
    public class OpenCCConverter : Converter , IDisposable
    {
        private readonly OpenCC openCC;
        private bool disposedValue;

        public OpenCCConverter(string option)
        {
            openCC = new OpenCC(option);
        }

        public override string Convert(string text)
        {
            if (ConvertBefore != null) text = ReplaceVocabulary(text, ConvertBefore);

            text = openCC.Convert(text);

            if (ConvertAfter != null) text = ReplaceVocabulary(text, ConvertAfter);
            if (ConvertProtect != null)
            {
                var protect = ConvertProtect.ToDictionary(x => openCC.Convert(x), x => x);
                text = ReplaceVocabulary(text, protect);
            }

            return text;
        }

        private static string ReplaceVocabulary(string text, Dictionary<string, string> vocMap)
        {
            foreach (var key in vocMap.Keys)
            {
                text = text.Replace(key, vocMap[key]);
            }
            return text;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    openCC.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
