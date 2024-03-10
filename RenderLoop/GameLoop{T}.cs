// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
{
    using System;
    using RenderLoop.SoftwareRenderer;
    using Silk.NET.Windowing;

    public abstract class GameLoop<TState> : GameLoop
    {
        private TState state;

        public GameLoop(IWindow display, TState initialState)
            : base(display)
        {
            this.state = initialState;
        }

        public GameLoop(CooperativeIdleApplicationContext context, TState initialState)
            : base(context)
        {
            this.state = initialState;
        }

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
