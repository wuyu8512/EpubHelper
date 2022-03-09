using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EpubConvert
{
	public class OpenCC : IDisposable
	{
		private readonly IntPtr opencc;

		[DllImport("opencc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "opencc_open_w")]
		private static extern IntPtr opencc_open(string configFileName);

		[DllImport("opencc", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr opencc_convert_utf8(IntPtr opencc, byte[] input, int length);

		[DllImport("opencc", CallingConvention = CallingConvention.Cdecl)]
		private static extern int opencc_close(IntPtr opencc);

		public OpenCC(string confPath)
		{
			opencc = opencc_open(confPath);
		}

		public string Convert(string text)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Marshal.PtrToStringUTF8(opencc_convert_utf8(opencc, bytes, bytes.Length));
		}

        public void Dispose()
        {
			GC.SuppressFinalize(this);
			var code = opencc_close(opencc);
			if (code != 0) throw new Exception("OpenCC Object Dispose eror");
		}
    }
}
