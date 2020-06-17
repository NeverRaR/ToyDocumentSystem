using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Threading;
using System.Collections;
using System.IO;
using DocumentSystem.Properties;
namespace DocumentSystem
{


    public partial class Form1 : Form
    {
        DocumentSystem docSys = new DocumentSystem();
        ArrayList curButo = new ArrayList();
        ArrayList curName = new ArrayList();
        String focusName;
        bool isInFile = false;
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = "root";
            docSys.IintSystem();
            RefreshFolder();
            this.Text = "Toy Document System";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isInFile)
            {
                MessageBox.Show("You are in a file!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            String s = Interaction.InputBox("Please enter the name of the folder you want to create (no more than 12 characters and in English) ", "CreateFolder");
            if (docSys.IsChildExisted(s))
            {
                MessageBox.Show("duplicated file name!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            if (s.Length == 0)
            {
                MessageBox.Show("empty file name!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            if (s.Length > 12)
            {
                MessageBox.Show("too long file name!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            docSys.CreatFileOnCur(s, true);
            RefreshFolder();

        }

        private void RefreshFolder()
        {
            int baseX = 40, baseY = 80;
            int deltaX = 104, deltaY = 99;
            int deltaNameY = 60;
            ArrayList al=docSys.GetAllChild((int)docSys.FCBLevel.Peek());
            ArrayList types = docSys.GetAllChildType((int)docSys.FCBLevel.Peek());
            foreach (Control obj in curButo)
            {
                this.Controls.Remove(obj);
            }
            foreach (Control obj in curName)
            {
                this.Controls.Remove(obj);
            }
            curButo.Clear();
            curName.Clear();
            int i;
            int curX = baseX, curY = baseY;
            int j;
            for(i=0;i*5<al.Count;++i)
            {
                curX = baseX;
                for (j = 0; i*5+j < al.Count&&j<5; ++j)
                {
                   
                    TextBox tb = new TextBox();
                    tb.Name = (string)al[i * 5 + j];
                    tb.Text = (string)al[i * 5 + j];
                    tb.ReadOnly = true;
                    tb.Size = new Size(80, 25);
                    tb.BorderStyle = BorderStyle.None;
                    exButton eb = new exButton();
                    eb.Size = new Size(50, 50);
                    eb.Location = new Point(curX, curY);
                    eb.BackgroundImageLayout = ImageLayout.Stretch;
                    eb.name = (string)al[i * 5 + j];
                    if ((bool)types[i * 5 + j])
                    {                       
                        eb.BackgroundImage = new System.Drawing.Bitmap(Resources.folder);
                        eb.DoubleClick += OpenFolder;
                    }
                    else
                    {
                        eb.BackgroundImage = new System.Drawing.Bitmap(Resources.file);
                        eb.DoubleClick += ReadFile;
                    }

                    eb.SingleClick += SetFocusName;
                  
                    curName.Add(eb);
                    tb.Location = new Point(curX-15, curY + deltaNameY);
                    tb.TextAlign = HorizontalAlignment.Center;
                    curX += deltaX;                  
                    curButo.Add(tb);
                    this.Controls.Add(tb);
                    this.Controls.Add(eb);
                }
                curY += deltaY;
            }
            string temp = "";
            for (i = 0; i < docSys.curPath.Count - 1; ++i)
            {
                temp += docSys.curPath[i];
                temp += "\\";
            }
            temp += docSys.curPath[i];
            textBox1.Text = temp;
        }
        public void SetFocusName(String s)
        {
            focusName = s;
            textBox3.Text = focusName;
        }
        private void OpenFolder()
        {
            docSys.OpenFile(focusName);
            SetFocusName("");
            RefreshFolder();

        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(isInFile)
            {
                MessageBox.Show("You are in a file!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            String s = Interaction.InputBox("Please enter the name of the file you want to create (no more than 12 characters and in English) ", "CreateFile");
            if (docSys.IsChildExisted(s))
            {
                MessageBox.Show("duplicated file name!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            if (s.Length == 0)
            {
                MessageBox.Show("empty file name!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            if (s.Length > 12)
            {
                MessageBox.Show("too long file name!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            docSys.CreatFileOnCur(s, false);
            RefreshFolder();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (isInFile || focusName == "") return;
            docSys.DeleteFile(focusName);
            SetFocusName("");
            RefreshFolder();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            docSys.BackOff();
            isInFile = false;
            RefreshFolder();
        }
        private void RefreshFile()
        {
            foreach (Control obj in curButo)
            {
                this.Controls.Remove(obj);
            }
            foreach (Control obj in curName)
            {
                this.Controls.Remove(obj);
            }
            curButo.Clear();
            curName.Clear();
            TextBox tb = new TextBox();
            tb.ReadOnly = true;
            tb.Size = new Size(582, 1000);
            tb.Location = new Point(10, 80);
            tb.Text = (docSys.ReadFile((int)docSys.FCBLevel.Peek()));
            curName.Add(tb);
            this.Controls.Add(tb);
        }
        private void ReadFile()
        {
            foreach (Control obj in curButo)
            {
                this.Controls.Remove(obj);
            }
            foreach (Control obj in curName)
            {
                this.Controls.Remove(obj);
            }
            curButo.Clear();
            curName.Clear();
            TextBox tb = new TextBox();            
            tb.ReadOnly = true;
            tb.Size = new Size(582, 1000);
            tb.Location = new Point(10, 80);
            docSys.OpenFile(focusName);
            tb.Text=(docSys.ReadFile((int)docSys.FCBLevel.Peek()));
            curButo.Add(tb);
            curName.Add(tb);
            this.Controls.Add(tb);
            string temp = "";
            int i;
            for (i = 0; i < docSys.curPath.Count - 1; ++i)
            {
                temp += docSys.curPath[i];
                temp += "\\";
            }
            temp += docSys.curPath[i];
            textBox1.Text = temp;
            SetFocusName("");
            isInFile = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(docSys.GetCurType())
            {
                MessageBox.Show("please open a file!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            String s = Interaction.InputBox("Please enter the string you want to write:", "WriteFile");
            if(!docSys.WriteFile((int)docSys.FCBLevel.Peek(),s))
            {
                MessageBox.Show("Space is full, some information may be lost!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }
            RefreshFile();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            FileStream dataFile = new FileStream(docSys.dataFileName, FileMode.OpenOrCreate, FileAccess.Write);
           
            BinaryWriter dataBinaryWriter = new BinaryWriter(dataFile);
            int i;
            for (i = 0; i < 512*64*1024;  ++i)
            {
                dataBinaryWriter.Write(docSys.disk[i]);
            }
            dataFile.Close();
            MessageBox.Show("Saved successfully!", "", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
    }
}
