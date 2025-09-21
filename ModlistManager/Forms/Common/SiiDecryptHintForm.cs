using System;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Common
{
    public partial class SiiDecryptHintForm : Form
    {
        public bool DontShowAgain => chkDontShow.Checked;

        public SiiDecryptHintForm(string expectedPath)
        {
            InitializeComponent();
            this.Text = "SII_Decrypt.exe erforderlich";
            this.lblMessage.Text =
                "Hinweis: Ab dieser Version wird SII_Decrypt.exe nicht mehr mitgeliefert. Bitte lege die Datei manuell in den Ordner 'ModlistManager\\Tools' neben der Anwendung ab.";
            this.txtPath.Text = expectedPath ?? string.Empty;
        }
    }
}
