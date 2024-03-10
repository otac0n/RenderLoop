// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop.Demo
{
    using System;
    using System.Numerics;
    using RenderLoop.SoftwareRenderer;
    using Silk.NET.Windowing;

    public abstract class CameraSpinner : GameLoop<CameraSpinner.AppState>
    {
        public record class AppState(double T);

        protected CameraSpinner(IWindow window) : base(window, new AppState(0)) { }

        protected CameraSpinner(CooperativeIdleApplicationContext context) : base(context, new AppState(0)) { }

        protected CameraSpinner(Display display) : base(display, new AppState(0)) { }

        protected Camera Camera { get; } = new();

        protected sealed override void AdvanceFrame(ref AppState state, TimeSpan elapsed)
        {
            var dist = 2;

            var a = Math.Tau * state.T / 3;
            var (x, y) = Math.SinCos(a);
            var z = Math.Sin(a / 3);
            var p = new Vector3((float)(dist * x), (float)(dist * y), (float)(dist / 2 * z));

            this.Camera.Position = p;
            this.Camera.Direction = -p;

            state = state with
            {
                T = state.T + elapsed.TotalSeconds,
            };
        }
    }
}
