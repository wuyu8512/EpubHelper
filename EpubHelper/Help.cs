using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Tool;

namespace EpubHelper
{
	static class Help
	{
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		private static extern int WritePrivateProfileString(string section, string key, string val, string filePath);

		public static string IniReadValue(string filepath, string Section, string Key, int TextLength = 255)
		{
			StringBuilder temp = new StringBuilder(TextLength);
			int i = GetPrivateProfileString(Section, Key, "", temp, TextLength, filepath);
			return temp.ToString();
		}

		public static void IniWriteValue(string filepath, string Section, string Key, string Value)
		{
			WritePrivateProfileString(Section, Key, Value.ToString(), filepath);
		}

		public static void UnZip(string zipPath, string outPath)
		{
			ZipFile.ExtractToDirectory(zipPath, outPath);
		}

		public static void Zip(string zipPath, string outPath)
		{
			ZipFile.CreateFromDirectory(outPath, zipPath, CompressionLevel.Fastest, false);
		}

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

		public static List<cssinfo> GetCssInfo(string cssPath)
		{
			var cssinfos = new List<cssinfo>();
			foreach (var filename in Directory.GetFiles(cssPath))
			{
				var cssinfo = new cssinfo();
				cssinfo.filename = Path.GetFileName(filename);
				List<fontinfo> fontinfos = new List<fontinfo>();
				Dictionary<string, string> dic = new Dictionary<string, string>();
				CssParser css = new CssParser();
				var rules = css.ParseStyleSheet(File.ReadAllText(filename)).Rules;
				var fonts = rules.Where((c) => { return c.Type == CssRuleType.FontFace; });
				foreach (ICssFontFaceRule font in fonts)
				{
					string fontname = GetFontName(font.Source);
					string family = font.Family.Trim('\"');
					dic[family] = fontname;
					if (!fontinfos.Exists((f) => { return f.fontname == fontname; }))
					{
						fontinfo temp = new fontinfo();
						temp.fontname = fontname;
						fontinfos.Add(temp);
					}
				}
				var fontStyles = rules.ToList().FindAll((c) =>
				{
					if (c.Type == CssRuleType.Style)
					{
						var cssStyleRule = (ICssStyleRule)c;
						var style = cssStyleRule.Style;
						foreach (var item in style)
						{
							if (item.Name == "font-family")
							{
								if (dic.ContainsKey(item.Value))
								{
									var fontinfo = fontinfos.Find((f) => { return f.fontname == dic[item.Value]; });
									if (fontinfo != null)
									{
										fontinfo.selectors.Add(cssStyleRule.SelectorText);
										return true;
									}
								}
							}
						}
					}
					return false;
				});
				cssinfo.fontinfos = fontinfos;
				cssinfos.Add(cssinfo);
			}
			return cssinfos;
		}

		public static void FillingCssInfo(HtmlParser htmlParser, List<cssinfo> cssinfos, string html)
		{
			var htmlDocument = htmlParser.ParseDocument(html);
			var links = htmlDocument.QuerySelectorAll("link");
			foreach (IHtmlLinkElement link in links)
			{
				var cssfilename = link.Href.GetRight("/");
				var cssinfo = cssinfos.Find((c) => { return c.filename == cssfilename; });
				if (cssinfo != null)
				{
					var fontinfos = cssinfo.fontinfos;
					for (int j = 0; j < fontinfos.Count; j++)
					{
						fontinfo fontinfo = fontinfos[j];
						var selectors = fontinfo.selectors;
						for (int i = 0; i < selectors.Count; i++)
						{
							var selector = selectors[i];
							var tempElement = htmlDocument.Body.Clone() as AngleSharp.Dom.IElement;
							var elements = tempElement.QuerySelectorAll(selector);
							foreach (var element in elements)
							{
								for (int k = 0; k < fontinfos.Count; k++)
								{
									if (k != j)
									{
										var otherfontinfo = fontinfos[k];
										foreach (var otherselectors in otherfontinfo.selectors)
										{
											if (element.Owner.QuerySelector(otherselectors) == element)
											{
												if (element.LocalName.Equals(selector)) goto break_0;
											}
											var others = element.QuerySelectorAll(otherselectors);
											foreach (var other in others) other.Remove();	
										}
									}
								}
								fontinfo.text += element.TextContent;
								break_0: { }
							}
						}
					}
				}
			}
		}

		private static string GetFontName(string source)
		{
			return Path.GetFileName(source.Between("(\"", "\")"));
		}

		public static void test()
		{

		}
	}
}
