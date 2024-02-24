namespace RenderLoop
{
    using System;
    using RenderLoop.SoftwareRenderer;

    public abstract class GameLoop<TState> : GameLoop
    {
        private TState state;

        public GameLoop(Display display, TState initialState)
            : base(display)
        {
            this.state = initialState;
        }

        public TState State => this.state;

        protected abstract void AdvanceFrame(ref TState state, TimeSpan elapsed);

        protected sealed override void AdvanceFrame(TimeSpan elapsed) =>
            this.AdvanceFrame(ref this.state, elapsed);

        protected abstract void DrawScene(TState state, TimeSpan elapsed);

        protected sealed override void DrawScene(TimeSpan elapsed) =>
            this.DrawScene(this.State, elapsed);
    }
}
