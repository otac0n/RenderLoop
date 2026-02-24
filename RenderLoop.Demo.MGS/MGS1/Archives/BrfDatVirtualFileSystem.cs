namespace RenderLoop.Demo.MGS.MGS1.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32.SafeHandles;
    using static PathExtensions;
    using Entry = (string FolderName, string FileName, long Offset, long Length);

    internal class BrfDatVirtualFileSystem : IFileSystem, IDisposable
    {
        private readonly Entry[] index;
        private Stream sourceStream;
        private bool disposed;

        public BrfDatVirtualFileSystem(Stream sourceStream)
        {
            this.index = [.. ReadIndex(sourceStream)];
            this.sourceStream = sourceStream;

            this.Directory = new DirectoryProvider(this);
            this.File = new FileProvider(this);
            this.Path = new PathProvider(this);
        }

        private static void Align(Stream stream, long alignment)
        {
            var offset = (alignment - (stream.Position % alignment)) % alignment;
            if (offset > 0)
            {
                stream.Seek(offset, SeekOrigin.Current);
            }
        }

        private static bool TryAlign(Stream stream, long alignment)
        {
            var offset = (alignment - (stream.Position % alignment)) % alignment;
            if (offset + stream.Position > stream.Length)
            {
                return false;
            }

            if (offset > 0)
            {
                stream.Seek(offset, SeekOrigin.Current);
            }

            return true;
        }

        private static List<Entry> ReadIndex(Stream stream)
        {
            var result = new List<Entry>();
            using var reader = new BinaryReader(stream);
            while (true)
            {
                var fileCount = reader.ReadUInt32();
                if (fileCount == 0 || IsPcx(fileCount))
                {
                    break;
                }

                foreach (var entry in IndexFolder(fileCount, reader))
                {
                    result.Add(entry);
                }

                Align(stream, 0x800);
            }

            stream.Seek(-4, SeekOrigin.Current);
            foreach (var entry in IndexPcx(reader))
            {
                result.Add(entry);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return result;
        }

        private static bool IsPcx(uint signature) => (signature & 0xFFFFFF) == 0x01050a;

        private static IEnumerable<Entry> IndexPcx(BinaryReader reader)
        {
            var stream = reader.BaseStream;
            var end = false;
            while (!end)
            {
                var pcxId = (uint)stream.Position;
                if (stream.Position < stream.Length && IsPcx(reader.ReadUInt32()))
                {
                    stream.Seek(-4, SeekOrigin.Current);
                    SeekPastPCX(reader);
                    yield return new("pcx", pcxId.ToString("x8", CultureInfo.InvariantCulture) + ".pcx", pcxId, stream.Position - pcxId);
                    if (!TryAlign(stream, 0x800))
                    {
                        end = true;
                    }
                }
                else
                {
                    end = true;
                }
            }
        }

        public static void SeekPastPCX(BinaryReader br)
        {
            var s = br.BaseStream;

            var start = s.Position;

            var manufacturer = br.ReadByte(); // must be 0x0A
            var version = br.ReadByte();
            var encoding = br.ReadByte(); // must be 1 (RLE)
            var bitsPerPixel = br.ReadByte();

            if (manufacturer != 0x0A)
            {
                throw new InvalidDataException("Not a PCX file.");
            }

            if (encoding != 1)
            {
                throw new InvalidDataException("Unsupported PCX encoding.");
            }

            var xmin = br.ReadUInt16();
            var ymin = br.ReadUInt16();
            var xmax = br.ReadUInt16();
            var ymax = br.ReadUInt16();
            var width = xmax - xmin + 1;
            var height = ymax - ymin + 1;

            s.Seek(start + 0x041, SeekOrigin.Begin);
            var colorPlanes = br.ReadByte();

            s.Seek(start + 0x042, SeekOrigin.Begin);
            var bytesPerLine = br.ReadUInt16();

            var decodedBytesRequired =
                (long)height * colorPlanes * bytesPerLine;

            var bitmapPos = start + 0x80;
            s.Seek(bitmapPos, SeekOrigin.Begin);

            long decoded = 0;
            while (decoded < decodedBytesRequired)
            {
                var b = s.ReadByte();
                if (b < 0)
                {
                    throw new EndOfStreamException();
                }

                if ((b & 0xC0) == 0xC0)
                {
                    var runLength = b & 0x3F;
                    if (s.ReadByte() < 0)
                    {
                        throw new EndOfStreamException();
                    }

                    decoded += runLength;
                }
                else
                {
                    decoded += 1;
                }
            }

            if (bitsPerPixel == 8 && colorPlanes == 1)
            {
                var marker = s.ReadByte();

                if (marker == 0x0C)
                {
                    s.Seek(0x300, SeekOrigin.Current);
                }
                else
                {
                    s.Seek(-1, SeekOrigin.Current);
                }
            }
        }

        private static IEnumerable<Entry> IndexFolder(uint fileCount, BinaryReader reader)
        {
            var stream = reader.BaseStream;
            var folderId = (uint)stream.Position;
            for (var i = 0; i < fileCount; i++)
            {
                var fileName = ReadString(stream);
                Align(stream, 0x004);
                var fileSize = reader.ReadUInt32();
                yield return new(folderId.ToString("x8", CultureInfo.InvariantCulture), fileName, stream.Position, fileSize);
                stream.Seek(fileSize + 1, SeekOrigin.Current);
            }
        }

        private static string ReadString(Stream stream)
        {
            var value = new StringBuilder();
            int c;
            while ((c = stream.ReadByte()) != -1)
            {
                if (c == 0)
                {
                    break;
                }

                value.Append((char)c);
            }

            return value.ToString();
        }

        public IDirectory Directory { get; }

        public IDirectoryInfoFactory DirectoryInfo => throw new NotImplementedException();

        public IDriveInfoFactory DriveInfo => throw new NotImplementedException();

        public IFile File { get; }

        public IFileInfoFactory FileInfo => throw new NotImplementedException();

        public IFileStreamFactory FileStream => throw new NotImplementedException();

        public IFileSystemWatcherFactory FileSystemWatcher => throw new NotImplementedException();

        public IPath Path { get; }

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

        private (long Offset, long Length)? GetStreamSpanRange(string path)
        {
            var parts = path.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var entry = this.index.FirstOrDefault(e => e.FolderName == parts[0] && e.FileName == parts[1]);
                if (entry != default)
                {
                    return (entry.Offset, entry.Length);
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

            throw new FileNotFoundException(new FileNotFoundException().Message, path);
        }

        private class DirectoryProvider : IDirectory
        {
            private BrfDatVirtualFileSystem parent;

            public DirectoryProvider(BrfDatVirtualFileSystem parent)
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
                var parts = path.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    throw new DirectoryNotFoundException(path);
                }

                if (parts.Length == 0)
                {
                    return this.parent.index.Select(e => e.FolderName).Distinct();
                }
                else
                {
                    if (!this.parent.index.Any(e => e.FolderName == path))
                    {
                        throw new DirectoryNotFoundException(path);
                    }

                    return [];
                }
            }

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public IEnumerable<string> EnumerateFiles(string path) => this.EnumerateFiles(path, "*");

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => this.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
            {
                var parts = path.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    throw new DirectoryNotFoundException(path);
                }

                if (parts.Length == 0)
                {
                    return [];
                }
                else
                {
                    var files = this.parent.index.Where(e => e.FolderName == path);
                    if (!files.Any())
                    {
                        throw new DirectoryNotFoundException(path);
                    }

                    return files.Select(e => e.FolderName + "/" + e.FileName);
                }
            }

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public IEnumerable<string> EnumerateFileSystemEntries(string path) => this.EnumerateFileSystemEntries(path, "*");

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) => this.EnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => this.EnumerateDirectories(path, searchPattern, searchOption).Concat(this.EnumerateFiles(path, searchPattern, searchOption));

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.EnumerateDirectories(path, searchPattern, enumerationOptions).Concat(this.EnumerateFiles(path, searchPattern, enumerationOptions));

            public bool Exists([NotNullWhen(true)] string? path) => throw new NotImplementedException();

            public DateTime GetCreationTime(string path) => throw new NotImplementedException();

            public DateTime GetCreationTimeUtc(string path) => throw new NotImplementedException();

            public string GetCurrentDirectory() => throw new NotImplementedException();

            public string[] GetDirectories(string path) => [.. this.EnumerateDirectories(path)];

            public string[] GetDirectories(string path, string searchPattern) => [.. this.EnumerateDirectories(path, searchPattern)];

            public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => [.. this.EnumerateDirectories(path, searchPattern, searchOption)];

            public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => [.. this.EnumerateDirectories(path, searchPattern, enumerationOptions)];

            public string GetDirectoryRoot(string path) => throw new NotImplementedException();

            public string[] GetFiles(string path) => [.. this.EnumerateFiles(path)];

            public string[] GetFiles(string path, string searchPattern) => [.. this.EnumerateFiles(path, searchPattern)];

            public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [.. this.EnumerateFiles(path, searchPattern, searchOption)];

            public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => [.. this.EnumerateFiles(path, searchPattern, enumerationOptions)];

            public string[] GetFileSystemEntries(string path) => [.. this.EnumerateFileSystemEntries(path)];

            public string[] GetFileSystemEntries(string path, string searchPattern) => [.. this.EnumerateFileSystemEntries(path, searchPattern)];

            public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => [.. this.EnumerateFileSystemEntries(path, searchPattern, searchOption)];

            public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => [.. this.EnumerateFileSystemEntries(path, searchPattern, enumerationOptions)];

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
            private BrfDatVirtualFileSystem parent;

            public FileProvider(BrfDatVirtualFileSystem parent)
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
            private BrfDatVirtualFileSystem parent;

            public PathProvider(BrfDatVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public char AltDirectorySeparatorChar => throw new NotImplementedException();

            public char DirectorySeparatorChar => '/';

            public char PathSeparator => throw new NotImplementedException();

            public char VolumeSeparatorChar => throw new NotImplementedException();

            public IFileSystem FileSystem => throw new NotImplementedException();

            [return: NotNullIfNotNull("path")]
            public string? ChangeExtension(string? path, string? extension) => throw new NotImplementedException();

            public string Combine(string path1, string path2) => this.CombineWithSeparator(this.DirectorySeparatorChar, path1, path2);

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

            public string GetRelativePath(string relativeTo, string path) => this.GetRelativePath(this.DirectorySeparatorChar, relativeTo, path);

            public string GetTempFileName() => throw new NotImplementedException();

            public string GetTempPath() => throw new NotImplementedException();

            public bool HasExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public bool HasExtension([NotNullWhen(true)] string? path) => throw new NotImplementedException();

            public bool IsPathFullyQualified(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public bool IsPathFullyQualified(string path) => throw new NotImplementedException();

            public bool IsPathRooted(ReadOnlySpan<char> path) => System.IO.Path.IsPathRooted(path);

            public bool IsPathRooted([NotNullWhen(true)] string? path) => System.IO.Path.IsPathRooted(path);

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
