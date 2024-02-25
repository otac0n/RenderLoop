namespace RenderLoop.Demo.MGS
{
    using System.IO;
    using System;
    using System.IO.Abstractions;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;
    using RenderLoop.Demo.MGS.Archives;
    using DirEntry = (string name, long offset);
    using FileEntry = (string name, long offset, long size);

    public sealed class StageDirVirtualFileSystem : IFileSystem, IDisposable
    {
        private static readonly long SectorSize = 2048L;
        private static readonly char[] separators = ['/', '\\'];
        private static readonly ImmutableDictionary<byte, string> extensions = new Dictionary<byte, string>
        {
            [0x61] = "azm",
            [0x62] = "bin",
            [0x63] = "con",
            [0x64] = "dar",
            [0x65] = "efx",
            [0x67] = "gcx",
            [0x68] = "hzm",
            [0x69] = "img",
            [0x6b] = "kmd",
            [0x6c] = "lit",
            [0x6d] = "mdx",
            [0x6f] = "oar",
            [0x70] = "pcx",
            [0x72] = "res",
            [0x73] = "sgt",
            [0x77] = "wvx",
            [0x7a] = "zmd",
        }.ToImmutableDictionary();
        private static readonly ImmutableDictionary<byte, string> groups = new Dictionary<byte, string>
        {
            [0x63] = "model",
            [0x6e] = "texture",
            [0x73] = "sound",
        }.ToImmutableDictionary();

        private bool disposed;
        private readonly DirEntry[] index;
        private readonly Dictionary<string, FileEntry[]> fileEntries = [];
        private Stream sourceStream;

        public StageDirVirtualFileSystem(Stream sourceStream)
        {
            this.index = ReadIndex(sourceStream);
            this.sourceStream = sourceStream;

            this.Directory = new DirectoryProvider(this);
            this.File = new FileProvider(this);
            this.Path = new PathProvider(this);
        }

        public IDirectory Directory { get; }

        public IDirectoryInfoFactory DirectoryInfo => throw new NotImplementedException();

        public IDriveInfoFactory DriveInfo => throw new NotImplementedException();

        public IFile File { get; }

        public IFileInfoFactory FileInfo => throw new NotImplementedException();

        public IFileStreamFactory FileStream => throw new NotImplementedException();

        public IFileSystemWatcherFactory FileSystemWatcher => throw new NotImplementedException();

        public IPath Path { get; }

        private static string GetExtension(byte id) =>
            extensions.TryGetValue(id, out var extension) ? extension : $"x{id:x2}";

        private static string GetGroup(byte id) =>
            groups.TryGetValue(id, out var group) ? group : $"x{id:x2}";

        private static DirEntry[] ReadIndex(Stream source)
        {
            var buffer = new byte[12];
            source.ReadExactly(buffer, 4);

            var dataOffset = BitConverter.ToUInt32(buffer, 0);
            var entries = new DirEntry[dataOffset / 12];
            for (var i = 0; i < entries.Length; i++)
            {
                source.ReadExactly(buffer, 12);

                var name = Encoding.ASCII.GetString(buffer, 0, 8).TrimEnd('\0');
                var offset = BitConverter.ToUInt32(buffer, 8) * SectorSize;

                entries[i] = (name, offset);
            }

            return entries;
        }

        private static FileEntry[] ReadDar(Stream source, string group, long offset, long length)
        {
            var buffer = new byte[8];

            var entries = new List<FileEntry>();
            var relative = 0u;
            while (relative < length - 7)
            {
                source.Seek(offset + relative, SeekOrigin.Begin);
                source.ReadExactly(buffer, 8);

                var id = string.Concat(buffer[..2].Reverse().Select(b => b.ToString("x2")));
                var ext = GetExtension(buffer[2]);
                var size = BitConverter.ToUInt32(buffer, 4);

                var key = $"{group}/{id}.{ext}";

                entries.Add((key, offset + relative, size));

                relative += 8 + size;
            }

            return entries.ToArray();
        }

        private static FileEntry[] ReadList(Stream source, long offset)
        {
            source.Seek(offset, SeekOrigin.Begin);

            var buffer = new byte[8];
            source.ReadExactly(buffer, 4);
            var totalSize = BitConverter.ToUInt16(buffer, 2) * SectorSize;

            var rawEntries = new List<(ushort id, byte group, byte ext, uint size, bool packed)>();
            while (true)
            {
                source.ReadExactly(buffer, 8);
                if (BitConverter.ToUInt32(buffer, 0) == 0)
                {
                    break;
                }

                var id = BitConverter.ToUInt16(buffer, 0);
                var group = buffer[2];
                var ext = buffer[3];
                var size = BitConverter.ToUInt32(buffer, 4);

                if (ext == byte.MaxValue)
                {
                    var notLast = false;
                    for (var i = rawEntries.Count - 1; i >= 0; i--)
                    {
                        var prev = rawEntries[i];
                        if (prev.group != group)
                        {
                            break;
                        }

                        var nextSize = prev.size;
                        rawEntries[i] = prev with { packed = notLast, size = size - prev.size };
                        size = nextSize;
                        notLast = true;
                    }
                }
                else
                {
                    rawEntries.Add((id, group, ext, size, false));
                }
            }

            var entries = new List<FileEntry>();
            var counts = new Dictionary<string, int>();
            var relative = SectorSize;
            foreach (var entry in rawEntries)
            {
                var group = GetGroup(entry.group);

                if (entry.ext == 0x64)
                {
                    entries.AddRange(ReadDar(source, group, offset + relative, entry.size));
                }
                else
                {
                    var id = entry.id.ToString("x4");
                    var ext = GetExtension(entry.ext);

                    var key = $"{group}/{id}.{ext}";
                    counts.TryGetValue(key, out var ix);
                    counts[key] = ix + 1;

                    if (ix > 0)
                    {
                        key = $"{group}/{id}.{ix}.{ext}";
                    }

                    entries.Add((key, offset + relative, entry.size));
                }

                relative += entry.size;
                if (!entry.packed && relative % SectorSize != 0)
                {
                    relative += SectorSize - relative % SectorSize;
                }
            }

            return entries.OrderBy(e => e.name).ToArray();
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.sourceStream?.Dispose();
                }

                this.sourceStream = null!;
                this.disposed = true;
            }
        }

        private FileEntry[] GetFileIndex(string path)
        {
            var ix = Array.FindIndex(this.index, e => e.name == path);
            if (ix < 0)
            {
                throw new DirectoryNotFoundException();
            }

            if (!this.fileEntries.TryGetValue(path, out var files))
            {
                var entry = this.index[ix];
                this.fileEntries[path] = files = ReadList(this.sourceStream, entry.offset);
            }

            return files;
        }

        private (long offset, long size)? GetStreamSpanRange(string path)
        {
            var ix = path.AsSpan().IndexOfAny(separators);
            if (ix >= 0)
            {
                var name = path[(ix + 1)..];
                var dir = path[..ix].TrimEnd(separators);

                var files = this.GetFileIndex(dir);
                ix = Array.FindIndex(files, e => e.name == name);
                if (ix >= 0)
                {
                    var file = files[ix];
                    return (file.offset, file.size);
                }
            }

            return null;
        }

        private Stream GetStreamSpan(string path)
        {
            if (this.GetStreamSpanRange(path) is (long offset, long size))
            {
                return new OffsetStreamSpan(this.sourceStream, offset, size);
            }

            var ex = new FileNotFoundException();
            throw new FileNotFoundException(ex.Message, path);
        }

        private class DirectoryProvider : IDirectory
        {
            private StageDirVirtualFileSystem parent;

            public DirectoryProvider(StageDirVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public IFileSystem FileSystem => throw new NotImplementedException();

            public IDirectoryInfo CreateDirectory(string path) => throw new NotImplementedException();
            public IDirectoryInfo CreateDirectory(string path, UnixFileMode unixCreateMode) => throw new NotImplementedException();
            public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) => throw new NotImplementedException();
            public IDirectoryInfo CreateTempSubdirectory(string? prefix = null) => throw new NotImplementedException();
            public void Delete(string path) => throw new NotImplementedException();
            public void Delete(string path, bool recursive) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateDirectories(string path) => this.EnumerateDirectories(path, "*");
            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => this.EnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
            {
                var glob = PathExtensions.GlobToRegex(searchPattern);
                if (path == string.Empty)
                {
                    if (searchOption == SearchOption.TopDirectoryOnly)
                    {
                        return this.parent.index.Select(i => i.name).Where(f => glob.IsMatch(f));
                    }
                }

                var parts = path.Split(separators, 2, StringSplitOptions.RemoveEmptyEntries);

                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFiles(string path) => this.EnumerateFiles(path, "*");

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => this.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
            {
                var glob = PathExtensions.GlobToRegex(searchPattern);
                if (path == string.Empty)
                {
                    if (searchOption == SearchOption.TopDirectoryOnly)
                    {
                        return Enumerable.Empty<string>();
                    }
                    else
                    {
                        return this.parent.index.SelectMany(i =>
                            this.parent.GetFileIndex(i.name)
                                .Where(f =>
                                    glob.IsMatch(System.IO.Path.GetFileName(f.name)))
                                .Select(f => $"{i.name}/{f.name}"));
                    }
                }
                else
                {
                    var parts = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    var root = parts[0];
                    var dir = string.Concat(parts.Skip(1).Select(p => p + "/"));
                    if (searchOption == SearchOption.TopDirectoryOnly)
                    {
                        return this.parent.GetFileIndex(root)
                            .Where(f =>
                                f.name.StartsWith(dir) &&
                                f.name.IndexOf('/', dir.Length) == -1 &&
                                glob.IsMatch(System.IO.Path.GetFileName(f.name)))
                            .Select(f => $"{root}/{f.name}");
                    }
                    else
                    {
                        return this.parent.GetFileIndex(root)
                            .Where(f =>
                                f.name.StartsWith(dir) &&
                                glob.IsMatch(System.IO.Path.GetFileName(f.name)))
                            .Select(f => $"{root}/{f.name}");
                    }

                }
            }

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFileSystemEntries(string path) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public bool Exists([NotNullWhen(true)] string? path) => throw new NotImplementedException();
            public DateTime GetCreationTime(string path) => throw new NotImplementedException();
            public DateTime GetCreationTimeUtc(string path) => throw new NotImplementedException();
            public string GetCurrentDirectory() => throw new NotImplementedException();
            public string[] GetDirectories(string path) => throw new NotImplementedException();
            public string[] GetDirectories(string path, string searchPattern) => throw new NotImplementedException();
            public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();
            public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public string GetDirectoryRoot(string path) => throw new NotImplementedException();
            public string[] GetFiles(string path) => throw new NotImplementedException();
            public string[] GetFiles(string path, string searchPattern) => throw new NotImplementedException();
            public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();
            public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public string[] GetFileSystemEntries(string path) => throw new NotImplementedException();
            public string[] GetFileSystemEntries(string path, string searchPattern) => throw new NotImplementedException();
            public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();
            public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public DateTime GetLastAccessTime(string path) => throw new NotImplementedException();
            public DateTime GetLastAccessTimeUtc(string path) => throw new NotImplementedException();
            public DateTime GetLastWriteTime(string path) => throw new NotImplementedException();
            public DateTime GetLastWriteTimeUtc(string path) => throw new NotImplementedException();
            public string[] GetLogicalDrives() => throw new NotImplementedException();
            public IDirectoryInfo? GetParent(string path) => throw new NotImplementedException();
            public void Move(string sourceDirName, string destDirName) => throw new NotImplementedException();
            public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) => throw new NotImplementedException();
            public void SetCreationTime(string path, DateTime creationTime) => throw new NotImplementedException();
            public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => throw new NotImplementedException();
            public void SetCurrentDirectory(string path) => throw new NotImplementedException();
            public void SetLastAccessTime(string path, DateTime lastAccessTime) => throw new NotImplementedException();
            public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => throw new NotImplementedException();
            public void SetLastWriteTime(string path, DateTime lastWriteTime) => throw new NotImplementedException();
            public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => throw new NotImplementedException();
        }

        private class FileProvider : IFile
        {
            private StageDirVirtualFileSystem parent;

            public FileProvider(StageDirVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public IFileSystem FileSystem => throw new NotImplementedException();

            public void AppendAllLines(string path, IEnumerable<string> contents) => throw new NotImplementedException();
            public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding) => throw new NotImplementedException();
            public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void AppendAllText(string path, string? contents) => throw new NotImplementedException();
            public void AppendAllText(string path, string? contents, Encoding encoding) => throw new NotImplementedException();
            public Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AppendAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public StreamWriter AppendText(string path) => throw new NotImplementedException();
            public void Copy(string sourceFileName, string destFileName) => throw new NotImplementedException();
            public void Copy(string sourceFileName, string destFileName, bool overwrite) => throw new NotImplementedException();
            public FileSystemStream Create(string path) => throw new NotImplementedException();
            public FileSystemStream Create(string path, int bufferSize) => throw new NotImplementedException();
            public FileSystemStream Create(string path, int bufferSize, FileOptions options) => throw new NotImplementedException();
            public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) => throw new NotImplementedException();
            public StreamWriter CreateText(string path) => throw new NotImplementedException();
            public void Decrypt(string path) => throw new NotImplementedException();
            public void Delete(string path) => throw new NotImplementedException();
            public void Encrypt(string path) => throw new NotImplementedException();
            public bool Exists([NotNullWhen(true)] string? path) => this.parent.GetStreamSpanRange(path) is not null;
            public FileAttributes GetAttributes(string path) => throw new NotImplementedException();
            public FileAttributes GetAttributes(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public DateTime GetCreationTime(string path) => throw new NotImplementedException();
            public DateTime GetCreationTime(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public DateTime GetCreationTimeUtc(string path) => throw new NotImplementedException();
            public DateTime GetCreationTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public DateTime GetLastAccessTime(string path) => throw new NotImplementedException();
            public DateTime GetLastAccessTime(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public DateTime GetLastAccessTimeUtc(string path) => throw new NotImplementedException();
            public DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public DateTime GetLastWriteTime(string path) => throw new NotImplementedException();
            public DateTime GetLastWriteTime(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public DateTime GetLastWriteTimeUtc(string path) => throw new NotImplementedException();
            public DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public UnixFileMode GetUnixFileMode(string path) => throw new NotImplementedException();
            public UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle) => throw new NotImplementedException();
            public void Move(string sourceFileName, string destFileName) => throw new NotImplementedException();
            public void Move(string sourceFileName, string destFileName, bool overwrite) => throw new NotImplementedException();
            public FileSystemStream Open(string path, FileMode mode) => throw new NotImplementedException();
            public FileSystemStream Open(string path, FileMode mode, FileAccess access) => throw new NotImplementedException();
            public FileSystemStream Open(string path, FileMode mode, FileAccess access, FileShare share) => throw new NotImplementedException();
            public FileSystemStream Open(string path, FileStreamOptions options) => throw new NotImplementedException();
            public FileSystemStream OpenRead(string path) => new StreamWrapper(this.parent.GetStreamSpan(path), path, isAsync: false);
            public StreamReader OpenText(string path) => throw new NotImplementedException();
            public FileSystemStream OpenWrite(string path) => throw new NotImplementedException();
            public byte[] ReadAllBytes(string path) => throw new NotImplementedException();
            public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public string[] ReadAllLines(string path) => throw new NotImplementedException();
            public string[] ReadAllLines(string path, Encoding encoding) => throw new NotImplementedException();
            public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public string ReadAllText(string path) => throw new NotImplementedException();
            public string ReadAllText(string path, Encoding encoding) => throw new NotImplementedException();
            public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public IEnumerable<string> ReadLines(string path) => throw new NotImplementedException();
            public IEnumerable<string> ReadLines(string path, Encoding encoding) => throw new NotImplementedException();
            public IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public IAsyncEnumerable<string> ReadLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName) => throw new NotImplementedException();
            public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors) => throw new NotImplementedException();
            public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) => throw new NotImplementedException();
            public void SetAttributes(string path, FileAttributes fileAttributes) => throw new NotImplementedException();
            public void SetAttributes(SafeFileHandle fileHandle, FileAttributes fileAttributes) => throw new NotImplementedException();
            public void SetCreationTime(string path, DateTime creationTime) => throw new NotImplementedException();
            public void SetCreationTime(SafeFileHandle fileHandle, DateTime creationTime) => throw new NotImplementedException();
            public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => throw new NotImplementedException();
            public void SetCreationTimeUtc(SafeFileHandle fileHandle, DateTime creationTimeUtc) => throw new NotImplementedException();
            public void SetLastAccessTime(string path, DateTime lastAccessTime) => throw new NotImplementedException();
            public void SetLastAccessTime(SafeFileHandle fileHandle, DateTime lastAccessTime) => throw new NotImplementedException();
            public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => throw new NotImplementedException();
            public void SetLastAccessTimeUtc(SafeFileHandle fileHandle, DateTime lastAccessTimeUtc) => throw new NotImplementedException();
            public void SetLastWriteTime(string path, DateTime lastWriteTime) => throw new NotImplementedException();
            public void SetLastWriteTime(SafeFileHandle fileHandle, DateTime lastWriteTime) => throw new NotImplementedException();
            public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => throw new NotImplementedException();
            public void SetLastWriteTimeUtc(SafeFileHandle fileHandle, DateTime lastWriteTimeUtc) => throw new NotImplementedException();
            public void SetUnixFileMode(string path, UnixFileMode mode) => throw new NotImplementedException();
            public void SetUnixFileMode(SafeFileHandle fileHandle, UnixFileMode mode) => throw new NotImplementedException();
            public void WriteAllBytes(string path, byte[] bytes) => throw new NotImplementedException();
            public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void WriteAllLines(string path, string[] contents) => throw new NotImplementedException();
            public void WriteAllLines(string path, IEnumerable<string> contents) => throw new NotImplementedException();
            public void WriteAllLines(string path, string[] contents, Encoding encoding) => throw new NotImplementedException();
            public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding) => throw new NotImplementedException();
            public Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void WriteAllText(string path, string? contents) => throw new NotImplementedException();
            public void WriteAllText(string path, string? contents, Encoding encoding) => throw new NotImplementedException();
            public Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class PathProvider : IPath
        {
            private StageDirVirtualFileSystem parent;

            public PathProvider(StageDirVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public char AltDirectorySeparatorChar => throw new NotImplementedException();

            public char DirectorySeparatorChar => throw new NotImplementedException();

            public char PathSeparator => throw new NotImplementedException();

            public char VolumeSeparatorChar => throw new NotImplementedException();

            public IFileSystem FileSystem => throw new NotImplementedException();

            [return: NotNullIfNotNull("path")]
            public string? ChangeExtension(string? path, string? extension) => throw new NotImplementedException();
            public string Combine(string path1, string path2) => throw new NotImplementedException();
            public string Combine(string path1, string path2, string path3) => throw new NotImplementedException();
            public string Combine(string path1, string path2, string path3, string path4) => throw new NotImplementedException();
            public string Combine(params string[] paths) => throw new NotImplementedException();
            public bool EndsInDirectorySeparator(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public bool EndsInDirectorySeparator(string path) => throw new NotImplementedException();
            public bool Exists([NotNullWhen(true)] string? path) => throw new NotImplementedException();
            public ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public string? GetDirectoryName(string? path) => throw new NotImplementedException();
            public ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();
            [return: NotNullIfNotNull("path")]
            public string? GetExtension(string? path) => throw new NotImplementedException();
            public ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path) => throw new NotImplementedException();
            [return: NotNullIfNotNull("path")]
            public string? GetFileName(string? path) => throw new NotImplementedException();
            public ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();
            [return: NotNullIfNotNull("path")]
            public string? GetFileNameWithoutExtension(string? path) => throw new NotImplementedException();
            public string GetFullPath(string path) => throw new NotImplementedException();
            public string GetFullPath(string path, string basePath) => throw new NotImplementedException();
            public char[] GetInvalidFileNameChars() => throw new NotImplementedException();
            public char[] GetInvalidPathChars() => throw new NotImplementedException();
            public ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public string? GetPathRoot(string? path) => throw new NotImplementedException();
            public string GetRandomFileName() => throw new NotImplementedException();
            public string GetRelativePath(string relativeTo, string path) => throw new NotImplementedException();
            public string GetTempFileName() => throw new NotImplementedException();
            public string GetTempPath() => throw new NotImplementedException();
            public bool HasExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public bool HasExtension([NotNullWhen(true)] string? path) => throw new NotImplementedException();
            public bool IsPathFullyQualified(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public bool IsPathFullyQualified(string path) => throw new NotImplementedException();
            public bool IsPathRooted(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public bool IsPathRooted([NotNullWhen(true)] string? path) => throw new NotImplementedException();
            public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2) => throw new NotImplementedException();
            public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3) => throw new NotImplementedException();
            public string Join(string? path1, string? path2) => throw new NotImplementedException();
            public string Join(string? path1, string? path2, string? path3) => throw new NotImplementedException();
            public string Join(params string?[] paths) => throw new NotImplementedException();
            public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4) => throw new NotImplementedException();
            public string Join(string? path1, string? path2, string? path3, string? path4) => throw new NotImplementedException();
            public ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) => throw new NotImplementedException();
            public string TrimEndingDirectorySeparator(string path) => throw new NotImplementedException();
            public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten) => throw new NotImplementedException();
            public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten) => throw new NotImplementedException();
        }
    }
}
