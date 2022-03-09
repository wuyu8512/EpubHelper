using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubHelper
{
    internal static class Utils
    {
		internal static void AddText(this TextBox textBox, string text)
		{
			if (string.IsNullOrEmpty(textBox.Text))
			{
				textBox.Text = text;
			}
			else
			{
				textBox.Text = textBox.Text + "\r\n" + text;
			}
		}
	}
}
