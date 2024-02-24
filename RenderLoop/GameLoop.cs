namespace RenderLoop
{
    using System;
    using RenderLoop.SoftwareRenderer;
    using System.Diagnostics;
    using System.Windows.Forms;
    using System.Threading;

    public abstract class GameLoop : IDisposable
    {
        private readonly Action<CancellationToken> run;

        private long timestamp;

        public GameLoop(Display display)
        {
            void FrameTimer_FirstTick(object? sender, EventArgs e)
            {
                this.Initialize();
                var now = Stopwatch.GetTimestamp();
                this.timestamp = now;
                display.FrameTimer.Tick -= FrameTimer_FirstTick;
                display.FrameTimer.Tick += FrameTimer_Tick;
                this.Tick(TimeSpan.Zero);
            }

            void FrameTimer_Tick(object? sender, EventArgs e)
            {
                var now = Stopwatch.GetTimestamp();
                var elapsed = Stopwatch.GetElapsedTime(this.timestamp, now);
                this.timestamp = now;
                this.Tick(elapsed);
            }

            display.FrameTimer.Tick += FrameTimer_FirstTick;

            this.run = (CancellationToken cancel) =>
            {
                var context = new ApplicationContext(display);
                cancel.Register(context.ExitThread);
                Application.Run(context);
            };
        }

        protected virtual void Initialize()
        {
        }

        protected abstract void AdvanceFrame(TimeSpan elapsed);

        protected abstract void DrawScene(TimeSpan elapsed);

        private void Tick(TimeSpan elapsed)
        {
            this.AdvanceFrame(elapsed);
            this.DrawScene(elapsed);
        }

        public void Run(CancellationToken cancel)
        {
            this.run(cancel);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        void IDisposable.Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
