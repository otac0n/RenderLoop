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
    using Entry = (string Path, bool IsFile, bool IsNestedFileSystem);

    internal partial class Browser : Form
    {
        private readonly Dictionary<string, IFileSystem> fileSystems = new();

        public Browser(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<Program.Options>();
            var fs = serviceProvider.GetRequiredKeyedService<MArchiveV1VirtualFileSystem>(WellKnownPaths.AllDataBin);

            this.InitializeComponent();
            this.saveSelectedDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveSelectedDialog.InitialDirectory);
            this.saveToFolderDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveToFolderDialog.InitialDirectory);

            Entry entry = (string.Empty, false, false);
            this.fileSystems.Add(entry.Path, fs);
            this.fileTree.Nodes.Add(new TreeNode(entry.Path == string.Empty ? "Root" : Path.GetFileName(entry.Path), 0, 0, [this.CreateExpanderDummy(entry)]) { Tag = entry });
            this.Navigate(entry);
        }

        private TreeNode CreateExpanderDummy(Entry entry) => new("...");

        private IEnumerable<Entry> EnumerateEntries(Entry entry)
        {
            var fs = this.FindParentFileSystem(entry.Path, out var fsPath, out var subPath);
            if (fs != null)
            {
                if (entry.IsNestedFileSystem && subPath != string.Empty)
                {
                    fs = CreateNestedFileSystem(fs, subPath);
                    this.fileSystems.Add(entry.Path, fs);
                    fsPath = entry.Path;
                    subPath = string.Empty;
                }

                foreach (var d in fs.Directory.EnumerateDirectories(subPath))
                {
                    yield return (PathExtensions.CombineIgnoringAbsolute(fsPath, d), false, false);
                }

                foreach (var f in fs.Directory.EnumerateFiles(subPath))
                {
                    yield return (PathExtensions.CombineIgnoringAbsolute(fsPath, f), true, DetectNestedFileSystem(f, fs, fsPath, subPath));
                }
            }
        }

        private static int DetectFileType(Entry entry) =>
            !entry.IsFile ? 0 :
            entry.IsNestedFileSystem ? 2 :
            string.Equals(Path.GetExtension(entry.Path), ".pcx", StringComparison.OrdinalIgnoreCase) ? 3 :
            1;

        private bool DetectNestedFileSystem(string file, IFileSystem fs, string? fsPath, string subPath)
        {
            if (fs is MArchiveV1VirtualFileSystem &&
                string.Equals(Path.GetExtension(file), ".bin", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(subPath) == "roms")
            {
                return true;
            }
            else if (fs is CDReaderVFSAdapter &&
                string.Equals(Path.GetExtension(file), ".DIR", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(subPath) == "MGS")
            {
                return true;
            }

            return false;
        }

        private IFileSystem CreateNestedFileSystem(IFileSystem fs, string subPath)
        {
            if (fs is MArchiveV1VirtualFileSystem &&
                string.Equals(Path.GetExtension(subPath), ".bin", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(Path.GetDirectoryName(subPath)) == "roms")
            {
                var file = fs.File.OpenRead(subPath);
                var cdSector = new CDSectorStream(file, CDSectorStream.XAForm1);
                var cdReader = new CDReader(cdSector, joliet: false);
                var subFs = new CDReaderVFSAdapter(cdReader);
                return subFs;
            }
            else if (fs is CDReaderVFSAdapter &&
                string.Equals(Path.GetExtension(subPath), ".DIR", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(Path.GetDirectoryName(subPath)) == "MGS")
            {
                var file = fs.File.OpenRead(subPath);
                var subFs = new StageDirVirtualFileSystem(file);
                return subFs;
            }

            throw new InvalidOperationException($"Don't know how to create a nested file system for {subPath} in a {fs.GetType().Name}.");
        }

        private void Navigate(Entry entry)
        {
            this.pathBox.Text = entry.Path;
            var fs = this.FindParentFileSystem(entry.Path, out var _, out var subPath);
            if (fs != null)
            {
                var entries = EnumerateEntries(entry)
                    .Select(e => new ListViewItem(Path.GetFileName(e.Path), DetectFileType(e)) { Tag = e })
                    .ToArray();
                this.entryList.Items.Clear();
                this.EntryList_SelectedIndexChanged(this.entryList, EventArgs.Empty);
                this.entryList.Items.AddRange(entries);
            }
        }

        private IFileSystem? FindParentFileSystem(string path, out string? fsPath, out string subPath)
        {
            if (this.fileSystems.TryGetValue(path, out var fs))
            {
                fsPath = path;
                subPath = string.Empty;
                return fs;
            }

            var parent = PathExtensions.GetDirectoryName(path);
            if (parent == null)
            {
                fsPath = null;
                subPath = path;
                return null;
            }

            fs = this.FindParentFileSystem(parent, out fsPath, out var rest);
            subPath = Path.Combine(rest, parent == string.Empty ? path : Path.GetRelativePath(parent, path));
            return fs;
        }

        private static bool IsFolderLike(Entry entry) => !entry.IsFile || entry.IsNestedFileSystem;

        private void FileTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is Entry entry && e.Node.Nodes is [TreeNode onlyChild] && onlyChild.Text == "...")
            {
                e.Node.Nodes.Clear();
                var entries = this.EnumerateEntries(entry).Where(IsFolderLike);
                e.Node.Nodes.AddRange([.. entries.Select(e => new TreeNode(Path.GetFileName(e.Path), 0, 0, [this.CreateExpanderDummy(e)]) { Tag = e })]);
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
                        var file = this.FindParentFileSystem(entry.Path, out var _, out var subPath)!.File.OpenRead(subPath);
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
                var entry = (Entry)this.entryList.SelectedItems[0]?.Tag;
                var fs = this.FindParentFileSystem(entry.Path, out var _, out var subPath);
                using var input = fs.File.OpenRead(subPath);

                MagickImageInfo? fileInfo = null;
                try
                {
                    fileInfo = new MagickImageInfo(input);
                }
                catch
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
                    var fs = this.FindParentFileSystem(source, out var _, out var subPath);
                    if (fs != null)
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
