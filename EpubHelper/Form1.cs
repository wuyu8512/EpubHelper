using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.IO.Compression;

namespace EpubHelper
{
	public partial class Form1 : Form
	{
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

		private void button1_Click(object sender, EventArgs e)
		{
			if (!File.Exists(textBox1.Text) || Path.GetExtension(textBox1.Text) != ".epub")
			{
				MessageBox.Show(this, "请先选择文件", "信息");
				return;
			}

			if (!radioButton1.Checked && !radioButton2.Checked && comboBox1.SelectedIndex <= 0 && !checkBox1.Checked)
			{
				MessageBox.Show(this, "请正确选择选项", "信息");
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

			ZipFile.ExtractToDirectory(textBox1.Text, filesPath);

			if (radioButton1.Checked || radioButton2.Checked || comboBox1.SelectedIndex > 0)
			{
				textBox2.AddText("读取转换配置...");
				ConvertType? convertType = null;
				string convertTypeStr = string.Empty;
				ConvertOptions convertOptions = new()
				{
					Protect = "protect.txt",
					S2TAfter = "s2t_after.txt",
					S2TBefore = "s2t_after_before.txt",
					T2SAfter = "t2s_after.txt",
					T2SBefore = "t2s_before.txt"
				};
				Dictionary<string, string> protect, before, after;

				if (radioButton1.Checked)
				{
					convertType = ConvertType.S2t;
					convertTypeStr = "繁化";
				}
				else if (radioButton2.Checked)
				{
					convertType = ConvertType.T2S;
					convertTypeStr = "简化";
				}

				if (convertType == null) return;

				protect = File.ReadAllLines(convertOptions.Protect).Select(x => x.Split(' ')).ToDictionary(x => x[0], x => x[1]);
				if (convertType == ConvertType.T2S)
				{
					before = File.ReadAllLines(convertOptions.T2SBefore).Select(x => x.Split(' ')).ToDictionary(x => x[0], x => x[1]);
					after = File.ReadAllLines(convertOptions.T2SAfter).Select(x => x.Split(' ')).ToDictionary(x => x[0], x => x[1]);
                }
                else
                {
					before = File.ReadAllLines(convertOptions.S2TBefore).Select(x => x.Split(' ')).ToDictionary(x => x[0], x => x[1]);
					after = File.ReadAllLines(convertOptions.S2TAfter).Select(x => x.Split(' ')).ToDictionary(x => x[0], x => x[1]);
				}

				//string ncx = null;
				//if (File.Exists(filesPath + "OEBPS\\toc.ncx")) ncx = File.ReadAllText(filesPath + "OEBPS\\toc.ncx");
				//var opf = File.ReadAllText(filesPath + "OEBPS\\content.opf");
				if (comboBox1.SelectedIndex == 0)
				{
					textBox2.AddText("开始" + convertTypeStr + "...");
					//使用OpenCC

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
					if (ncx != null) ncx = openCC.Convert(ncx);
					epubName = openCC.Convert(epubName);
				}
				else
				{
					textBox2.AddText("开始" + comboBox1.Text + "...");
					//使用繁化姬
				}
				//File.WriteAllText(filesPath + "OEBPS\\content.opf", opf);
				//if (ncx != null) File.WriteAllText(filesPath + "OEBPS\\toc.ncx", ncx);
				epubName = epubName + "_" + convert;
				textBox2.AddText(convert + "结束");
			}

			textBox2.AddText("重新打包...");
			ZipFile.CreateFromDirectory(Path.GetDirectoryName(textBox1.Text) + "\\" + epubName + ".epub", filesPath, CompressionLevel.Optimal, false);
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
}