namespace RenderLoop.Demo.MGS.MGS1.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DiscUtils.Iso9660;
    using Microsoft.Win32.SafeHandles;

    internal class CDReaderVFSAdapter : IFileSystem
    {
        private readonly CDReader cdReader;

        public CDReaderVFSAdapter(CDReader cdReader)
        {
            this.cdReader = cdReader;
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

        private static string[] FilterNames(string[] names)
        {
            return Array.ConvertAll(names, name =>
            {
                var lastSemicolon = name.LastIndexOf(";", StringComparison.OrdinalIgnoreCase);
                if (lastSemicolon >= 0)
                {
                    return name[..lastSemicolon];
                }
                else
                {
                    return name;
                }
            });
        }

        private class DirectoryProvider : IDirectory
        {
            private CDReaderVFSAdapter parent;

            public DirectoryProvider(CDReaderVFSAdapter parent)
            {
                this.parent = parent;
            }

            public IFileSystem FileSystem => this.parent;

            public IDirectoryInfo CreateDirectory(string path) => throw new NotImplementedException();

            public IDirectoryInfo CreateDirectory(string path, UnixFileMode unixCreateMode) => throw new NotImplementedException();

            public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) => throw new NotImplementedException();

            public IDirectoryInfo CreateTempSubdirectory(string? prefix = null) => throw new NotImplementedException();

            public void Delete(string path) => this.parent.cdReader.DeleteDirectory(path);

            public void Delete(string path, bool recursive) => this.parent.cdReader.DeleteDirectory(path, recursive);

            public IEnumerable<string> EnumerateDirectories(string path) => this.GetDirectories(path);

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => this.GetDirectories(path, searchPattern);

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) => this.GetDirectories(path, searchPattern, searchOption);

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.GetDirectories(path, searchPattern, enumerationOptions);

            public IEnumerable<string> EnumerateFiles(string path) => this.GetFiles(path);

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => this.GetFiles(path, searchPattern);

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => this.GetFiles(path, searchPattern, searchOption);

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.GetFiles(path, searchPattern, enumerationOptions);

            public IEnumerable<string> EnumerateFileSystemEntries(string path) => this.GetFileSystemEntries(path);

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) => this.GetFileSystemEntries(path, searchPattern);

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => this.GetFileSystemEntries(path, searchPattern, searchOption);

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.GetFileSystemEntries(path, searchPattern, enumerationOptions);

            public bool Exists([NotNullWhen(true)] string? path) => this.parent.cdReader.Exists(path);

            public DateTime GetCreationTime(string path) => this.parent.cdReader.GetCreationTime(path);

            public DateTime GetCreationTimeUtc(string path) => this.parent.cdReader.GetCreationTimeUtc(path);

            public string GetCurrentDirectory() => throw new NotImplementedException();

            public string[] GetDirectories(string path) => this.parent.cdReader.GetDirectories(path);

            public string[] GetDirectories(string path, string searchPattern) => this.parent.cdReader.GetDirectories(path, searchPattern);

            public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => this.parent.cdReader.GetDirectories(path, searchPattern, searchOption);

            public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public string GetDirectoryRoot(string path) => throw new NotImplementedException();

            public string[] GetFiles(string path) => FilterNames(this.parent.cdReader.GetFiles(path));

            public string[] GetFiles(string path, string searchPattern) => FilterNames(this.parent.cdReader.GetFiles(path, searchPattern));

            public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => FilterNames(this.parent.cdReader.GetFiles(path, searchPattern, searchOption));

            public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public string[] GetFileSystemEntries(string path) => FilterNames(this.parent.cdReader.GetFileSystemEntries(path));

            public string[] GetFileSystemEntries(string path, string searchPattern) => FilterNames(this.parent.cdReader.GetFileSystemEntries(path, searchPattern));

            public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => searchOption == SearchOption.TopDirectoryOnly ? FilterNames(this.parent.cdReader.GetFileSystemEntries(path, searchPattern)) : throw new NotSupportedException();

            public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public DateTime GetLastAccessTime(string path) => this.parent.cdReader.GetLastAccessTime(path);

            public DateTime GetLastAccessTimeUtc(string path) => this.parent.cdReader.GetLastAccessTimeUtc(path);

            public DateTime GetLastWriteTime(string path) => this.parent.cdReader.GetLastWriteTime(path);

            public DateTime GetLastWriteTimeUtc(string path) => this.parent.cdReader.GetLastWriteTimeUtc(path);

            public string[] GetLogicalDrives() => throw new NotImplementedException();

            public IDirectoryInfo? GetParent(string path) => throw new NotImplementedException();

            public void Move(string sourceDirName, string destDirName) => throw new NotImplementedException();

            public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) => throw new NotImplementedException();

            public void SetCreationTime(string path, DateTime creationTime) => this.parent.cdReader.SetCreationTime(path, creationTime);

            public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => this.parent.cdReader.SetCreationTimeUtc(path, creationTimeUtc);

            public void SetCurrentDirectory(string path) => throw new NotImplementedException();

            public void SetLastAccessTime(string path, DateTime lastAccessTime) => this.parent.cdReader.SetLastAccessTime(path, lastAccessTime);

            public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => this.parent.cdReader.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

            public void SetLastWriteTime(string path, DateTime lastWriteTime) => this.parent.cdReader.SetLastWriteTime(path, lastWriteTime);

            public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => this.parent.cdReader.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
        }

        private class FileProvider : IFile
        {
            private CDReaderVFSAdapter parent;

            public FileProvider(CDReaderVFSAdapter parent)
            {
                this.parent = parent;
            }

            public IFileSystem FileSystem => this.parent;

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

            public bool Exists([NotNullWhen(true)] string? path) => this.parent.cdReader.Exists(path);

            public FileAttributes GetAttributes(string path) => this.parent.cdReader.GetAttributes(path);

            public FileAttributes GetAttributes(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetCreationTime(string path) => this.parent.cdReader.GetCreationTime(path);

            public DateTime GetCreationTime(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetCreationTimeUtc(string path) => this.parent.cdReader.GetCreationTimeUtc(path);

            public DateTime GetCreationTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastAccessTime(string path) => this.parent.cdReader.GetLastAccessTime(path);

            public DateTime GetLastAccessTime(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastAccessTimeUtc(string path) => this.parent.cdReader.GetLastAccessTimeUtc(path);

            public DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastWriteTime(string path) => this.parent.cdReader.GetLastWriteTime(path);

            public DateTime GetLastWriteTime(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastWriteTimeUtc(string path) => this.parent.cdReader.GetLastWriteTimeUtc(path);

            public DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public UnixFileMode GetUnixFileMode(string path) => throw new NotImplementedException();

            public UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public void Move(string sourceFileName, string destFileName) => throw new NotImplementedException();

            public void Move(string sourceFileName, string destFileName, bool overwrite) => throw new NotImplementedException();

            public FileSystemStream Open(string path, FileMode mode) => new StreamWrapper(this.parent.cdReader.OpenFile(path, mode), path, isAsync: false);

            public FileSystemStream Open(string path, FileMode mode, FileAccess access) => new StreamWrapper(this.parent.cdReader.OpenFile(path, mode, access), path, isAsync: false);

            public FileSystemStream Open(string path, FileMode mode, FileAccess access, FileShare share) => throw new NotImplementedException();

            public FileSystemStream Open(string path, FileStreamOptions options) => throw new NotImplementedException();

            public FileSystemStream OpenRead(string path) => new StreamWrapper(this.parent.cdReader.OpenFile(path, FileMode.Open), path, isAsync: false);

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

            public void SetAttributes(string path, FileAttributes fileAttributes) => this.parent.cdReader.SetAttributes(path, fileAttributes);

            public void SetAttributes(SafeFileHandle fileHandle, FileAttributes fileAttributes) => throw new NotImplementedException();

            public void SetCreationTime(string path, DateTime creationTime) => this.parent.cdReader.SetCreationTime(path, creationTime);

            public void SetCreationTime(SafeFileHandle fileHandle, DateTime creationTime) => throw new NotImplementedException();

            public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => this.parent.cdReader.SetCreationTimeUtc(path, creationTimeUtc);

            public void SetCreationTimeUtc(SafeFileHandle fileHandle, DateTime creationTimeUtc) => throw new NotImplementedException();

            public void SetLastAccessTime(string path, DateTime lastAccessTime) => this.parent.cdReader.SetLastAccessTime(path, lastAccessTime);

            public void SetLastAccessTime(SafeFileHandle fileHandle, DateTime lastAccessTime) => throw new NotImplementedException();

            public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => this.parent.cdReader.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

            public void SetLastAccessTimeUtc(SafeFileHandle fileHandle, DateTime lastAccessTimeUtc) => throw new NotImplementedException();

            public void SetLastWriteTime(string path, DateTime lastWriteTime) => this.parent.cdReader.SetLastWriteTime(path, lastWriteTime);

            public void SetLastWriteTime(SafeFileHandle fileHandle, DateTime lastWriteTime) => throw new NotImplementedException();

            public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => this.parent.cdReader.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

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
            private CDReaderVFSAdapter parent;

            public PathProvider(CDReaderVFSAdapter parent)
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
