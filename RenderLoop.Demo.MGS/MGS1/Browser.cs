// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Windows.Forms;
    using DiscUtils.Iso9660;
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

            var entry = (string.Empty, false, false);
            this.AddFileSystem(entry, fs);
            this.Navigate(entry);
        }

        private void AddFileSystem(Entry entry, IFileSystem fileSystem)
        {
            this.fileSystems.Add(entry.Path, fileSystem);
            this.fileTree.Nodes.Add(new TreeNode(entry.Path == string.Empty ? "Root" : Path.GetFileName(entry.Path), 0, 0, [this.CreateExpanderDummy(entry)]) { Tag = entry });
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
                    AddFileSystem(entry, fs);
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
                    .Select(e => new ListViewItem(Path.GetFileName(e.Path), e.IsFile ? e.IsNestedFileSystem ? 2 : 1 : 0) { Tag = e })
                    .ToArray();
                this.entryList.Items.Clear();
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
            if (item?.Tag is Entry entry && IsFolderLike(entry))
            {
                this.Navigate(entry);
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
    }
}
