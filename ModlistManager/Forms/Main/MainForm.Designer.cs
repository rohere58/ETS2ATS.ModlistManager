using System.Drawing;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Main
{
    partial class MainForm
    {

        private TableLayoutPanel tableHeader;
        private Label lblGame;
        private Label lblProfile;
        private Label lblModlist;
        private ComboBox cbGame;
        private ComboBox cbProfile;
    private ComboBox cbModlist;
    private ETS2ATS.ModlistManager.Controls.HeaderBannerControl headerBanner;

    private FlowLayoutPanel panelActions;
    private Button btnAdopt, btnCreate, btnTextCheck;

        // Grid + Spalten
        private DataGridView gridMods;
        private DataGridViewTextBoxColumn colIndex;
        private DataGridViewTextBoxColumn colPackage;
        private DataGridViewTextBoxColumn colModName;
    private DataGridViewTextBoxColumn colInfo;
    private DataGridViewTextBoxColumn colUrl;
    
    private DataGridViewButtonColumn colDownload;
    private DataGridViewButtonColumn colSearch;

        // Footer
        private Panel footerPanel;
        private TableLayoutPanel footerLayout;
        private PictureBox pbGameLogo;
        private TableLayoutPanel footerCenterPanel;
        private Label lblModInfo;
        private TextBox txtModInfo;
    private Panel footerRightPanel;
    private TableLayoutPanel footerRightLayout; // 2 Zeilen: Header (23px) + Log (Fill)
    private TableLayoutPanel footerRightHeader; // Header mit Titel links, Status/Undo rechts
    private FlowLayoutPanel rightHeaderFlow;    // rechts: Undo-Link + Status nebeneinander (rechtsbündig)
    private Label lblStatus;
    private LinkLabel linkUndo;
    private TextBox txtLog;
    private Label lblStatusHeader;
    private ContextMenuStrip cmsGrid;
    private ToolStripMenuItem miAddLink;
    private ToolStripMenuItem miRemoveLink;
    private ToolStripMenuItem miAddLinkPerList;
    private ToolStripMenuItem miRemoveLinkPerList;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            tableHeader = new TableLayoutPanel();
            lblGame = new Label();
            cbGame = new ComboBox();
            lblProfile = new Label();
            cbProfile = new ComboBox();
            lblModlist = new Label();
            cbModlist = new ComboBox();
            headerBanner = new ETS2ATS.ModlistManager.Controls.HeaderBannerControl();
            panelActions = new FlowLayoutPanel();
            btnAdopt = new Button();
            btnCreate = new Button();
            btnTextCheck = new Button();
            gridMods = new DataGridView();
            colIndex = new DataGridViewTextBoxColumn();
            colPackage = new DataGridViewTextBoxColumn();
            colModName = new DataGridViewTextBoxColumn();
            colInfo = new DataGridViewTextBoxColumn();
            
            colDownload = new DataGridViewButtonColumn();
            colSearch = new DataGridViewButtonColumn();
            colUrl = new DataGridViewTextBoxColumn();
            footerPanel = new Panel();
            footerLayout = new TableLayoutPanel();
            pbGameLogo = new PictureBox();
            footerCenterPanel = new TableLayoutPanel();
            lblModInfo = new Label();
            txtModInfo = new TextBox();
            footerRightPanel = new Panel();
            miProfiles = new ToolStripMenuItem();
            miProfClone = new ToolStripMenuItem();
            miProfRename = new ToolStripMenuItem();
            miProfDelete = new ToolStripMenuItem();
            miProfOpen = new ToolStripMenuItem();
            miModlists = new ToolStripMenuItem();
            miModOpen = new ToolStripMenuItem();
            miModShare = new ToolStripMenuItem();
            miModImport = new ToolStripMenuItem();
            miModDelete = new ToolStripMenuItem();
            miBackup = new ToolStripMenuItem();
            miBkAll = new ToolStripMenuItem();
            miBkRestore = new ToolStripMenuItem();
            miBkSii = new ToolStripMenuItem();
            miTools = new ToolStripMenuItem();
            miDonate = new ToolStripMenuItem();
            miOptions = new ToolStripMenuItem();
            miOptsOpen = new ToolStripMenuItem();
            miHelp = new ToolStripMenuItem();
            miAbout = new ToolStripMenuItem();
            menuMain = new MenuStrip();
            tableHeader.SuspendLayout();
            // no PictureBox init for headerBanner
            panelActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridMods).BeginInit();
            cmsGrid = new ContextMenuStrip();
            footerPanel.SuspendLayout();
            footerLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbGameLogo).BeginInit();
            footerCenterPanel.SuspendLayout();
            menuMain.SuspendLayout();
            SuspendLayout();
            // 
            // (Title label removed per design refresh)
            // 
            // tableHeader
            // 
            tableHeader.AutoSize = true;
            tableHeader.ColumnCount = 3;
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableHeader.Controls.Add(lblGame, 0, 0);
            tableHeader.Controls.Add(cbGame, 1, 0);
            tableHeader.Controls.Add(lblProfile, 0, 1);
            tableHeader.Controls.Add(cbProfile, 1, 1);
            tableHeader.Controls.Add(lblModlist, 0, 2);
            tableHeader.Controls.Add(cbModlist, 1, 2);
            tableHeader.Controls.Add(headerBanner, 2, 0);
            tableHeader.Dock = DockStyle.Top;
            tableHeader.Location = new Point(0, 27);
            tableHeader.Name = "tableHeader";
            tableHeader.Padding = new Padding(10);
            tableHeader.RowCount = 3;
            tableHeader.RowStyles.Add(new RowStyle());
            tableHeader.RowStyles.Add(new RowStyle());
            tableHeader.RowStyles.Add(new RowStyle());
            tableHeader.Size = new Size(1384, 107);
            tableHeader.TabIndex = 1;
            // 
            // lblGame
            // 
            lblGame.Anchor = AnchorStyles.Left;
            lblGame.AutoSize = true;
            lblGame.Location = new Point(13, 17);
            lblGame.Name = "lblGame";
            lblGame.Size = new Size(35, 15);
            lblGame.TabIndex = 0;
            lblGame.Tag = "MainForm.Game";
            lblGame.Text = "Spiel:";
            lblGame.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbGame
            // 
            cbGame.Anchor = AnchorStyles.Left;
            cbGame.DropDownStyle = ComboBoxStyle.DropDownList;
            cbGame.Location = new Point(75, 13);
            cbGame.Name = "cbGame";
            cbGame.Size = new Size(280, 23);
            cbGame.TabIndex = 1;
            // 
            // lblProfile
            // 
            lblProfile.Anchor = AnchorStyles.Left;
            lblProfile.AutoSize = true;
            lblProfile.Location = new Point(13, 46);
            lblProfile.Name = "lblProfile";
            lblProfile.Size = new Size(38, 15);
            lblProfile.TabIndex = 2;
            lblProfile.Tag = "MainForm.Profile";
            lblProfile.Text = "Profil:";
            lblProfile.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbProfile
            // 
            cbProfile.Anchor = AnchorStyles.Left;
            cbProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            cbProfile.Location = new Point(75, 42);
            cbProfile.Name = "cbProfile";
            cbProfile.Size = new Size(280, 23);
            cbProfile.TabIndex = 3;
            // 
            // lblModlist
            // 
            lblModlist.Anchor = AnchorStyles.Left;
            lblModlist.AutoSize = true;
            lblModlist.Location = new Point(13, 75);
            lblModlist.Name = "lblModlist";
            lblModlist.Size = new Size(56, 15);
            lblModlist.TabIndex = 4;
            lblModlist.Tag = "MainForm.Modlist";
            lblModlist.Text = "Modliste:";
            lblModlist.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbModlist
            // 
            cbModlist.Anchor = AnchorStyles.Left;
            cbModlist.DropDownStyle = ComboBoxStyle.DropDownList;
            cbModlist.Location = new Point(75, 71);
            cbModlist.Name = "cbModlist";
            cbModlist.Size = new Size(280, 23);
            cbModlist.TabIndex = 5;
            // 
            // headerBanner
            // 
            headerBanner.Dock = DockStyle.Fill;
            headerBanner.Location = new Point(380, 10);
            headerBanner.Margin = new Padding(8, 0, 0, 0);
            headerBanner.Name = "headerBanner";
            tableHeader.SetRowSpan(headerBanner, 3);
            headerBanner.Size = new Size(994, 87);
            headerBanner.TabIndex = 6;
            headerBanner.BackgroundOpacity = 0.8f; // halbtransparentes Banner
            // 
            // panelActions
            // 
            panelActions.AutoSize = true;
            panelActions.Controls.Add(btnAdopt);
            panelActions.Controls.Add(btnCreate);
            panelActions.Controls.Add(btnTextCheck);
            panelActions.Dock = DockStyle.Top;
            panelActions.Location = new Point(0, 174);
            panelActions.Name = "panelActions";
            panelActions.Padding = new Padding(8);
            panelActions.Size = new Size(1384, 45);
            panelActions.TabIndex = 0;
            panelActions.WrapContents = false;
            // 
            // btnAdopt
            // 
            btnAdopt.Location = new Point(11, 11);
            btnAdopt.Name = "btnAdopt";
            btnAdopt.Size = new Size(136, 23);
            btnAdopt.TabIndex = 0;
            btnAdopt.Tag = "MainForm.Toolbar.Adopt";
            btnAdopt.Text = "Modliste übernehmen";
            // 
            // btnCreate
            // 
            btnCreate.Location = new Point(153, 11);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new Size(131, 23);
            btnCreate.TabIndex = 1;
            btnCreate.Tag = "MainForm.Toolbar.Create";
            btnCreate.Text = "Modliste erstellen";
        // 
        // btnTextCheck
        // 
        btnTextCheck.Location = new Point(295, 11);
        btnTextCheck.Name = "btnTextCheck";
        btnTextCheck.Size = new Size(110, 23);
        btnTextCheck.TabIndex = 2;
        btnTextCheck.Tag = "MainForm.Toolbar.TextCheck";
        btnTextCheck.Text = "Text-Check";
            // 
            // gridMods
            // 
            // Context menu for grid
            cmsGrid.SuspendLayout();
            cmsGrid.Name = "cmsGrid";
            cmsGrid.ShowImageMargin = false;
            miAddLink = new ToolStripMenuItem();
            miAddLink.Name = "miAddLink";
            miAddLink.Tag = "MainForm.Grid.AddLink";
            miAddLink.Text = "Download-Link hinzufügen…";
            miRemoveLink = new ToolStripMenuItem();
            miRemoveLink.Name = "miRemoveLink";
            miRemoveLink.Tag = "MainForm.Grid.RemoveLink";
            miRemoveLink.Text = "Download-Link entfernen";
            miAddLinkPerList = new ToolStripMenuItem();
            miAddLinkPerList.Name = "miAddLinkPerList";
            miAddLinkPerList.Tag = "MainForm.Grid.AddLinkPerList";
            miAddLinkPerList.Text = "In Modlisten-Links speichern…";
            miRemoveLinkPerList = new ToolStripMenuItem();
            miRemoveLinkPerList.Name = "miRemoveLinkPerList";
            miRemoveLinkPerList.Tag = "MainForm.Grid.RemoveLinkPerList";
            miRemoveLinkPerList.Text = "Aus Modlisten-Links entfernen";
            cmsGrid.Items.AddRange(new ToolStripItem[] { miAddLink, miRemoveLink, new ToolStripSeparator(), miAddLinkPerList, miRemoveLinkPerList });
            cmsGrid.ResumeLayout(false);

            gridMods.AllowUserToAddRows = false;
            gridMods.AllowUserToDeleteRows = false;
            gridMods.AllowUserToOrderColumns = true;
            gridMods.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridMods.BackgroundColor = SystemColors.Window;
            gridMods.ReadOnly = false;
            gridMods.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            gridMods.Columns.AddRange(new DataGridViewColumn[] { colIndex, colPackage, colModName, colInfo, colDownload, colSearch, colUrl });
            gridMods.Dock = DockStyle.Fill;
            gridMods.Location = new Point(0, 219);
            gridMods.MultiSelect = false;
            gridMods.Name = "gridMods";
            gridMods.RowHeadersVisible = false;
            gridMods.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridMods.Size = new Size(1384, 502);
            gridMods.TabIndex = 0;
            gridMods.ContextMenuStrip = cmsGrid;
            // 
            // colIndex
            // 
            colIndex.FillWeight = 10F;
            colIndex.HeaderText = "#";
            colIndex.Name = "colIndex";
            colIndex.ReadOnly = true;
            // 
            // colPackage
            // 
            colPackage.FillWeight = 120F;
            colPackage.HeaderText = "Package";
            colPackage.Name = "colPackage";
            colPackage.ReadOnly = true;
            // 
            // colModName
            // 
            colModName.FillWeight = 160F;
            colModName.HeaderText = "Modname";
            colModName.Name = "colModName";
            colModName.ReadOnly = true;
            // 
            // colInfo
            // 
            colInfo.FillWeight = 220F;
            colInfo.HeaderText = "Info";
            colInfo.Name = "colInfo";
            colInfo.ReadOnly = false;
            // 
            // colUrl (hidden)
            // 
            colUrl.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colUrl.HeaderText = "Url";
            colUrl.Name = "colUrl";
            colUrl.ReadOnly = true;
            colUrl.Visible = false;
            colUrl.Width = 5;
            // 
            
            // 
            // colDownload
            // 
            colDownload.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colDownload.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colDownload.Width = 100;
            colDownload.MinimumWidth = 100;
            colDownload.HeaderText = "Download";
            colDownload.Name = "colDownload";
            colDownload.Text = "Download"; // Standardtext, wird pro Zeile überschrieben
            colDownload.UseColumnTextForButtonValue = false;
            // 
            // colSearch
            // 
            colSearch.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colSearch.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colSearch.Width = 90;
            colSearch.MinimumWidth = 90;
            colSearch.HeaderText = "Suche";
            colSearch.Name = "colSearch";
            colSearch.Text = "Suchen";
            colSearch.UseColumnTextForButtonValue = true;
            // 
            // footerPanel
            // 
            footerPanel.Controls.Add(footerLayout);
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Location = new Point(0, 721);
            footerPanel.Name = "footerPanel";
            footerPanel.Padding = new Padding(10, 8, 10, 8);
            footerPanel.Size = new Size(1384, 140);
            footerPanel.TabIndex = 1;
            // 
            // footerLayout
            // 
            footerLayout.ColumnCount = 3;
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            footerLayout.Controls.Add(pbGameLogo, 0, 0);
            footerLayout.Controls.Add(footerCenterPanel, 1, 0);
            footerLayout.Controls.Add(footerRightPanel, 2, 0);
            footerLayout.Dock = DockStyle.Fill;
            footerLayout.Location = new Point(10, 8);
            footerLayout.Name = "footerLayout";
            footerLayout.RowCount = 1;
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            footerLayout.Size = new Size(1364, 124);
            footerLayout.TabIndex = 0;
            // 
            // pbGameLogo
            // 
            pbGameLogo.Anchor = AnchorStyles.Left;
            pbGameLogo.Location = new Point(0, 12);
            pbGameLogo.Margin = new Padding(0);
            pbGameLogo.Name = "pbGameLogo";
            pbGameLogo.Size = new Size(180, 100);
            pbGameLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pbGameLogo.TabIndex = 0;
            pbGameLogo.TabStop = false;
            // 
            // footerCenterPanel
            // 
            footerCenterPanel.ColumnCount = 1;
            footerCenterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            footerCenterPanel.Controls.Add(lblModInfo, 0, 0);
            footerCenterPanel.Controls.Add(txtModInfo, 0, 1);
            footerCenterPanel.Dock = DockStyle.Fill;
            footerCenterPanel.Location = new Point(203, 3);
            footerCenterPanel.Name = "footerCenterPanel";
            footerCenterPanel.Padding = new Padding(0, 0, 0, 4);
            footerCenterPanel.RowCount = 2;
            footerCenterPanel.RowStyles.Add(new RowStyle());
            footerCenterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            footerCenterPanel.Size = new Size(958, 118);
            footerCenterPanel.TabIndex = 1;
            // 
            // lblModInfo
            // 
            lblModInfo.Dock = DockStyle.Top;
            lblModInfo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblModInfo.Location = new Point(0, 0);
            lblModInfo.Margin = new Padding(0, 0, 0, 4);
            lblModInfo.Name = "lblModInfo";
            lblModInfo.Size = new Size(958, 23);
            lblModInfo.TabIndex = 0;
            lblModInfo.Tag = "MainForm.ModInfo";
            lblModInfo.Text = "Infos zur Modliste";
            lblModInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtModInfo
            // 
            txtModInfo.Dock = DockStyle.Fill;
            txtModInfo.Location = new Point(3, 30);
            txtModInfo.Multiline = true;
            txtModInfo.Name = "txtModInfo";
            txtModInfo.ScrollBars = ScrollBars.Vertical;
            txtModInfo.Size = new Size(952, 81);
            txtModInfo.TabIndex = 1;
            // Kein Tag für txtModInfo – enthält benutzerdefinierten Inhalt, darf nicht lokalisiert werden
            // 
            // footerRightPanel
            // 
            footerRightPanel.Dock = DockStyle.Fill;
            footerRightPanel.Location = new Point(1167, 3);
            footerRightPanel.MinimumSize = new Size(180, 0);
            footerRightPanel.Name = "footerRightPanel";
            footerRightPanel.Size = new Size(194, 118);
            footerRightPanel.TabIndex = 2;
            footerRightPanel.Padding = new Padding(0, 0, 0, 4);

            // footerRightLayout (2 Zeilen: 23px + *)
            footerRightLayout = new TableLayoutPanel();
            footerRightLayout.Dock = DockStyle.Fill;
            footerRightLayout.ColumnCount = 1;
            footerRightLayout.RowCount = 2;
            footerRightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            footerRightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            footerRightPanel.Controls.Add(footerRightLayout);

            // footerRightHeader (2 Spalten: 50% | 50%)
            footerRightHeader = new TableLayoutPanel();
            footerRightHeader.Dock = DockStyle.Fill;
            footerRightHeader.ColumnCount = 2;
            footerRightHeader.RowCount = 1;
            footerRightHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            footerRightHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            footerRightLayout.Controls.Add(footerRightHeader, 0, 0);
            // 
            // lblStatusHeader (links in Header-Zeile)
            lblStatusHeader = new Label();
            lblStatusHeader.Dock = DockStyle.Fill;
            lblStatusHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblStatusHeader.Margin = new Padding(0);
            lblStatusHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStatusHeader.Name = "lblStatusHeader";
            lblStatusHeader.Tag = "MainForm.Status.Header";
            lblStatusHeader.Text = "Status & Log";
            footerRightHeader.Controls.Add(lblStatusHeader, 0, 0);

            // rechter Header-Bereich: FlowLayout (rechtsbündig)
            rightHeaderFlow = new FlowLayoutPanel();
            rightHeaderFlow.Dock = DockStyle.Fill;
            rightHeaderFlow.FlowDirection = FlowDirection.RightToLeft;
            rightHeaderFlow.WrapContents = false;
            rightHeaderFlow.Margin = new Padding(0);

            // linkUndo (optional sichtbar)
            linkUndo = new LinkLabel();
            linkUndo.Name = "linkUndo";
            linkUndo.Text = "";
            linkUndo.AutoSize = true;
            linkUndo.Visible = false;
            linkUndo.Margin = new Padding(8, 3, 0, 3);

            // lblStatus (rechts ausgerichtet)
            lblStatus = new Label();
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "";
            lblStatus.AutoSize = true;
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
            lblStatus.Margin = new Padding(0, 3, 0, 3);

            rightHeaderFlow.Controls.Add(linkUndo);
            rightHeaderFlow.Controls.Add(lblStatus);
            footerRightHeader.Controls.Add(rightHeaderFlow, 1, 0);

            // txtLog (füllt die zweite Zeile)
            txtLog = new TextBox();
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.Name = "txtLog";
            footerRightLayout.Controls.Add(txtLog, 0, 1);
            // 
            // miProfiles
            // 
            miProfiles.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miProfiles.DropDownItems.AddRange(new ToolStripItem[] { miProfClone, miProfRename, miProfDelete, miProfOpen });
            miProfiles.Name = "miProfiles";
            miProfiles.Size = new Size(57, 21);
            miProfiles.Tag = "MainForm.Menu.Profiles";
            miProfiles.Text = "Profile";
            // 
            // miProfClone
            // 
            miProfClone.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miProfClone.Name = "miProfClone";
            miProfClone.Size = new Size(194, 22);
            miProfClone.Tag = "MainForm.Menu.Profiles.Clone";
            miProfClone.Text = "Profil klonen…";
            // 
            // miProfRename
            // 
            miProfRename.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miProfRename.Name = "miProfRename";
            miProfRename.Size = new Size(194, 22);
            miProfRename.Tag = "MainForm.Menu.Profiles.Rename";
            miProfRename.Text = "Profil umbenennen…";
            // 
            // miProfDelete
            // 
            miProfDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miProfDelete.Name = "miProfDelete";
            miProfDelete.Size = new Size(194, 22);
            miProfDelete.Tag = "MainForm.Menu.Profiles.Delete";
            miProfDelete.Text = "Profil löschen…";
            // 
            // miProfOpen
            // 
            miProfOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miProfOpen.Name = "miProfOpen";
            miProfOpen.Size = new Size(194, 22);
            miProfOpen.Tag = "MainForm.Menu.Profiles.OpenFolder";
            miProfOpen.Text = "Profilordner öffnen";
            // 
            // miModlists
            // 
            miModlists.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miModlists.DropDownItems.AddRange(new ToolStripItem[] { miModOpen, miModShare, miModImport, miModDelete });
            miModlists.Name = "miModlists";
            miModlists.Size = new Size(78, 21);
            miModlists.Tag = "MainForm.Menu.Modlists";
            miModlists.Text = "Modlisten";
            // 
            // miModOpen
            // 
            miModOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miModOpen.Name = "miModOpen";
            miModOpen.Size = new Size(215, 22);
            miModOpen.Tag = "MainForm.Menu.Modlists.OpenFolder";
            miModOpen.Text = "Modlistenordner öffnen";
            // 
            // miModShare
            // 
            miModShare.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miModShare.Name = "miModShare";
            miModShare.Size = new Size(215, 22);
            miModShare.Tag = "MainForm.Menu.Modlists.Share";
            miModShare.Text = "Weitergeben…";
            // 
            // miModImport
            // 
            miModImport.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miModImport.Name = "miModImport";
            miModImport.Size = new Size(215, 22);
            miModImport.Tag = "MainForm.Menu.Modlists.Import";
            miModImport.Text = "Importieren…";
            // 
            // miModDelete
            // 
            miModDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miModDelete.Name = "miModDelete";
            miModDelete.Size = new Size(215, 22);
            miModDelete.Tag = "MainForm.Menu.Modlists.Delete";
            miModDelete.Text = "Löschen…";
            // 
            // miBackup
            // 
            miBackup.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miBackup.DropDownItems.AddRange(new ToolStripItem[] { miBkAll, miBkRestore, miBkSii });
            miBackup.Name = "miBackup";
            miBackup.Size = new Size(114, 21);
            miBackup.Tag = "MainForm.Menu.Backup";
            miBackup.Text = "Backup & Restore";
            // 
            // miBkAll
            // 
            miBkAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miBkAll.Name = "miBkAll";
            miBkAll.Size = new Size(237, 22);
            miBkAll.Tag = "MainForm.Menu.Backup.AllProfiles";
            miBkAll.Text = "Alle Profile sichern…";
            // 
            // miBkRestore
            // 
            miBkRestore.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miBkRestore.Name = "miBkRestore";
            miBkRestore.Size = new Size(237, 22);
            miBkRestore.Tag = "MainForm.Menu.Backup.RestoreProfiles";
            miBkRestore.Text = "Profile wiederherstellen…";
            // 
            // miBkSii
            // 
            miBkSii.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miBkSii.Name = "miBkSii";
            miBkSii.Size = new Size(237, 22);
            miBkSii.Tag = "MainForm.Menu.Backup.RestoreSii";
            miBkSii.Text = "profile.sii wiederherstellen…";
            // 
            // miTools entfernt; Donate wird unter Hilfe einsortiert
            // miDonate
            miDonate.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miDonate.Name = "miDonate";
            miDonate.Size = new Size(158, 22);
            miDonate.Tag = "MainForm.Menu.Tools.Donate";
            miDonate.Text = "Donate (Ko-fi)";
            // 
            // miOptions
            // 
            miOptions.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miOptions.DropDownItems.AddRange(new ToolStripItem[] { miOptsOpen });
            miOptions.Name = "miOptions";
            miOptions.Size = new Size(74, 21);
            miOptions.Tag = "MainForm.Menu.Options";
            miOptions.Text = "Optionen";
            // 
            // miOptsOpen
            // 
            miOptsOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miOptsOpen.Name = "miOptsOpen";
            miOptsOpen.Size = new Size(180, 22);
            miOptsOpen.Tag = "MainForm.Menu.Options.Open";
            miOptsOpen.Text = "Optionen…";
            // 
            // miHelp
            // 
            miHelp.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miHelp.DropDownItems.AddRange(new ToolStripItem[] { miAbout, miDonate });

            // miFaq (wird dynamisch vor Donate einsortiert)
            var miFaq = new ToolStripMenuItem();
            miFaq.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miFaq.Name = "miFaq";
            miFaq.Size = new Size(180, 22);
            miFaq.Tag = "MainForm.Menu.Help.Faq";
            miFaq.Text = "FAQ";
            miFaq.Click += (s, e) => { try { ShowFaq(); } catch { } };
            // Einfügen direkt hinter About (Index 0) und vor Donate
            miHelp.DropDownItems.Insert(1, miFaq);
            miHelp.Name = "miHelp";
            miHelp.Size = new Size(46, 21);
            miHelp.Tag = "MainForm.Menu.Help";
            miHelp.Text = "Hilfe";
            // 
            // miAbout
            // 
            miAbout.DisplayStyle = ToolStripItemDisplayStyle.Text;
            miAbout.Name = "miAbout";
            miAbout.Size = new Size(114, 22);
            miAbout.Tag = "MainForm.Menu.Help.About";
            miAbout.Text = "Über…";
            // 
            // menuMain
            // 
            menuMain.BackColor = SystemColors.Control;
            menuMain.Font = new Font("Segoe UI", 9.5F);
            menuMain.ForeColor = SystemColors.ControlText;
            menuMain.ImageScalingSize = new Size(20, 20);
            menuMain.Items.AddRange(new ToolStripItem[] { miProfiles, miModlists, miBackup, miOptions, miHelp });
            menuMain.Location = new Point(0, 0);
            menuMain.Name = "menuMain";
            menuMain.Padding = new Padding(6, 3, 6, 3);
            menuMain.RenderMode = ToolStripRenderMode.System;
            menuMain.ShowItemToolTips = true;
            menuMain.Size = new Size(1384, 27);
            menuMain.TabIndex = 3;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1384, 861);
            Controls.Add(gridMods);
            Controls.Add(footerPanel);
            Controls.Add(panelActions);
            Controls.Add(tableHeader);
            Controls.Add(menuMain);
            Font = new Font("Segoe UI", 9F);
            MainMenuStrip = menuMain;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ETS2/ATS Modlist Manager";
            tableHeader.ResumeLayout(false);
            tableHeader.PerformLayout();
            panelActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridMods).EndInit();
            footerPanel.ResumeLayout(false);
            footerLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbGameLogo).EndInit();
            footerCenterPanel.ResumeLayout(false);
            footerCenterPanel.PerformLayout();
            menuMain.ResumeLayout(false);
            menuMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        private ToolStripMenuItem miProfiles;
        private ToolStripMenuItem miProfClone;
        private ToolStripMenuItem miProfRename;
        private ToolStripMenuItem miProfDelete;
        private ToolStripMenuItem miProfOpen;
        private ToolStripMenuItem miModlists;
        private ToolStripMenuItem miModOpen;
    private ToolStripMenuItem miModShare;
    private ToolStripMenuItem miModImport;
    private ToolStripMenuItem miModDelete;
        private ToolStripMenuItem miBackup;
        private ToolStripMenuItem miBkAll;
        private ToolStripMenuItem miBkRestore;
        private ToolStripMenuItem miBkSii;
        private ToolStripMenuItem miTools;
        private ToolStripMenuItem miDonate;
        private ToolStripMenuItem miOptions;
        private ToolStripMenuItem miOptsOpen;
        private ToolStripMenuItem miHelp;
        private ToolStripMenuItem miAbout;
        private MenuStrip menuMain;
    }
}

