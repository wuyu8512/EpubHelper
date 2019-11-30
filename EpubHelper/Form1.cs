using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Tool.ChineseConvert;
using Tool.Windows;
using System.Linq;

namespace EpubHelper
{
	public partial class Form1 : Form
	{
		// P/Invoke constants
		private const int WM_SYSCOMMAND = 0x112;
		private const int MF_STRING = 0x0;
		private const int MF_SEPARATOR = 0x800;

		// P/Invoke declarations
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool InsertMenu(IntPtr hMenu, int uPosition, int uFlags, int uIDNewItem, string lpNewItem);

		// ID for the About item on the system menu
		private int SYSMENU_ABOUT_ID = 0x1;

		private Dictionary<string, string> fanhuajiConverts = new Dictionary<string, string>();

		public Form1()
		{
			InitializeComponent();
			Control.CheckForIllegalCrossThreadCalls = false;
		}

		private void textBox1_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				//得到拖进来的路径,取第一个文件
				string path = files[0];
				string kzm = Path.GetExtension(path);
				if (!string.Equals(kzm, ".epub"))
				{
					MessageBox.Show("拖放进来的不是epub文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				textBox1.Text = path;
			}
		}

		private void textBox1_DragEnter(object sender, DragEventArgs e)
		{
			//如果拖进来的是文件类型
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
				//得到拖进来的路径,取第一个文件
				string path = paths[0];
				//路径字符串长度不为空
				if (path.Length > 1)
				{
					//判断是文件夹吗
					FileInfo fil = new FileInfo(path);
					if (fil.Attributes == FileAttributes.Directory)//文件夹
					{
						//鼠标图标链接
						e.Effect = DragDropEffects.Link;
					}
					else//文件
					{
						//鼠标图标链接
						e.Effect = DragDropEffects.Link;
					}
				}
				else
				{
					//鼠标图标禁止
					e.Effect = DragDropEffects.None;
				}
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Get a handle to a copy of this form's system (window) menu
			IntPtr hSysMenu = GetSystemMenu(this.Handle, false);

			// Add a separator
			AppendMenu(hSysMenu, MF_SEPARATOR, 0, string.Empty);

			// Add the About menu item
			AppendMenu(hSysMenu, MF_STRING, SYSMENU_ABOUT_ID, "关于 EpubHelper(&A)...");
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			// Test if the About item was selected from the system menu
			if ((m.Msg == WM_SYSCOMMAND) && ((int)m.WParam == SYSMENU_ABOUT_ID))
			{
				MessageBoxEx.Show(this, "EpubHelper，版本 1.5\r\n版权所有（无语）2019", "关于 EpubHelper",MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (!File.Exists(textBox1.Text) || Path.GetExtension(textBox1.Text) != ".epub")
			{
				MessageBoxEx.Show(this, "请先选择文件", "信息");
				return;
			}

			if (!radioButton1.Checked && !radioButton2.Checked && comboBox1.SelectedIndex <= 0 && !checkBox1.Checked)
			{
				MessageBoxEx.Show(this, "请正确选择选项", "信息");
				return;
			}

			new Thread(Start)
			{
				IsBackground = true
			}.Start();
		}		

		void Start()
		{
			button1.Enabled = false;
			string epubName = Path.GetFileNameWithoutExtension(textBox1.Text);
			string filesPath = Application.StartupPath + "\\" + epubName + "\\";
			if (Directory.Exists(filesPath)) Directory.Delete(filesPath, true);
			Directory.CreateDirectory(filesPath);
			textBox2.Text = string.Empty;
			textBox2.AddText("准备解包Epub...");
			Help.UnZip(textBox1.Text, filesPath);

			if (radioButton1.Checked || radioButton2.Checked || comboBox1.SelectedIndex > 0)
			{
				textBox2.AddText("读取转换配置...");
				string convert = string.Empty, config = string.Empty;
				string protect = string.Empty, before = string.Empty, after = string.Empty;

				if (radioButton1.Checked)
				{
					config = "Traditional";
					convert = "繁化";
				}
				else if (radioButton2.Checked)
				{
					config = "Simplified";
					convert = "简化";
				}
				else if (comboBox1.Text == "簡體化" || comboBox1.Text == "中國化" || comboBox1.Text == "維基簡體化")
				{
					config = "Simplified";
					convert = "简化";
				}
				else if (comboBox1.Text == "繁體化" || comboBox1.Text == "香港化" || comboBox1.Text == "台灣化" || comboBox1.Text == "維基繁體化")
				{
					config = "Traditional";
					convert = "繁化";
				}

				if (!string.IsNullOrWhiteSpace(config))
				{
					protect = File.ReadAllText("epub_config\\Protect.txt").Replace("\r\n", "\n");
					before = File.ReadAllText($"epub_config\\{config}_before.txt").Replace("\r\n", "\n");
					after = File.ReadAllText($"epub_config\\{config}_after.txt").Replace("\r\n", "\n");
				}

				var opf = File.ReadAllText(filesPath + "OEBPS\\content.opf");
				var ncx = File.ReadAllText(filesPath + "OEBPS\\toc.ncx");
				if (comboBox1.SelectedIndex == 0)
				{
					textBox2.AddText("开始" + convert + "...");
					//使用OpenCC
					string conf = Help.IniReadValue("epub_config\\epub_config.ini", "config", config);
					OpenCC openCC = new OpenCC(Application.StartupPath + "\\" + conf);
					string[] protectL = string.IsNullOrWhiteSpace(protect) ? new string[0] : protect.Split('\n');
					List<string[]> protectList = new List<string[]>(protectL.Select((p) => { return new string[] { openCC.Convert(p), p }; }));
					List<string[]> beforeList = new List<string[]>();
					List<string[]> afterList = new List<string[]>();
					//转换前
					foreach (var item in before.Split('\n'))
					{
						var temp = item.Split('=');
						if (temp.Length == 2) beforeList.Add(temp);
					}
					//转换后
					foreach (var item in after.Split('\n'))
					{
						var temp = item.Split('=');
						if (temp.Length == 2) afterList.Add(temp);
					}
					foreach (var item in Directory.GetFiles(filesPath + "OEBPS\\Text"))
					{
						var text = File.ReadAllText(item);
						text = replaceVocabulary(text, beforeList);
						text = openCC.Convert(text);
						text = replaceVocabulary(text, afterList);
						text = replaceVocabulary(text, protectList);
						File.WriteAllText(item, text);
					}
					opf = openCC.Convert(opf);
					ncx = openCC.Convert(ncx);
					epubName = openCC.Convert(epubName);
				}
				else
				{
					textBox2.AddText("开始" + comboBox1.Text + "...");
					//使用繁化姬
					var fanhuaji = new Fanhuaji();
					fanhuaji.DefaultConf.converter = fanhuajiConverts[comboBox1.Text];
					fanhuaji.DefaultConf.userProtectReplace = protect;
					fanhuaji.DefaultConf.userPreReplace = before;
					fanhuaji.DefaultConf.userPostReplace = after;
					foreach (var item in Directory.GetFiles(filesPath + "OEBPS\\Text"))
					{
						var text = File.ReadAllText(item);
						var result = fanhuaji.Convert(text).data as ConvertResult;
						File.WriteAllText(item, result.text);
					}
					epubName = ((ConvertResult)fanhuaji.Convert(epubName).data).text;
					opf = ((ConvertResult)fanhuaji.Convert(opf).data).text;
					ncx = ((ConvertResult)fanhuaji.Convert(ncx).data).text;
					convert = comboBox1.Text;
				}
				File.WriteAllText(filesPath + "OEBPS\\content.opf", opf);
				File.WriteAllText(filesPath + "OEBPS\\toc.ncx", ncx);
				epubName = epubName + "_" + convert;
				textBox2.AddText(convert + "结束");
			}

			if (checkBox1.Checked)
			{
				textBox2.AddText("正在获取Css内的字体信息...");
				var cssinfos = Help.GetCssInfo(filesPath + "OEBPS\\Styles");
				textBox2.AddText("正在解析HTML文件并寻找需要子集化的字体...");
				HtmlParser htmlParser = new HtmlParser();
				foreach (var file in Directory.GetFiles(filesPath+ "OEBPS\\Text"))
				{
					Help.FillingCssInfo(htmlParser , cssinfos, File.ReadAllText(file));
				}
				textBox2.AddText("正在子集化字体...");
				Dictionary<string, string> fontdic = new Dictionary<string, string>();
				foreach (var css in cssinfos)
				{
					foreach (var item in css.fontinfos)
					{
						if(fontdic.ContainsKey(item.fontname))
						{
							fontdic[item.fontname] += item.text;
						}
						else
						{
							fontdic[item.fontname] = item.text;
						}						
					}
				}
				if (File.Exists("pyftsubset.exe"))
				{
					var fontpath = filesPath + @"OEBPS\Fonts\";
					var ty = File.ReadAllText(@"epub_config\font_subset.txt");
					foreach (var ft in fontdic)
					{
						if (File.Exists(filesPath + @"OEBPS\Fonts\" + ft.Key))
						{
							File.WriteAllText("temp.txt", ft.Value + ty);
							ProcessStartInfo info = new ProcessStartInfo
							{
								FileName = "pyftsubset.exe",
								WindowStyle = ProcessWindowStyle.Hidden,
								Arguments = string.Format("\"{0}\" \"--text-file={1}\" \"--output-file={2}\"", fontpath + ft.Key, "temp.txt", fontpath + "sub_temp.ttf")
							};
							Process proc = Process.Start(info);
							proc.WaitForExit();
							if (File.Exists(fontpath + "sub_temp.ttf"))
							{
								File.Delete(fontpath + ft.Key);
								FileInfo fi = new FileInfo(fontpath + @"sub_temp.ttf");
								fi.MoveTo(fontpath + ft.Key);
							}
							else
							{
								textBox2.AddText("子集化出现错误，请尝试将软件放到其他目录运行");
							}
						}
					}
					textBox2.AddText("子集化完毕");
					File.Delete("temp.txt");
				}
				else
				{
					textBox2.AddText("子集化失败，缺少pyftsubset.exe，请检查。");
				}
				epubName += "_子";
			}

			textBox2.AddText("重新打包...");
			Help.Zip(Path.GetDirectoryName(textBox1.Text) + "\\" + epubName + ".epub", filesPath);
			Directory.Delete(filesPath, true);
			textBox2.AddText("运行完毕");
			button1.Enabled = true;
		}

		string replaceVocabulary(string text, List<string[]> vocList)
		{
			foreach (var item in vocList)
			{
				text = text.Replace(item[0], item[1]);
			}
			return text;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			var re = serializer.Deserialize<ConvertReturn>(File.ReadAllText("epub_config\\fanhuaji_service.json"));
			var service = serializer.ConvertToType<ConvertService>(re.data);
			comboBox1.Items.Add("禁用");
			foreach (var item in service.converters)
			{
				fanhuajiConverts[item.Value.name] = item.Key;
				comboBox1.Items.Add(item.Value.name);
			}
			//Help.test();
		}

		private void radioButton1_CheckedChanged(object sender, EventArgs e)
		{
			if(!radioButton3.Checked)
			{
				comboBox1.SelectedIndex = 0;
			}			
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox1.SelectedIndex!=0)
			{
				radioButton3.Checked = true;
			}
		}		
	}

	internal class cssinfo
	{
		public string filename;
		public List<fontinfo> fontinfos = new List<fontinfo>();
	}

	internal class fontinfo
	{
		public string fontname;
		public List<string> selectors = new List<string>();
		public string text = string.Empty;
	}
}
