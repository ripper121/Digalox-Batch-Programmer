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
            richTextBoxLog.Location = new Point(12, 46);
            richTextBoxLog.Name = "richTextBoxLog";
            richTextBoxLog.Size = new Size(776, 392);
            richTextBoxLog.TabIndex = 2;
            richTextBoxLog.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
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
    }
}
