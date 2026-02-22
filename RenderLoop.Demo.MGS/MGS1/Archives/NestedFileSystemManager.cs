namespace RenderLoop.Demo.MGS.MGS1.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using DiscUtils.Iso9660;

    internal class NestedFileSystemManager
    {
        public delegate Func<IFileSystem, string, IFileSystem>? Handler(string file, IFileSystem fileSystem, string? fileSystemPath, string nestedPath);

        private readonly Dictionary<string, Func<IFileSystem, string, IFileSystem>?> nestedFactories = new();
        private readonly Dictionary<string, IFileSystem> fileSystems = new();
        private readonly Handler[] handlers;

        public NestedFileSystemManager(MArchiveV1VirtualFileSystem fs, params Handler[] handlers)
        {
            this.handlers = handlers;
            this.fileSystems.Add(string.Empty, fs);
            this.RootEntry = new(string.Empty, false, false);
        }

        public Entry RootEntry { get; }

        public bool TryFindParentFileSystem(string path, [NotNullWhen(true)] out IFileSystem? fs, [NotNullWhen(true)] out string? fsPath, out string subPath)
        {
            if (this.fileSystems.TryGetValue(path, out fs))
            {
                fsPath = path;
                subPath = string.Empty;
                return true;
            }

            if (PathExtensions.GetDirectoryName(path) is string parent && this.TryFindParentFileSystem(parent, out fs, out fsPath, out var rest))
            {
                subPath = Path.Combine(rest, parent == string.Empty ? path : Path.GetRelativePath(parent, path));

                return true;
            }

            fs = null;
            fsPath = null;
            subPath = path;
            return false;
        }

        public IEnumerable<Entry> EnumerateEntries(Entry entry)
        {
            if (this.TryFindParentFileSystem(entry.Path, out var fs, out var fsPath, out var subPath))
            {
                if (entry.IsNestedFileSystem && subPath != string.Empty)
                {
                    if (!this.nestedFactories.TryGetValue(entry.Path, out var factory) || factory is null)
                    {
                        throw new InvalidOperationException($"No factory registered for nested file system at path '{entry.Path}'.");
                    }

                    fs = factory(fs, subPath);
                    this.fileSystems.Add(entry.Path, fs);
                    this.nestedFactories.Remove(entry.Path);
                    fsPath = entry.Path;
                    subPath = string.Empty;
                }

                foreach (var d in fs.Directory.EnumerateDirectories(subPath))
                {
                    yield return new(PathExtensions.CombineIgnoringAbsolute(fsPath, d), false, false);
                }

                foreach (var f in fs.Directory.EnumerateFiles(subPath))
                {
                    var path = PathExtensions.CombineIgnoringAbsolute(fsPath, f);
                    yield return new(path, true, this.IsNestedFileSystem(path, fs, fsPath, subPath));
                }
            }
        }

        private bool IsNestedFileSystem(string file, IFileSystem fs, string? fsPath, string subPath)
        {
            if (this.fileSystems.ContainsKey(file))
            {
                return true;
            }

            if (!this.nestedFactories.TryGetValue(file, out var factory))
            {
                this.nestedFactories[file] = factory = this.GetNestedFactory(file, fs, fsPath, subPath);
            }

            return factory is not null;
        }

        private Func<IFileSystem, string, IFileSystem>? GetNestedFactory(string file, IFileSystem fs, string? fsPath, string subPath) =>
            this.handlers.Select(h => h(file, fs, fsPath, subPath)).FirstOrDefault(f => f is not null);

        public record Entry(string Path, bool IsFile, bool IsNestedFileSystem);
    }
}
