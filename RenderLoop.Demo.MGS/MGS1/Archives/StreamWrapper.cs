// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1.Archives
{
    using System.IO;
    using System.IO.Abstractions;

    /// <summary>
    /// This should have been provided by System.IO.Abstractions (3rd-Party Package).
    /// </summary>
    /// <param name="stream">The stream with a filename.</param>
    /// <param name="path">The path from which the file can (at one point) be acquired.</param>
    /// <param name="isAsync">A value indicating whether or not the underlying stream was opened in an async mode.</param>
    internal class StreamWrapper(Stream stream, string path, bool isAsync)
        : FileSystemStream(stream, path, isAsync)
    {
    }
}
