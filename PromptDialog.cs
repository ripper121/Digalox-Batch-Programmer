using System;
using System.Drawing;
using System.Windows.Forms;

namespace Digalox_Batch_Programmer
{
 /// <summary>
 /// Simple modal prompt dialog that returns a string input or null if cancelled.
 /// </summary>
 public class PromptDialog : Form
 {
 private TextBox _textBox;
 private Label _label;
 private Button _okButton;
 private Button _cancelButton;

 public string ResponseText => _textBox.Text;

 public PromptDialog(string title, string prompt, string defaultText = "")
 {
 Text = title;
 StartPosition = FormStartPosition.CenterParent;
 FormBorderStyle = FormBorderStyle.FixedDialog;
 MaximizeBox = false;
 MinimizeBox = false;
 ShowInTaskbar = false;
 ClientSize = new Size(400,130);

 _label = new Label()
 {
 Text = prompt,
 AutoSize = false,
 Location = new Point(12,8),
 Size = new Size(ClientSize.Width -24,40)
 };
 Controls.Add(_label);

 _textBox = new TextBox()
 {
 Location = new Point(12,52),
 Size = new Size(ClientSize.Width -24,23),
 Text = defaultText
 };
 Controls.Add(_textBox);

 _okButton = new Button()
 {
 Text = "OK",
 DialogResult = DialogResult.OK,
 Location = new Point(ClientSize.Width -180,85),
 Size = new Size(80,28)
 };
 Controls.Add(_okButton);

 _cancelButton = new Button()
 {
 Text = "Cancel",
 DialogResult = DialogResult.Cancel,
 Location = new Point(ClientSize.Width -90,85),
 Size = new Size(80,28)
 };
 Controls.Add(_cancelButton);

 AcceptButton = _okButton;
 CancelButton = _cancelButton;
 }

 /// <summary>
 /// Shows the prompt dialog and returns the entered string, or null if cancelled.
 /// </summary>
 public static string? Show(IWin32Window owner, string title, string prompt, string defaultText = "")
 {
 using (var dlg = new PromptDialog(title, prompt, defaultText))
 {
 var res = owner == null ? dlg.ShowDialog() : dlg.ShowDialog(owner);
 if (res == DialogResult.OK)
 return dlg.ResponseText;
 return null;
 }
 }
 }
}
