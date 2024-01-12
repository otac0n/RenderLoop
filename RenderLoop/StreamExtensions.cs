namespace RenderLoop
{
    using System;
    using System.IO;

    internal static class StreamExtensions
    {
        public static void ReadExactly(this Stream source, byte[] buffer, int count) => source.ReadExactly(buffer, 0, count);

        public static void CopyTo(this Stream source, Stream destination, long offset, SeekOrigin origin, long count)
        {
            source.Seek(offset, origin);
            int read;
            var buffer = new byte[81920];
            while (count > 0 && (read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, count))) > 0)
            {
                destination.Write(buffer, 0, read);
                count -= read;
            }
        }

        public static bool Contains(this Stream source, byte[] pattern)
        {
            using var memory = new MemoryStream();
            source.CopyTo(memory);
            var subject = memory.ToArray();
            var l = subject.LongLength;
            for (var i = 0L; i < l; i++)
            {
                var found = true;
                for (var j = 0; found && j < pattern.Length && (i + j) < l; j++)
                {
                    if (subject[i + j] != pattern[j])
                    {
                        found = false;
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
