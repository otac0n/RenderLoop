// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1.Archives
{
    using System;
    using System.IO;

    public class OffsetStreamSpan : Stream
    {
        private readonly Stream underlying;
        private readonly long offset;
        private long position;

        public OffsetStreamSpan(Stream underlying, long offset, long length)
        {
            this.underlying = underlying;
            this.offset = offset;
            this.Length = length;
        }

        public override bool CanRead => this.underlying.CanRead;

        public override bool CanSeek => this.underlying.CanSeek;

        public override bool CanWrite => this.underlying.CanWrite;

        public override long Length { get; }

        public override long Position
        {
            get => this.position;
            set
            {
                if (value < 0 || value > this.Length)
                {
                    throw new NotSupportedException();
                }

                this.underlying.Position = value + this.offset;
                this.position = value;
            }
        }

        public override void Flush() => this.underlying.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, this.Length - this.position);
            this.underlying.Seek(this.position + this.offset, SeekOrigin.Begin);
            var read = this.underlying.Read(buffer, offset, count);
            this.position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.End:
                    offset = -this.position + this.Length - offset;
                    goto case SeekOrigin.Current;

                case SeekOrigin.Begin:
                    offset -= this.position;
                    goto case SeekOrigin.Current;

                case SeekOrigin.Current:
                    var newPosition = this.position + offset;
                    if (newPosition < 0 || newPosition > this.Length)
                    {
                        throw new NotSupportedException();
                    }

                    return this.position = newPosition;

                default:
                    throw new NotSupportedException();
            }
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, this.Length - this.position);
            this.underlying.Seek(this.position + this.offset, SeekOrigin.Begin);
            this.underlying.Write(buffer, offset, count);
        }
    }
}
