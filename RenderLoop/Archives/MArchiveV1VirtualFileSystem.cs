namespace RenderLoop.Archives
{
    using GMWare.M2.Psb;
    using GMWare.M2.Models;
    using GMWare.M2.MArchive;
    using System.IO;
    using System;
    using System.IO.Abstractions;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

    public sealed class MArchiveV1VirtualFileSystem : IFileSystem, IDisposable
    {
        private bool disposed;
        private readonly ArchiveV1 index;
        private Stream sourceStream;

        public MArchiveV1VirtualFileSystem(string binPath, string key, IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            this.index = ReadIndex(fileSystem.Path.ChangeExtension(binPath, ".psb.m"), key);
            this.sourceStream = fileSystem.File.OpenRead(binPath);

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

        private static ArchiveV1 ReadIndex(string indexPath, string key)
        {
            var packer = new MArchivePacker(new ZlibCodec(), key, 64);
            using (var stream = new MemoryStream())
            {
                packer.DecompressFile(indexPath, keepOrig: true, outputStream: stream);

                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new PsbReader(stream, filter: null))
                {
                    var root = reader.Root;
                    return root.ToObject<ArchiveV1>();
                }
            }
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

        private Stream GetStreamSpan(string path)
        {
            if (this.index.FileInfo.TryGetValue(path, out var span))
            {
                return new OffsetStreamSpan(this.sourceStream, span[0], span[1]);
            }

            var ex = new FileNotFoundException();
            throw new FileNotFoundException(ex.Message, path);
        }

        private class DirectoryProvider : IDirectory
        {
            private MArchiveV1VirtualFileSystem parent;

            public DirectoryProvider(MArchiveV1VirtualFileSystem parent)
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
            public IEnumerable<string> EnumerateDirectories(string path) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFiles(string path) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => throw new NotImplementedException();
            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();
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
            private MArchiveV1VirtualFileSystem parent;

            public FileProvider(MArchiveV1VirtualFileSystem parent)
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
            public bool Exists([NotNullWhen(true)] string? path) => throw new NotImplementedException();
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
            private MArchiveV1VirtualFileSystem parent;

            public PathProvider(MArchiveV1VirtualFileSystem parent)
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
