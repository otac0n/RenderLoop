namespace RenderLoop.Demo.MGS.MGS1
{
    partial class Browser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(Browser));
            this.pathBox = new System.Windows.Forms.TextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.fileTree = new System.Windows.Forms.TreeView();
            this.entryList = new System.Windows.Forms.ListView();
            this.fileTypes = new System.Windows.Forms.ImageList(this.components);
            this.topToolStrip = new System.Windows.Forms.ToolStrip();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            this.viewDrowDown = new System.Windows.Forms.ToolStripDropDownButton();
            this.listToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smallIconsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lowerStatusStrip = new System.Windows.Forms.StatusStrip();
            this.saveSelectedDialog = new System.Windows.Forms.SaveFileDialog();
            this.saveToFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            ((System.ComponentModel.ISupportInitialize)this.splitContainer2).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.topToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // pathBox
            // 
            this.pathBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.pathBox.Location = new System.Drawing.Point(0, 33);
            this.pathBox.Name = "pathBox";
            this.pathBox.Size = new System.Drawing.Size(1203, 31);
            this.pathBox.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 64);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.fileTree);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.entryList);
            this.splitContainer2.Size = new System.Drawing.Size(1203, 702);
            this.splitContainer2.SplitterDistance = 401;
            this.splitContainer2.TabIndex = 1;
            // 
            // fileTree
            // 
            this.fileTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileTree.Location = new System.Drawing.Point(0, 0);
            this.fileTree.Name = "fileTree";
            this.fileTree.Size = new System.Drawing.Size(401, 702);
            this.fileTree.TabIndex = 0;
            this.fileTree.BeforeExpand += this.FileTree_BeforeExpand;
            this.fileTree.AfterSelect += this.FileTree_AfterSelect;
            // 
            // entryList
            // 
            this.entryList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.entryList.LargeImageList = this.fileTypes;
            this.entryList.Location = new System.Drawing.Point(0, 0);
            this.entryList.Name = "entryList";
            this.entryList.Size = new System.Drawing.Size(798, 702);
            this.entryList.SmallImageList = this.fileTypes;
            this.entryList.TabIndex = 0;
            this.entryList.UseCompatibleStateImageBehavior = false;
            this.entryList.View = System.Windows.Forms.View.List;
            this.entryList.ItemActivate += this.EntryList_ItemActivate;
            this.entryList.SelectedIndexChanged += this.EntryList_SelectedIndexChanged;
            // 
            // fileTypes
            // 
            this.fileTypes.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.fileTypes.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("fileTypes.ImageStream");
            this.fileTypes.TransparentColor = System.Drawing.Color.Transparent;
            this.fileTypes.Images.SetKeyName(0, "folder");
            this.fileTypes.Images.SetKeyName(1, "file");
            this.fileTypes.Images.SetKeyName(2, "streamline-icon-common-file-stack@20x20.png");
            this.fileTypes.Images.SetKeyName(3, "streamline-icon-image-file-camera@20x20.png");
            this.fileTypes.Images.SetKeyName(4, "streamline-icon-video-file-camera@20x20.png");
            this.fileTypes.Images.SetKeyName(5, "streamline-icon-audio-file-volume@20x20.png");
            // 
            // topToolStrip
            // 
            this.topToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.topToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.saveButton, this.viewDrowDown });
            this.topToolStrip.Location = new System.Drawing.Point(0, 0);
            this.topToolStrip.Name = "topToolStrip";
            this.topToolStrip.Size = new System.Drawing.Size(1203, 33);
            this.topToolStrip.TabIndex = 2;
            this.topToolStrip.Text = "toolStrip1";
            // 
            // saveButton
            // 
            this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveButton.Enabled = false;
            this.saveButton.Image = Properties.Resources.streamline_icon_floppy_disk_20x20;
            this.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(34, 28);
            this.saveButton.Text = "&Save";
            this.saveButton.Click += this.SaveButton_Click;
            // 
            // viewDrowDown
            // 
            this.viewDrowDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.viewDrowDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.listToolStripMenuItem, this.smallIconsToolStripMenuItem });
            this.viewDrowDown.Image = Properties.Resources.streamline_icon_cog_20x20;
            this.viewDrowDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.viewDrowDown.Name = "viewDrowDown";
            this.viewDrowDown.Size = new System.Drawing.Size(42, 28);
            this.viewDrowDown.Text = "View";
            // 
            // listToolStripMenuItem
            // 
            this.listToolStripMenuItem.Name = "listToolStripMenuItem";
            this.listToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.listToolStripMenuItem.Text = "List";
            this.listToolStripMenuItem.Click += this.ListToolStripMenuItem_Click;
            // 
            // smallIconsToolStripMenuItem
            // 
            this.smallIconsToolStripMenuItem.Name = "smallIconsToolStripMenuItem";
            this.smallIconsToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.smallIconsToolStripMenuItem.Text = "Small Icons";
            this.smallIconsToolStripMenuItem.Click += this.SmallIconsToolStripMenuItem_Click;
            // 
            // lowerStatusStrip
            // 
            this.lowerStatusStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.lowerStatusStrip.Location = new System.Drawing.Point(0, 766);
            this.lowerStatusStrip.Name = "lowerStatusStrip";
            this.lowerStatusStrip.Size = new System.Drawing.Size(1203, 22);
            this.lowerStatusStrip.TabIndex = 3;
            this.lowerStatusStrip.Text = "statusStrip1";
            // 
            // saveSelectedDialog
            // 
            this.saveSelectedDialog.InitialDirectory = "%USERPROFILE%\\Downloads";
            // 
            // saveToFolderDialog
            // 
            this.saveToFolderDialog.InitialDirectory = "%USERPROFILE%\\Downloads";
            this.saveToFolderDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // Browser
            // 
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1203, 788);
            this.Controls.Add(this.splitContainer2);
            this.Controls.Add(this.lowerStatusStrip);
            this.Controls.Add(this.pathBox);
            this.Controls.Add(this.topToolStrip);
            this.Name = "Browser";
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.splitContainer2).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.topToolStrip.ResumeLayout(false);
            this.topToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
        private System.Windows.Forms.TextBox pathBox;
        private System.Windows.Forms.ListView entryList;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ImageList fileTypes;
        private System.Windows.Forms.TreeView fileTree;
        private System.Windows.Forms.ToolStrip topToolStrip;
        private System.Windows.Forms.ToolStripDropDownButton viewDrowDown;
        private System.Windows.Forms.ToolStripMenuItem listToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smallIconsToolStripMenuItem;
        private System.Windows.Forms.StatusStrip lowerStatusStrip;
        private System.Windows.Forms.ToolStripButton saveButton;
        private System.Windows.Forms.SaveFileDialog saveSelectedDialog;
        private System.Windows.Forms.FolderBrowserDialog saveToFolderDialog;
    }
}
