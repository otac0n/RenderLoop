// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.MGS1.Archives;
    using Entry = Archives.NestedFileSystemManager.Entry;

    internal partial class Browser : Form
    {
        private readonly NestedFileSystemManager fsm;
        private readonly VirtualImageList<Entry> textureDisplay;

        public Browser(IServiceProvider serviceProvider)
        {
            this.fsm = serviceProvider.GetRequiredKeyedService<NestedFileSystemManager>(WellKnownPaths.AllDataBin);

            this.InitializeComponent();
            this.saveSelectedDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveSelectedDialog.InitialDirectory);
            this.saveToFolderDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveToFolderDialog.InitialDirectory);
            this.textureDisplay = new VirtualImageList<Entry>(
                entry =>
                {
                    using var textureFile = this.fsm.OpenRead(entry.Path);
                    return Task.FromResult(new MagickImage(textureFile).ToBitmap());
                },
                InterpolationMode.NearestNeighbor)
            {
                Dock = DockStyle.Fill,
                Visible = false,
            };
            this.splitContainer.Panel2.Controls.Add(this.textureDisplay);

            this.fileTree.Nodes.Add(new TreeNode(WellKnownPaths.AllDataBin, 0, 0, [this.CreateExpanderDummy()]) { Tag = this.fsm.RootEntry });
            this.Navigate(this.fsm.RootEntry);
        }

        private TreeNode CreateExpanderDummy() => new("...");

        private static int DetectFileType(Entry entry) =>
            entry.CanEnumerateEntries && !entry.CanOpen ? 0 :
            entry.CanEnumerateEntries ? 2 :
            string.Equals(Path.GetExtension(entry.Path), ".pcx", StringComparison.OrdinalIgnoreCase) ? 3 : // TODO: Integrate with DetectFileType.
            1;

        private void Navigate(Entry entry)
        {
            this.pathBox.Text = entry.Path;
            if (this.fsm.TryFindParentFileSystem(entry.Path, out var fs, out var _, out var subPath))
            {
                var entries = this.fsm.EnumerateEntries(entry.Path);
                var items = entries
                    .Select(e => new ListViewItem(Path.GetFileName(e.Path), DetectFileType(e)) { Tag = e })
                    .ToArray();
                this.entryList.Items.Clear();
                this.EntryList_SelectedIndexChanged(this.entryList, EventArgs.Empty);
                this.entryList.Items.AddRange(items);

                this.textureDisplay.Items = entries.Where(e => string.Equals(Path.GetExtension(e.Path), ".pcx", StringComparison.OrdinalIgnoreCase)); // TODO: Integrate with DetectFileType.
            }
        }

        private void FileTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is Entry entry && e.Node.Nodes is [TreeNode onlyChild] && onlyChild.Text == "...")
            {
                e.Node.Nodes.Clear();
                var entries = this.fsm.EnumerateEntries(entry.Path).Where(e => e.CanEnumerateEntries);
                e.Node.Nodes.AddRange([.. entries.Select(e => new TreeNode(Path.GetFileName(e.Path), 0, 0, [this.CreateExpanderDummy()]) { Tag = e })]);
            }
        }

        private void FileTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is Entry entry)
            {
                this.Navigate(entry);
            }
        }

        private void EntryList_ItemActivate(object sender, EventArgs e)
        {
            var item = this.entryList.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
            if (item?.Tag is Entry entry)
            {
                if (entry.CanEnumerateEntries)
                {
                    this.Navigate(entry);
                }
                else
                {
                    // TODO: Integrate with DetectFileType.
                    if (string.Equals(Path.GetExtension(entry.Path), ".pcx", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.fsm.TryFindParentFileSystem(entry.Path, out var fs, out var _, out var subPath))
                        {
                            var file = fs.File.OpenRead(subPath);
                            var childForm = new Form();
                            childForm.Controls.Add(new PictureBox
                            {
                                Dock = DockStyle.Fill,
                                SizeMode = PictureBoxSizeMode.Zoom,
                                Image = new MagickImage(file).ToBitmap(),
                                BackColor = Color.Black,
                            });
                            childForm.Show(this);
                        }
                    }
                }
            }
        }

        private void ListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.View = View.List;
            this.entryList.Visible = true;
            this.textureDisplay.Visible = false;
            this.listToolStripMenuItem.Checked = true;
            this.smallIconsToolStripMenuItem.Checked = false;
            this.imagePreviewToolStripMenuItem.Checked = false;
        }

        private void SmallIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.View = View.SmallIcon;
            this.entryList.Visible = true;
            this.textureDisplay.Visible = false;
            this.listToolStripMenuItem.Checked = false;
            this.smallIconsToolStripMenuItem.Checked = true;
            this.imagePreviewToolStripMenuItem.Checked = false;
        }

        private void ImagePreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.Visible = false;
            this.textureDisplay.Visible = true;
            this.listToolStripMenuItem.Checked = false;
            this.smallIconsToolStripMenuItem.Checked = false;
            this.imagePreviewToolStripMenuItem.Checked = true;
        }

        private void EntryList_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.saveButton.Enabled = this.entryList.SelectedItems.Count >= 1 && this.entryList.SelectedItems.Cast<ListViewItem>().All(i => i.Tag is Entry entry && entry.CanOpen);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (this.entryList.SelectedItems.Count == 1)
            {
                var entry = (Entry)this.entryList.SelectedItems[0]?.Tag!;
                if (!this.fsm.TryFindParentFileSystem(entry.Path, out var fs, out var _, out var subPath))
                {
                    return;
                }

                using var input = fs.File.OpenRead(subPath);

                MagickImageInfo? fileInfo = null;
                try
                {
                    fileInfo = new MagickImageInfo(input);
                }
                catch (MagickMissingDelegateErrorException)
                {
                }
                finally
                {
                    input.Seek(0, SeekOrigin.Begin);
                }

                this.saveSelectedDialog.Filter = fileInfo != null
                    ? "Image Files|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.pcx|All Files|*.*"
                    : "All Files|*.*";

                this.saveSelectedDialog.FileName = Path.GetFileName(entry.Path);
                var result = this.saveSelectedDialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }

                if (fs != null)
                {
                    var path = this.saveSelectedDialog.FileName;
                    if (Path.GetExtension(path) != Path.GetExtension(subPath))
                    {
                        using var image = new MagickImage(input);
                        image.Write(path);
                    }
                    else
                    {
                        using var output = File.Create(path);
                        input.CopyTo(output);
                    }
                }
            }
            else if (this.entryList.SelectedItems.Count >= 0)
            {
                var entries = this.entryList.SelectedItems.Cast<ListViewItem>().Select(i => (Entry)i.Tag).ToList();

                this.saveToFolderDialog.SelectedPath = string.Empty;
                var result = this.saveToFolderDialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }

                var path = this.saveToFolderDialog.SelectedPath;
                var targetFiles = entries.Select(e => (Source: e.Path, Target: Path.Combine(path, Path.GetFileName(e.Path)))).ToList();
                if (targetFiles.Any(t => File.Exists(t.Target)))
                {
                    var overwriteResult = MessageBox.Show($"The destination path \"{path}\" already contians files with the same name. Do you want to overwrite?", "Confirm Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (overwriteResult != DialogResult.Yes)
                    {
                        return;
                    }
                }

                foreach (var (source, target) in targetFiles)
                {
                    if (this.fsm.TryFindParentFileSystem(source, out var fs, out var _, out var subPath))
                    {
                        using var input = fs.File.OpenRead(subPath);
                        using var output = File.Create(target);
                        input.CopyTo(output);
                    }
                }
            }
        }
    }
}
