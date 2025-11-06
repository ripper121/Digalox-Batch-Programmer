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
            SuspendLayout();
            // 
            // comboBoxComPorts
            // 
            comboBoxComPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxComPorts.Enabled = false;
            comboBoxComPorts.FormattingEnabled = true;
            comboBoxComPorts.Location = new Point(12, 12);
            comboBoxComPorts.Name = "comboBoxComPorts";
            comboBoxComPorts.Size = new Size(251, 28);
            comboBoxComPorts.TabIndex = 1;
            // 
            // richTextBoxLog
            // 
            richTextBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxLog.BackColor = SystemColors.ScrollBar;
            richTextBoxLog.Location = new Point(12, 81);
            richTextBoxLog.Name = "richTextBoxLog";
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.Size = new Size(560, 357);
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
            buttonWriteFile.Enabled = false;
            buttonWriteFile.Location = new Point(369, 12);
            buttonWriteFile.Name = "buttonWriteFile";
            buttonWriteFile.Size = new Size(94, 29);
            buttonWriteFile.TabIndex = 4;
            buttonWriteFile.Text = "Write";
            buttonWriteFile.UseVisualStyleBackColor = true;
            buttonWriteFile.Click += buttonWriteFile_Click;
            // 
            // checkBoxAuto
            // 
            checkBoxAuto.AutoSize = true;
            checkBoxAuto.Enabled = false;
            checkBoxAuto.Location = new Point(469, 15);
            checkBoxAuto.Name = "checkBoxAuto";
            checkBoxAuto.Size = new Size(103, 24);
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
            progressBarWrite.Location = new Point(12, 46);
            progressBarWrite.Name = "progressBarWrite";
            progressBarWrite.Size = new Size(560, 29);
            progressBarWrite.TabIndex = 6;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 450);
            Controls.Add(progressBarWrite);
            Controls.Add(checkBoxAuto);
            Controls.Add(buttonWriteFile);
            Controls.Add(buttonLoadFile);
            Controls.Add(richTextBoxLog);
            Controls.Add(comboBoxComPorts);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Digalox Batch Programmer v1.0";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
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
    }
}
