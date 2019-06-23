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

        //file data
        byte[] FWdata = new byte[MAX_FW_SIZE];
        int usedBlocks;
        string filePath = string.Empty;
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
            Byte[] array = BitConverter.GetBytes((color & 0xffffff ) << 8);
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
                button.BackColor = Color.FromArgb(255, 255, 255, 255); //set to white
                button.Enabled = false;
                button.Text = "Unknown";
            }
        }

        private void UpdateGUIcolors()
        {
            UpdateButton(button3, FileNameColor);
            UpdateButton(button4, FileBackgroundColor);
            UpdateButton(button5, LastFileColor);
            textBox2.BackColor = Color.FromArgb(FileBackgroundColor);
            textBox2.ForeColor = Color.FromArgb(FileNameColor);
            textBox3.BackColor = Color.FromArgb(FileBackgroundColor);
            textBox3.ForeColor = Color.FromArgb(LastFileColor);
            UpdateButton(button6, PBarFillColor);
            UpdateButton(button7, PBarEmptyColor);
            textBox4.BackColor = Color.FromArgb(PBarFillColor);
            textBox5.BackColor = Color.FromArgb(PBarEmptyColor);
            UpdateButton(button8, TextColor);
            UpdateButton(button9, BackgroundColor);
            UpdateButton(button10, ButtonColor);
            textBox6.BackColor = Color.FromArgb(BackgroundColor);
            textBox6.ForeColor = Color.FromArgb(TextColor);
            button11.BackColor = Color.FromArgb(ButtonColor);
            button11.ForeColor = Color.FromArgb(TextColor);
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

        private void Button1_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "FW files (*.lcd)|*.lcd|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;

                //Decode input file
                usedBlocks = FWCodec.GetFW(filePath, FWdata);
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
                Filter = "FW files (*.lcd)|*.lcd|All files (*.*)|*.*",
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
                    string customVersion = "V"+ Version + "_PCE";
                    SetVersion(FWdata, customVersion, MenuVersionOffset);

                    //Save FW
                    if (FWCodec.PutFW(filePath, FWdata, usedBlocks) == 0)
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
    }

}
