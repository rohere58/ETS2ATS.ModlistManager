using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Main
{
    public partial class MainForm
    {
        private void EnsureLanguageTags()
        {
            // Menu
            if (miProfiles != null) miProfiles.Tag = "MainForm.Menu.Profiles";
            if (miModlists != null) miModlists.Tag = "MainForm.Menu.Modlists";
            if (miBackup   != null) miBackup.Tag   = "MainForm.Menu.Backup";
            if (miOptions  != null) miOptions.Tag  = "MainForm.Menu.Options";
            if (miHelp     != null) miHelp.Tag     = "MainForm.Menu.Help";

            // Untermen�s (entsprechend Designer-Namen)
            if (miProfClone  != null) miProfClone.Tag  = "MainForm.Menu.Profiles.Clone";
            if (miProfRename != null) miProfRename.Tag = "MainForm.Menu.Profiles.Rename";
            if (miProfDelete != null) miProfDelete.Tag = "MainForm.Menu.Profiles.Delete";
            if (miProfOpen   != null) miProfOpen.Tag   = "MainForm.Menu.Profiles.OpenFolder";

            if (miModOpen   != null) miModOpen.Tag   = "MainForm.Menu.Modlists.OpenFolder";
            if (miModShare  != null) miModShare.Tag  = "MainForm.Menu.Modlists.Share";
            if (miModImport != null) miModImport.Tag = "MainForm.Menu.Modlists.Import";
            if (miModDelete != null) miModDelete.Tag = "MainForm.Menu.Modlists.Delete";

            if (miBkAll     != null) miBkAll.Tag     = "MainForm.Menu.Backup.All";
            if (miBkRestore != null) miBkRestore.Tag = "MainForm.Menu.Backup.Restore";
            if (miBkSii     != null) miBkSii.Tag     = "MainForm.Menu.Backup.RestoreSii";

            // Donate bleibt unter Hilfe einsortiert, behält aus Kompatibilitätsgründen den bestehenden Schlüssel
            if (miDonate   != null) miDonate.Tag   = "MainForm.Menu.Tools.Donate";
            if (miOptsOpen != null) miOptsOpen.Tag = "MainForm.Menu.Options.Open";
            if (miAbout    != null) miAbout.Tag    = "MainForm.Menu.Help.About";

            // Header
            if (lblGame    != null) lblGame.Tag    = "MainForm.Game";
            if (lblProfile != null) lblProfile.Tag = "MainForm.Profile";
            if (lblModlist != null) lblModlist.Tag = "MainForm.Modlist";

            // Buttons
            if (btnAdopt   != null) btnAdopt.Tag   = "MainForm.Toolbar.Adopt";
            if (btnCreate  != null) btnCreate.Tag  = "MainForm.Toolbar.Create";
            if (btnTextCheck != null) btnTextCheck.Tag = "MainForm.Toolbar.TextCheck";

            // Grid
            if (colIndex    != null) colIndex.Tag    = "MainForm.Grid.Index";
            if (colPackage  != null) colPackage.Tag  = "MainForm.Grid.Package";
            if (colModName  != null) colModName.Tag  = "MainForm.Grid.ModName";
            if (colInfo     != null) colInfo.Tag     = "MainForm.Grid.Info";
            
            if (colDownload != null) colDownload.Tag = "MainForm.Grid.Download";
            if (colSearch   != null) colSearch.Tag   = "MainForm.Grid.Search";

            // Footer
            if (lblModInfo != null) lblModInfo.Tag = "MainForm.ModInfo";
            if (lblStatus  != null) lblStatus.Tag  = "MainForm.Status.Ready";
        }

        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);
            if (IsDesignTime) return;
            EnsureLanguageTags();
            try { _lang.Load(_settings.Current.Language ?? "de"); } catch { }
            ApplyLanguage();
        }
    }
}