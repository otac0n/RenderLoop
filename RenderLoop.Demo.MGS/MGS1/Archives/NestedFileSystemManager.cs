namespace RenderLoop.Demo.MGS.MGS1.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;

    public class NestedFileSystemManager
    {
        public delegate Func<IFileSystem, string, IFileSystem>? Handler(string file, IFileSystem fileSystem, string fileSystemPath);

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
                subPath = fs.Path.Combine(rest, parent == string.Empty ? path : fs.Path.GetRelativePath(parent, path));
                if (this.GetOrAddFactory(path, fs, fsPath, out var factory))
                {
                    fs = factory(fs, subPath);
                    this.fileSystems.Add(path, fs);
                    this.nestedFactories.Remove(path);
                    fsPath = path;
                    subPath = string.Empty;
                }

                return true;
            }

            fs = null;
            fsPath = null;
            subPath = path;
            return false;
        }

        public IEnumerable<Entry> EnumerateEntries(string path, bool recursive = false)
        {
            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                foreach (var entry in this.EnumerateEntries(stack.Pop()))
                {
                    yield return entry;

                    if (recursive && entry.CanEnumerateEntries)
                    {
                        stack.Push(entry.Path);
                    }
                }
            }
        }

        public IEnumerable<Entry> EnumerateFiles(string path, string searchPattern, bool recursive = false)
        {
            var glob = PathExtensions.GlobToRegex(searchPattern);
            foreach (var entry in this.EnumerateEntries(path, recursive))
            {
                if (!entry.CanOpen || !glob.IsMatch(Path.GetFileName(entry.Path)))
                {
                    continue;
                }

                yield return entry;
            }
        }

        public bool FileExists(string path)
        {
            if (!this.TryFindParentFileSystem(path, out var fs, out _, out var subPath))
            {
                throw new FileNotFoundException(path);
            }

            return fs.File.Exists(subPath);
        }

        public Stream OpenRead(string path)
        {
            if (!this.TryFindParentFileSystem(path, out var fs, out _, out var subPath))
            {
                throw new FileNotFoundException(path);
            }

            return fs.File.OpenRead(subPath);
        }

        private IEnumerable<Entry> EnumerateEntries(string path)
        {
            if (this.TryFindParentFileSystem(path, out var fs, out var fsPath, out var subPath))
            {
                foreach (var d in fs.Directory.EnumerateDirectories(subPath))
                {
                    yield return new(fs.Path.CombineIgnoringAbsolute(fsPath, d), false, true);
                }

                foreach (var f in fs.Directory.EnumerateFiles(subPath))
                {
                    var p = fs.Path.CombineIgnoringAbsolute(fsPath, f);
                    yield return new(p, true, this.IsNestedFileSystem(p, fs, fsPath));
                }
            }
        }

        private bool IsNestedFileSystem(string file, IFileSystem fs, string fsPath)
        {
            if (this.fileSystems.ContainsKey(file))
            {
                return true;
            }

            return this.GetOrAddFactory(file, fs, fsPath, out _);
        }

        private bool GetOrAddFactory(string file, IFileSystem fs, string fsPath, [NotNullWhen(true)] out Func<IFileSystem, string, IFileSystem>? factory)
        {
            if (!this.nestedFactories.TryGetValue(file, out factory))
            {
                this.nestedFactories[file] = factory = this.GetNestedFactory(file, fs, fsPath);
            }

            return factory is not null;
        }

        private Func<IFileSystem, string, IFileSystem>? GetNestedFactory(string file, IFileSystem fs, string fsPath) =>
            this.handlers.Select(h => h(file, fs, fsPath)).FirstOrDefault(f => f is not null);

        public record Entry(string Path, bool CanOpen, bool CanEnumerateEntries);
    }
}
