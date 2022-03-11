using EpubConvert;
using System.IO.Compression;
using System.Text.Json;

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

        private void TextBox1_DragDrop(object sender, DragEventArgs e)
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

        private void TextBox1_DragEnter(object sender, DragEventArgs e)
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
                    FileInfo fil = new(path);
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

        private void Button1_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textBox1.Text) || Path.GetExtension(textBox1.Text) != ".epub")
            {
                MessageBox.Show(this, "请先选择文件", "信息");
                return;
            }

            if (!radioButton1.Checked && !radioButton2.Checked && comboBox1.SelectedIndex <= 0)
            {
                MessageBox.Show(this, "请正确选择选项", "信息");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    Start();
                }
                catch (Exception e)
                {
                    textBox2.AddText(e.ToString());
                }
            });
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
                var convertOptions = JsonSerializer.Deserialize<ConvertOptions>(File.ReadAllText("./epub_config/convert_options.json"));
                string[] protect;
                Dictionary<string, string> before, after;


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

                protect = File.ReadAllLines(convertOptions.Protect);
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

                Converter converter;

                if (comboBox1.SelectedIndex == 0)
                {
                    textBox2.AddText("开始" + convertTypeStr + "...");
                    //使用OpenCC
                    converter = new OpenCCConverter(convertType == ConvertType.T2S ? convertOptions.OpenCCOptions.T2S : convertOptions.OpenCCOptions?.S2T)
                    {
                        ConvertProtect = protect,
                        ConvertAfter = after,
                        ConvertBefore = before,
                    };
                }
                else
                {
                    textBox2.AddText("开始" + comboBox1.Text + "...");
                    //使用繁化姬
                    converter = null;
                }

                foreach (var item in Directory.GetFiles(filesPath + "OEBPS/Text"))
                {
                    var text = File.ReadAllText(item);
                    text = converter.Convert(text);
                    File.WriteAllText(item, text);
                }
                if (File.Exists(filesPath + "OEBPS/toc.ncx"))
                {
                    var ncx = converter.Convert(File.ReadAllText(filesPath + "OEBPS/toc.ncx"));
                    File.WriteAllText(filesPath + "OEBPS\\toc.ncx", ncx);
                }
                if (File.Exists(filesPath + "OEBPS/content.opf"))
                {
                    var opf = converter.Convert(File.ReadAllText(filesPath + "OEBPS/content.opf"));
                    File.WriteAllText(filesPath + "OEBPS/content.opf", opf);
                }
                epubName = converter.Convert(epubName);

                epubName = epubName + "_" + convertTypeStr;
                textBox2.AddText(convertTypeStr + "结束");
            }

            textBox2.AddText("重新打包...");
            var outFilePath = Path.GetDirectoryName(textBox1.Text) + "/" + epubName + ".epub";
            if (File.Exists(outFilePath)) File.Delete(outFilePath);
            ZipFile.CreateFromDirectory(filesPath, outFilePath, CompressionLevel.Optimal, false);
            Directory.Delete(filesPath, true);
            textBox2.AddText("运行完毕");
            button1.Enabled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Add("禁用");
            comboBox1.SelectedIndex = 0;
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton3.Checked)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != 0)
            {
                radioButton3.Checked = true;
            }
        }
    }
}