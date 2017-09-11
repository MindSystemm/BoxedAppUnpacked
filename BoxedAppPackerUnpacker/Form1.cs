using Steamless.API.PE32;
using Steamless.API.PE64;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoxedAppPackerUnpacker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pictureBox1.AllowDrop = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        
        }
        public uint GetAlignment(uint val, uint align)
        {
            return (((val + align - 1) / align) * align);
        }
        
        /// <summary>
        /// Gets or sets the raw file data read from disk.
        /// </summary>
        public byte[] FileData { get; set; }

        /// <summary>
        /// Gets or sets the dos header of the file.
        /// </summary>
        public NativeApi32.ImageDosHeader32 DosHeader32 { get; set; }

        /// <summary>
        /// Gets or sets the NT headers of the file.
        /// </summary>
        public NativeApi32.ImageNtHeaders32 NtHeaders32 { get; set; }

        /// <summary>
        /// Gets or sets the sections of the file.
        /// </summary>
        public List<NativeApi32.ImageSectionHeader32> Sections32;

        /// <summary>
        /// Gets or sets the section data of the file.
        /// </summary>
        public List<byte[]> SectionData;

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void TextBox1DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        string DirectoryName = "";
        string ExePath = "";
        private void TextBox1DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array arrayyy = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (arrayyy != null)
                {
                    string text = arrayyy.GetValue(0).ToString();
                    int num = text.LastIndexOf(".", StringComparison.Ordinal);
                    if (num != -1)
                    {
                        string text2 = text.Substring(num);
                        text2 = text2.ToLower();
                        if (text2 == ".exe" || text2 == ".dll")
                        {
                            Activate();
                            ExePath = text;
                            label2.Text = "Status : Exe Loaded";
                            int num2 = text.LastIndexOf("\\", StringComparison.Ordinal);
                            if (num2 != -1)
                            {
                                DirectoryName = text.Remove(num2, text.Length - num2);
                            }
                            if (DirectoryName.Length == 2)
                            {
                                DirectoryName += "\\";
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            this.FileData = File.ReadAllBytes(ExePath);
            if (IntPtr.Size == 4)
            {
                this.NtHeaders32 = new NativeApi32.ImageNtHeaders32();
                this.DosHeader32 = new NativeApi32.ImageDosHeader32();
                this.DosHeader32 = Pe32Helpers.GetStructure<NativeApi32.ImageDosHeader32>(this.FileData);
                this.NtHeaders32 = Pe32Helpers.GetStructure<NativeApi32.ImageNtHeaders32>(this.FileData, this.DosHeader32.e_lfanew);
                int num = NtHeaders32.FileHeader.NumberOfSections;
                for (var x = 0; x < num; x++)
                {
                    var section = Pe32Helpers.GetSection(this.FileData, x, this.DosHeader32, this.NtHeaders32);
                    if (section.SectionName.Equals(".bxpck"))
                    {
                        var sectionData = new byte[this.GetAlignment(section.SizeOfRawData, this.NtHeaders32.OptionalHeader.FileAlignment)];
                        Array.Copy(this.FileData, section.PointerToRawData, sectionData, 0, section.SizeOfRawData);
                        string filename = DirectoryName + "\\" + Path.GetFileNameWithoutExtension(ExePath) + "-Unpacked" + Path.GetExtension(ExePath);

                        int firstIndex = GetNthIndex(sectionData, 0x5A, 1);
                        if (sectionData[firstIndex - 1] == 0x4D)
                        {
                            File.WriteAllBytes(filename, sectionData.Skip(firstIndex - 1).ToArray());
                            label3.Text += (firstIndex-1).ToString("X");
                        }
                        else
                        {
                            int secondIndex = GetNthIndex(sectionData, 0x5A, 2);
                            if (sectionData[secondIndex - 1] == 0x4A)
                            {
                                File.WriteAllBytes(filename, sectionData.Skip(secondIndex - 1).ToArray());
                                label3.Text += (secondIndex-1).ToString("X");
                            }
                            else
                            {
                                int lastIndex = GetNthIndex(sectionData, 0x4D, 3);
                                if (sectionData[lastIndex - 1] == 0x4D)
                                {
                                    File.WriteAllBytes(filename, sectionData.Skip(lastIndex - 1).ToArray());
                                    label3.Text += (lastIndex - 1).ToString("X");
                                }
                                else
                                {

                                }
                            }
                        }
                        goto sucess;
                    }
                }

            }
            else
            {
                this.DosHeader64 = new NativeApi64.ImageDosHeader64();
                this.NtHeaders64 = new NativeApi64.ImageNtHeaders64();
                this.DosHeader64 = Pe64Helpers.GetStructure<NativeApi64.ImageDosHeader64>(this.FileData);
                this.NtHeaders64 = Pe64Helpers.GetStructure<NativeApi64.ImageNtHeaders64>(this.FileData, this.DosHeader64.e_lfanew);
                for (var x = 0; x < this.NtHeaders64.FileHeader.NumberOfSections; x++)
                {
                    var section = Pe64Helpers.GetSection(this.FileData, x, this.DosHeader64, this.NtHeaders64);
                    if (section.SectionName.Equals(".bxpck"))
                    {
                        var sectionData = new byte[this.GetAlignment(section.SizeOfRawData, this.NtHeaders64.OptionalHeader.FileAlignment)];
                        Array.Copy(this.FileData, section.PointerToRawData, sectionData, 0, section.SizeOfRawData);
                        string filename = DirectoryName + "\\" + Path.GetFileNameWithoutExtension(ExePath) + "-Unpacked" + Path.GetExtension(ExePath);
                        File.WriteAllBytes(filename, sectionData.Skip(182).ToArray());
                        goto sucess;
                    }
                }
              
            }
            MessageBox.Show("BoxedAppPacker section not found (.bxpck) ! ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            sucess:
            label2.Text = "Status : Success ! ";
        }
        public int GetNthIndex(byte[] s, byte t, int n)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == t)
                {
                    count++;
                    if (count == n)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        public static int GetFirstOccurance(byte[] byteArray, byte byteToFind)
        {
            return Array.IndexOf(byteArray, byteToFind);
        }
        /// <summary>
        /// Gets or sets the dos header of the file.
        /// </summary>
        public NativeApi64.ImageDosHeader64 DosHeader64 { get; set; }

        /// <summary>
        /// Gets or sets the NT headers of the file.
        /// </summary>
        public NativeApi64.ImageNtHeaders64 NtHeaders64 { get; set; }


        /// <summary>
        /// Gets or sets the sections of the file.
        /// </summary>
        public List<NativeApi64.ImageSectionHeader64> Sections64;
        /// <summary>
        /// Gets or sets the Tls directory of the file.
        /// </summary>
        public NativeApi64.ImageTlsDirectory64 TlsDirectory64 { get; set; }

        /// <summary>
        /// Gets or sets a list of Tls callbacks of the file.
        /// </summary>
        public List<ulong> TlsCallbacks64 { get; set; }
    }
}
