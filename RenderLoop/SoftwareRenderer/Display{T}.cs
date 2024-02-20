namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Drawing;

    public abstract class Display<TState> : Display
    {
        public Display(TState initialState)
        {
            this.State = initialState;
        }

        public TState State { get; private set; }

        protected abstract TState AdvanceFrame(TState state, TimeSpan elapsed);

        protected sealed override void AdvanceFrame(TimeSpan elapsed) =>
            this.State = this.AdvanceFrame(this.State, elapsed);

        protected abstract void DrawScene(TState state, Graphics g, Bitmap buffer, float[,] depthBuffer);

        protected sealed override void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            this.DrawScene(this.State, g, buffer, depthBuffer);
    }
}
