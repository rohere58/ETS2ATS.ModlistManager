using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Common
{
    partial class SiiDecryptHintForm
    {
        private System.ComponentModel.IContainer components = null;
    internal Label lblMessage;
    internal Label lblPathLabel;
    internal TextBox txtPath;
    internal CheckBox chkDontShow;
    internal Button btnOk;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.lblMessage = new Label();
            this.lblPathLabel = new Label();
            this.txtPath = new TextBox();
            this.chkDontShow = new CheckBox();
            this.btnOk = new Button();
            this.SuspendLayout();
            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = false;
            this.lblMessage.Text = "";
            this.lblMessage.Left = 12;
            this.lblMessage.Top = 12;
            this.lblMessage.Width = 460;
            this.lblMessage.Height = 60;
            this.lblMessage.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // lblPathLabel
            // 
            this.lblPathLabel.AutoSize = true;
            this.lblPathLabel.Text = "Erwarteter Pfad:";
            this.lblPathLabel.Left = 12;
            this.lblPathLabel.Top = 82;
            // 
            // txtPath
            // 
            this.txtPath.Left = 12;
            this.txtPath.Top = 102;
            this.txtPath.Width = 460;
            this.txtPath.ReadOnly = true;
            this.txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // chkDontShow
            // 
            this.chkDontShow.Left = 12;
            this.chkDontShow.Top = 142;
            this.chkDontShow.Width = 460;
            this.chkDontShow.Text = "Diesen Hinweis nicht mehr anzeigen";
            this.chkDontShow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // btnOk
            // 
            this.btnOk.Text = "OK";
            this.btnOk.Left = 397;
            this.btnOk.Top = 180;
            this.btnOk.Width = 75;
            this.btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnOk.DialogResult = DialogResult.OK;
            // 
            // SiiDecryptHintForm
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnOk;
            this.ClientSize = new System.Drawing.Size(484, 221);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblPathLabel);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.chkDontShow);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
