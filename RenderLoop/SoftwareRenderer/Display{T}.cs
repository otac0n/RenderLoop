namespace RenderLoop.SoftwareRenderer
{
    using System;
    using System.Drawing;

    public abstract class Display<TState> : Display
    {
        private TState state;

        public Display(TState initialState)
        {
            this.state = initialState;
        }

        public TState State => this.state;

        protected abstract void AdvanceFrame(ref TState state, TimeSpan elapsed);

        protected sealed override void AdvanceFrame(TimeSpan elapsed) =>
            this.AdvanceFrame(ref this.state, elapsed);

        protected abstract void DrawScene(TState state, Graphics g, Bitmap buffer, float[,] depthBuffer);

        protected sealed override void DrawScene(Graphics g, Bitmap buffer, float[,] depthBuffer) =>
            this.DrawScene(this.State, g, buffer, depthBuffer);
    }
}
