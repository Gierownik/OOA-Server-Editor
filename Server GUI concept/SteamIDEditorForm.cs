using System;
using System.Windows.Forms;

namespace Server_GUI_concept
{
    public partial class SteamIDEditorForm : Form
    {
        public string SteamIDs { get => textBox.Text; set => textBox.Text = value; }

        public SteamIDEditorForm(string label)
        {
            InitializeComponent();
            labelPrompt.Text = label;
        }

        private void InitializeComponent()
        {
            this.labelPrompt = new Label();
            this.textBox = new TextBox();
            this.buttonOK = new Button();

            this.SuspendLayout();

            labelPrompt.AutoSize = true;
            labelPrompt.Location = new System.Drawing.Point(12, 9);
            labelPrompt.Text = "Prompt";

            textBox.Location = new System.Drawing.Point(12, 30);
            textBox.Size = new System.Drawing.Size(360, 20);
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            buttonOK.Text = "OK";
            buttonOK.Location = new System.Drawing.Point(297, 60);
            buttonOK.Click += (s, e) => this.DialogResult = DialogResult.OK;

            this.Controls.Add(labelPrompt);
            this.Controls.Add(textBox);
            this.Controls.Add(buttonOK);
            this.ClientSize = new System.Drawing.Size(384, 100);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Edit SteamIDs";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Label labelPrompt;
        private TextBox textBox;
        private Button buttonOK;
    }
}
