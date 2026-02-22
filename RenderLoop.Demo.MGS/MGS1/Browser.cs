// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Forms;
    using DiscUtils.Iso9660;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.MGS1.Archives;
    using Entry = Archives.NestedFileSystemManager.Entry;

    internal partial class Browser : Form
    {
        private readonly NestedFileSystemManager fsm;

        public Browser(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var fs = serviceProvider.GetRequiredKeyedService<MArchiveV1VirtualFileSystem>(WellKnownPaths.AllDataBin);

            this.InitializeComponent();
            this.saveSelectedDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveSelectedDialog.InitialDirectory);
            this.saveToFolderDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveToFolderDialog.InitialDirectory);

            this.fsm = new NestedFileSystemManager(fs,
                (file, fs, fsPath, subPath) =>
                {
                    if (fs is MArchiveV1VirtualFileSystem &&
                    string.Equals(Path.GetExtension(file), ".bin", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(subPath) == "roms")
                    {
                        return static (IFileSystem fs, string subPath) =>
                        {
                            var file = fs.File.OpenRead(subPath);
                            var cdSector = new CDSectorStream(file, CDSectorStream.XAForm1);
                            var cdReader = new CDReader(cdSector, joliet: false);
                            var subFs = new CDReaderVFSAdapter(cdReader);
                            return subFs;
                        };
                    }
                    else if (fs is CDReaderVFSAdapter &&
                        string.Equals(Path.GetExtension(file), ".dir", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileName(subPath) == "MGS")
                    {
                        return static (IFileSystem fs, string subPath) =>
                        {
                            var file = fs.File.OpenRead(subPath);
                            var subFs = new StageDirVirtualFileSystem(file);
                            return subFs;
                        };
                    }

                    return null;
                });
            this.fileTree.Nodes.Add(new TreeNode("Root", 0, 0, [this.CreateExpanderDummy()]) { Tag = this.fsm.RootEntry });
            this.Navigate(this.fsm.RootEntry);
        }

        private TreeNode CreateExpanderDummy() => new("...");

        private static int DetectFileType(Entry entry) =>
            !entry.IsFile ? 0 :
            entry.IsNestedFileSystem ? 2 :
            string.Equals(Path.GetExtension(entry.Path), ".pcx", StringComparison.OrdinalIgnoreCase) ? 3 :
            1;

        private void Navigate(Entry entry)
        {
            this.pathBox.Text = entry.Path;
            if (this.fsm.TryFindParentFileSystem(entry.Path, out var fs, out var _, out var subPath))
            {
                var entries = this.fsm.EnumerateEntries(entry)
                    .Select(e => new ListViewItem(Path.GetFileName(e.Path), DetectFileType(e)) { Tag = e })
                    .ToArray();
                this.entryList.Items.Clear();
                this.EntryList_SelectedIndexChanged(this.entryList, EventArgs.Empty);
                this.entryList.Items.AddRange(entries);
            }
        }

        private static bool IsFolderLike(Entry entry) => !entry.IsFile || entry.IsNestedFileSystem;

        private void FileTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is Entry entry && e.Node.Nodes is [TreeNode onlyChild] && onlyChild.Text == "...")
            {
                e.Node.Nodes.Clear();
                var entries = this.fsm.EnumerateEntries(entry).Where(IsFolderLike);
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
                if (IsFolderLike(entry))
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
        }

        private void SmallIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.View = View.SmallIcon;
        }

        private void EntryList_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.saveButton.Enabled = this.entryList.SelectedItems.Count >= 1 && this.entryList.SelectedItems.Cast<ListViewItem>().All(i => i.Tag is Entry entry && entry.IsFile);
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
