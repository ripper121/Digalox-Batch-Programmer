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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            comboBoxComPorts = new ComboBox();
            richTextBoxLog = new RichTextBox();
            openFileDialog1 = new OpenFileDialog();
            buttonLoadFile = new Button();
            buttonWriteFile = new Button();
            checkBoxAuto = new CheckBox();
            timerCheck = new System.Windows.Forms.Timer(components);
            progressBarWrite = new ProgressBar();
            checkBoxCRC = new CheckBox();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // comboBoxComPorts
            // 
            comboBoxComPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxComPorts.Enabled = false;
            comboBoxComPorts.FormattingEnabled = true;
            comboBoxComPorts.Location = new Point(10, 9);
            comboBoxComPorts.Margin = new Padding(3, 2, 3, 2);
            comboBoxComPorts.Name = "comboBoxComPorts";
            comboBoxComPorts.Size = new Size(220, 23);
            comboBoxComPorts.TabIndex = 1;
            // 
            // richTextBoxLog
            // 
            richTextBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxLog.BackColor = SystemColors.ScrollBar;
            richTextBoxLog.Location = new Point(10, 62);
            richTextBoxLog.Margin = new Padding(3, 2, 3, 2);
            richTextBoxLog.Name = "richTextBoxLog";
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.Size = new Size(550, 420);
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
            buttonLoadFile.Location = new Point(236, 8);
            buttonLoadFile.Margin = new Padding(3, 2, 3, 2);
            buttonLoadFile.Name = "buttonLoadFile";
            buttonLoadFile.Size = new Size(82, 22);
            buttonLoadFile.TabIndex = 3;
            buttonLoadFile.Text = "Load File";
            buttonLoadFile.UseVisualStyleBackColor = true;
            buttonLoadFile.Click += buttonLoadFile_Click;
            // 
            // buttonWriteFile
            // 
            buttonWriteFile.Enabled = false;
            buttonWriteFile.Location = new Point(396, 8);
            buttonWriteFile.Margin = new Padding(3, 2, 3, 2);
            buttonWriteFile.Name = "buttonWriteFile";
            buttonWriteFile.Size = new Size(82, 22);
            buttonWriteFile.TabIndex = 4;
            buttonWriteFile.Text = "Write";
            buttonWriteFile.UseVisualStyleBackColor = true;
            buttonWriteFile.Click += buttonWriteFile_Click;
            // 
            // checkBoxAuto
            // 
            checkBoxAuto.AutoSize = true;
            checkBoxAuto.Enabled = false;
            checkBoxAuto.Location = new Point(484, 11);
            checkBoxAuto.Margin = new Padding(3, 2, 3, 2);
            checkBoxAuto.Name = "checkBoxAuto";
            checkBoxAuto.Size = new Size(83, 19);
            checkBoxAuto.TabIndex = 5;
            checkBoxAuto.Text = "Auto Write";
            checkBoxAuto.UseVisualStyleBackColor = true;
            // 
            // timerCheck
            // 
            timerCheck.Enabled = true;
            timerCheck.Interval = 500;
            timerCheck.Tick += timerSetButtons_Tick;
            // 
            // progressBarWrite
            // 
            progressBarWrite.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBarWrite.Location = new Point(9, 36);
            progressBarWrite.Margin = new Padding(3, 2, 3, 2);
            progressBarWrite.Name = "progressBarWrite";
            progressBarWrite.Size = new Size(550, 22);
            progressBarWrite.TabIndex = 6;
            // 
            // checkBoxCRC
            // 
            checkBoxCRC.AutoSize = true;
            checkBoxCRC.Location = new Point(324, 11);
            checkBoxCRC.Name = "checkBoxCRC";
            checkBoxCRC.Size = new Size(66, 19);
            checkBoxCRC.TabIndex = 7;
            checkBoxCRC.Text = "Fix CRC";
            checkBoxCRC.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 489);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(571, 22);
            statusStrip1.TabIndex = 8;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(273, 17);
            toolStripStatusLabel1.Text = "Created in collaboration with Strenuous.dev (2025)";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(571, 511);
            Controls.Add(statusStrip1);
            Controls.Add(checkBoxCRC);
            Controls.Add(progressBarWrite);
            Controls.Add(checkBoxAuto);
            Controls.Add(buttonWriteFile);
            Controls.Add(buttonLoadFile);
            Controls.Add(richTextBoxLog);
            Controls.Add(comboBoxComPorts);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form1";
            Text = "Digalox Batch Programmer v1.1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox comboBoxComPorts;
        private RichTextBox richTextBoxLog;
        private OpenFileDialog openFileDialog1;
        private Button buttonLoadFile;
        private Button buttonWriteFile;
        private CheckBox checkBoxAuto;
        private System.Windows.Forms.Timer timerCheck;
        private ProgressBar progressBarWrite;
        private CheckBox checkBoxCRC;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
    }
}
