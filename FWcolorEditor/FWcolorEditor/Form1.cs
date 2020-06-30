using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace FWcolorEditor
{


    public partial class Form1 : Form
    {
        private static readonly int MAX_FW_SIZE = 0x60000; //Max is 384KB (128KB are reserved for Bootloader)
        private static readonly uint LCD_FileMagic = 0x3F2D3D44; //File ID for .LCD files (Photon, Elegoo, Epax...)
        private static readonly uint TWP_FileMagic = 0xA4783E09; //File ID for .TWP files (Sonic Mini)

        //file data
        byte[] FWdata = new byte[MAX_FW_SIZE];
        int usedBlocks;
        string filePath = string.Empty;
        string fileExtension = string.Empty;
        uint fileMagic = LCD_FileMagic;

        Codec.CoDec FWCodec = new Codec.CoDec();

        //colors
        int FileNameColor;
        int FileBackgroundColor;
        int LastFileColor;
        int PBarFillColor;
        int PBarEmptyColor;
        int TextColor;
        int BackgroundColor;
        int ButtonColor;

        //addresses
        int file_background;
        int file_text;
        int file_last;
        int pBar_empty;
        int pBar_fill;
        int button_background;
        int text;
        int box_background;

        //version & brand
        string Version;
        string Brand;
        int MenuVersionOffset;

        public Form1()
        {
            InitializeComponent();
        }

        private int CheckVersion(string Version, int Length, byte[] File, int Offset)
        {
            byte[] array = new byte[Length];
            Buffer.BlockCopy(File, Offset, array, 0, Length);
            string fileVer = Encoding.UTF8.GetString(array, 0, Length);
            return (string.Compare(Version, 0, fileVer, 0, 1));
        }

        private int GetColor(byte[] File, int Offset)
        {
            if (Offset == 0) return 0; //unsupported edition
            if (Offset > MAX_FW_SIZE - 4)
            {
                string ErrorText = "Too big Offset: " + Offset;
                MessageBox.Show(ErrorText, "FW database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0; //Error in offset file
            }
            byte[] array = new byte[4];
            Buffer.BlockCopy(File, Offset, array, 0, 4);
            Array.Reverse(array);
            int value = BitConverter.ToInt32(array, 0);
            if ((value & 0xff) == 0)
                return (int)((value >> 8) | 0xff000000);
            else
            {
                string ErrorText = "Not a color value at Offset: " + Offset;
                MessageBox.Show(ErrorText, "FW database error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return 0; //value is not a color
            }
        }

        private int SetColor(byte[] File, int Offset, int color)
        {
            if (Offset == 0) return -1; //unsupported edition
            if (Offset > MAX_FW_SIZE - 4)
            {
                string ErrorText = "Too big Offset: " + Offset;
                MessageBox.Show(ErrorText, "FW database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -2; //Error in offset file
            }
            Byte[] array = BitConverter.GetBytes((color & 0xffffff) << 8);
            Array.Reverse(array);
            Buffer.BlockCopy(array, 0, File, Offset, 4);
            return 0;
        }

        private int ReadColors()
        {
            //Read the FW offsets file
            string text2 = AppDomain.CurrentDomain.BaseDirectory + "FWdata.csv";
            StreamReader streamReader;
            try
            {
                streamReader = new StreamReader(text2);
            }
            catch (Exception)
            {
                string ErrorText = "Unable to open Offset file: " + text2;
                MessageBox.Show(ErrorText, "FW database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            string line;
            int line_count = 1;
            Regex parts = new Regex(@"^([A-Za-z0-9]+)\,([A-Za-z0-9\.]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)$");
            line = streamReader.ReadLine(); //Discard first line
            while ((line = streamReader.ReadLine()) != null)
            {
                line_count++;
                Match match = parts.Match(line);
                if (match.Success)
                {
                    Brand = match.Groups[1].Value;
                    Version = match.Groups[2].Value;
                    string version = "V" + Version + "_LCD";
                    int ver_string1 = int.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
                    int ver_string2 = int.Parse(match.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
                    int ver_string3 = int.Parse(match.Groups[5].Value, System.Globalization.NumberStyles.HexNumber);
                    int ver_string4 = int.Parse(match.Groups[6].Value, System.Globalization.NumberStyles.HexNumber);
                    file_background = int.Parse(match.Groups[7].Value, System.Globalization.NumberStyles.HexNumber);
                    file_text = int.Parse(match.Groups[8].Value, System.Globalization.NumberStyles.HexNumber);
                    file_last = int.Parse(match.Groups[9].Value, System.Globalization.NumberStyles.HexNumber);
                    pBar_empty = int.Parse(match.Groups[10].Value, System.Globalization.NumberStyles.HexNumber);
                    pBar_fill = int.Parse(match.Groups[11].Value, System.Globalization.NumberStyles.HexNumber);
                    button_background = int.Parse(match.Groups[12].Value, System.Globalization.NumberStyles.HexNumber);
                    text = int.Parse(match.Groups[13].Value, System.Globalization.NumberStyles.HexNumber);
                    box_background = int.Parse(match.Groups[14].Value, System.Globalization.NumberStyles.HexNumber);

                    int versionLen = version.Length;

                    if ((CheckVersion(version, versionLen, FWdata, ver_string2) == 0) && (CheckVersion(version, versionLen, FWdata, ver_string4) == 0))
                    {
                        //FW found. Get color values
                        FileNameColor = GetColor(FWdata, file_text);
                        FileBackgroundColor = GetColor(FWdata, file_background);
                        LastFileColor = GetColor(FWdata, file_last);
                        PBarFillColor = GetColor(FWdata, pBar_fill);
                        PBarEmptyColor = GetColor(FWdata, pBar_empty);
                        TextColor = GetColor(FWdata, text);
                        BackgroundColor = GetColor(FWdata, box_background);
                        ButtonColor = GetColor(FWdata, button_background);
                        MenuVersionOffset = ver_string3;

                        return 0;
                    }
                    else MenuVersionOffset = 0;
                }
                else
                {
                    string ErrorText = "Error in Offset file: " + text2 + " at line " + line_count;
                    MessageBox.Show(ErrorText, "FW database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }
            }
            //Unsupported FW
            MessageBox.Show("Not supported FW version!", "Unknown FW version", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return -2;
        }

        private void UpdateButton(Button button, int color)
        {
            if (color != 0)
            {
                button.BackColor = Color.FromArgb(color);
                button.Enabled = true;
                button.Text = "";
            }
            else
            {
                button.BackColor = Color.FromArgb(255, 127, 127, 127); //set to mid gray
                button.Enabled = false;
                button.Text = "Unknown";
            }
        }

        private void UpdateSampleButton(Button button, int colorBackg, int colorForeg)
        {
            if (colorBackg != 0)
            {
                button.BackColor = Color.FromArgb(colorBackg);
            }
            else
            {
                button.BackColor = Color.FromArgb(255, 127, 127, 127); //set to  mid gray
            }
            if (colorForeg != 0)
            {
                button.ForeColor = Color.FromArgb(colorForeg);
            }
            else
            {
                button.ForeColor = Color.FromArgb(255, 0, 0, 0); //set to black
            }
        }

        private void UpdateSampleBox(TextBox box, int colorBackg, int colorForeg)
        {
            if (colorBackg != 0)
            {
                box.BackColor = Color.FromArgb(colorBackg);
            }
            else
            {
                box.BackColor = Color.FromArgb(255, 127, 127, 127); //set to mid gray
            }
            if (colorForeg != 0)
            {
                box.ForeColor = Color.FromArgb(colorForeg);
            }
            else
            {
                box.ForeColor = Color.FromArgb(255, 0, 0, 0); //set to black
            }
        }

        private void UpdateGUIcolors()
        {
            UpdateButton(button3, FileNameColor);
            UpdateButton(button4, FileBackgroundColor);
            UpdateButton(button5, LastFileColor);
            UpdateSampleBox(textBox2, FileBackgroundColor, FileNameColor);
            UpdateSampleBox(textBox3, FileBackgroundColor, LastFileColor);
            UpdateButton(button6, PBarFillColor);
            UpdateButton(button7, PBarEmptyColor);
            UpdateSampleBox(textBox4, PBarFillColor, ~(0)); //white text
            UpdateSampleBox(textBox5, PBarEmptyColor, (0xff << 24)); //black text
            UpdateButton(button8, TextColor);
            UpdateButton(button9, BackgroundColor);
            UpdateButton(button10, ButtonColor);
            UpdateSampleBox(textBox6, BackgroundColor, TextColor);
            UpdateSampleButton(button11, ButtonColor, TextColor);
        }

        private void EditColor(Button button, ref int color, int FWoffset)
        {
            colorDialog1.Color = button.BackColor;
            DialogResult result = colorDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                //update color as int32
                color = colorDialog1.Color.ToArgb();

                //update GUI
                UpdateGUIcolors();

                //update in FW
                SetColor(FWdata, FWoffset, color);
            }
        }

        private int SetVersion(Byte[] File, string Name, int Offset)
        {
            if (Offset == 0) return -1; //unsupported edition
            if (Offset > MAX_FW_SIZE - 4)
            {
                string ErrorText = "Too big version Offset: " + Offset;
                MessageBox.Show(ErrorText, "FW database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -2; //Error in offset file
            }
            Byte[] fileVer = Encoding.UTF8.GetBytes(Name);
            Buffer.BlockCopy(fileVer, 0, File, Offset, Name.Length);
            return 0;

        }

        private void CheckAndSetColor(string index, uint ThemeColor, ref int GUIColor, int FWoffset)
        {
            if ((ThemeColor & 0xFF000000) != 0xFF000000)
            {
                //Not a valid color
                string ErrorText = "Value " + index + " of theme file is not valid as a color";
                MessageBox.Show(ErrorText, "Theme file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //Set color in GUI
                GUIColor = (int)ThemeColor;

                //update in FW
               SetColor(FWdata, FWoffset, (int)ThemeColor);

            }

        }


        private void Button1_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "FW files (*.lcd;*.twp)|*.lcd;*.twp|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;
                //Get the specified file extension
                fileExtension = Path.GetExtension(filePath);
                //If file extension is TWP update file magic
                if (string.Compare(fileExtension, 0, ".twp", 0, 4, true) == 0)
                    fileMagic = TWP_FileMagic;
                else
                    fileMagic = LCD_FileMagic;

                //Decode input file
                usedBlocks = FWCodec.GetFW(filePath, FWdata, fileMagic);
                if (usedBlocks > 0)
                {
                    //Read current colors
                    if (ReadColors() == 0)
                    {
                        //Update GUI
                        textBox1.Text = Brand + " " + Version;
                        UpdateGUIcolors();

                        //Enable save
                        button2.Enabled = true;
                        //Enable save theme
                        button13.Enabled = true;
                    }
                    else
                    {
                        textBox1.Text = "Unsupported";

                        //Disable save
                        button2.Enabled = false;
                    }
                }
                else
                {
                    if (usedBlocks == -4)
                        MessageBox.Show("Unable to open specified file", "FW file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (usedBlocks == -3)
                        MessageBox.Show("Corrupted FW file", "FW file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (usedBlocks == -2)
                        MessageBox.Show("Invalid FW file size", "FW file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (usedBlocks == -1)
                        MessageBox.Show("Not a CBD FW file!", "FW file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                }

            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                //                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "FW files (*" + fileExtension + ")| *" + fileExtension + "|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                //backup old file (if tick checked)
                if (checkBox1.Checked)
                {
                    string oldName = filePath + ".OLD";
                    File.Copy(filePath, oldName, true);
                }
                //Get the path of specified file
                filePath = saveFileDialog.FileName;
                if (usedBlocks > 0)
                {
                    //Update Menu shown string to identify as customized version (Photonsters Color Editor=PCE)
                    string customVersion = "V" + Version + "_PCE";
                    SetVersion(FWdata, customVersion, MenuVersionOffset);

                    //Save FW
                    if (FWCodec.PutFW(filePath, FWdata, usedBlocks, fileMagic) == 0)
                    {
                        MessageBox.Show("FW file generated!", "Encode Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Unable to generate FW file", "FW generation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("No data to generate a FW file!", "FW generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            EditColor(button3, ref FileNameColor, file_text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            EditColor(button4, ref FileBackgroundColor, file_background);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            EditColor(button5, ref LastFileColor, file_last);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            EditColor(button6, ref PBarFillColor, pBar_fill);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            EditColor(button7, ref PBarEmptyColor, pBar_empty);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            EditColor(button8, ref TextColor, text);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            EditColor(button9, ref BackgroundColor, box_background);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            EditColor(button10, ref ButtonColor, button_background);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Color editor files (*.pce)|*.pce|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                string fileName = saveFileDialog.FileName;
                StreamWriter streamWriter;
                try
                {
                    streamWriter = new StreamWriter(fileName);
                }
                catch (Exception)
                {
                    string ErrorText = "Unable to generate theme file:" + fileName;
                    MessageBox.Show(ErrorText, "File creation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                streamWriter.WriteLine("FileBackgroung,FileText,LastFile,ProgBarEmpty,ProgBarFill,Button,Text,BoxBackground");
                streamWriter.WriteLine("{0:X8},{1:X8},{2:X8},{3:X8},{4:X8},{5:X8},{6:X8},{7:X8}", FileBackgroundColor, FileNameColor, LastFileColor, PBarEmptyColor, PBarFillColor, ButtonColor, TextColor, BackgroundColor);
                streamWriter.Close();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "Color editor files (*.pce)|*.pce|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                string fileName = openFileDialog.FileName;
                StreamReader streamReader;
                try
                {
                    streamReader = new StreamReader(fileName);
                }
                catch (Exception)
                {
                    string ErrorText = "Unable to open theme file: " + fileName;
                    MessageBox.Show(ErrorText, "File read error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string line;
                Regex parts = new Regex(@"^([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)\,([a-fA-F0-9]+)$");
                line = streamReader.ReadLine(); //Discard first line
                line = streamReader.ReadLine(); //Read colors line
                Match match = parts.Match(line);
                if (match.Success)
                {
                    uint ARGBfileBkg = uint.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("FileBackgroung", ARGBfileBkg, ref FileBackgroundColor, file_background);
                    uint ARGBfileTxt = uint.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("FileText", ARGBfileTxt, ref FileNameColor, file_text);
                    uint ARGBfileLast = uint.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("LastFile", ARGBfileLast, ref LastFileColor, file_last);
                    uint ARGBpBarE = uint.Parse(match.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("ProgBarEmpty", ARGBpBarE, ref PBarEmptyColor, pBar_empty);
                    uint ARGBpBarF = uint.Parse(match.Groups[5].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("ProgBarFill", ARGBpBarF, ref PBarFillColor, pBar_fill);
                    uint ARGBbutton = uint.Parse(match.Groups[6].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("Button", ARGBbutton, ref ButtonColor, button_background);
                    uint ARGBtext = uint.Parse(match.Groups[7].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("Text", ARGBtext, ref TextColor, text);
                    uint ARGBbackg = uint.Parse(match.Groups[8].Value, System.Globalization.NumberStyles.HexNumber);
                    CheckAndSetColor("BoxBackground", ARGBbackg, ref BackgroundColor, box_background);
                    UpdateGUIcolors();
                    //Enable save theme
                    button13.Enabled = true;
                }
            }
        }
    }
}
