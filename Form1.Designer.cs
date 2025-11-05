namespace Digalox_Batch_Programmer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonOpen = new Button();
            comboBoxComPorts = new ComboBox();
            richTextBoxLog = new RichTextBox();
            openFileDialog1 = new OpenFileDialog();
            buttonLoadFile = new Button();
            buttonWriteFile = new Button();
            SuspendLayout();
            // 
            // buttonOpen
            // 
            buttonOpen.Location = new Point(169, 12);
            buttonOpen.Name = "buttonOpen";
            buttonOpen.Size = new Size(94, 29);
            buttonOpen.TabIndex = 0;
            buttonOpen.Text = "Open";
            buttonOpen.UseVisualStyleBackColor = true;
            buttonOpen.Click += buttonOpen_Click;
            // 
            // comboBoxComPorts
            // 
            comboBoxComPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxComPorts.FormattingEnabled = true;
            comboBoxComPorts.Location = new Point(12, 12);
            comboBoxComPorts.Name = "comboBoxComPorts";
            comboBoxComPorts.Size = new Size(151, 28);
            comboBoxComPorts.TabIndex = 1;
            // 
            // richTextBoxLog
            // 
            richTextBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxLog.BackColor = SystemColors.ScrollBar;
            richTextBoxLog.Location = new Point(12, 47);
            richTextBoxLog.Name = "richTextBoxLog";
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.Size = new Size(776, 391);
            richTextBoxLog.TabIndex = 2;
            richTextBoxLog.Text = "";
            richTextBoxLog.WordWrap = false;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // buttonLoadFile
            // 
            buttonLoadFile.Location = new Point(269, 12);
            buttonLoadFile.Name = "buttonLoadFile";
            buttonLoadFile.Size = new Size(94, 29);
            buttonLoadFile.TabIndex = 3;
            buttonLoadFile.Text = "Load File";
            buttonLoadFile.UseVisualStyleBackColor = true;
            buttonLoadFile.Click += buttonLoadFile_Click;
            // 
            // buttonWriteFile
            // 
            buttonWriteFile.Location = new Point(369, 12);
            buttonWriteFile.Name = "buttonWriteFile";
            buttonWriteFile.Size = new Size(94, 29);
            buttonWriteFile.TabIndex = 4;
            buttonWriteFile.Text = "Write";
            buttonWriteFile.UseVisualStyleBackColor = true;
            buttonWriteFile.Click += buttonWriteFile_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(buttonWriteFile);
            Controls.Add(buttonLoadFile);
            Controls.Add(richTextBoxLog);
            Controls.Add(comboBoxComPorts);
            Controls.Add(buttonOpen);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button buttonOpen;
        private ComboBox comboBoxComPorts;
        private RichTextBox richTextBoxLog;
        private OpenFileDialog openFileDialog1;
        private Button buttonLoadFile;
        private Button buttonWriteFile;
    }
}
