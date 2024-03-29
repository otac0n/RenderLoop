﻿// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RenderLoop
{
    using System;
    using RenderLoop.SoftwareRenderer;
    using System.Diagnostics;
    using System.Windows.Forms;
    using System.Threading;
    using Silk.NET.Windowing;

    public abstract class GameLoop : IDisposable
    {
        private readonly Action<CancellationToken> run;

        private long timestamp;

        public GameLoop(IWindow window)
        {
            void FirstUpdate(double t)
            {
                window.Update -= FirstUpdate;
                window.Update += Update;
                this.AdvanceFrame(TimeSpan.Zero);
            }

            void Update(double t)
            {
                this.AdvanceFrame(TimeSpan.FromSeconds(t));
            }

            window.Load += this.Initialize;
            window.Update += FirstUpdate;
            window.Render += t => this.DrawScene(TimeSpan.FromSeconds(t));

            this.run = (CancellationToken cancel) =>
            {
                cancel.Register(window.Close);
                window.Run();
            };
        }

        public GameLoop(Display display)
            : this(display.CooperativeIdleContext = new CooperativeIdleApplicationContext())
        {
            var context = display.CooperativeIdleContext;
            var run = this.run;
            this.run = cancel =>
            {
                display.Show();
                run(cancel);
                context.Dispose();
            };
        }

        protected GameLoop(CooperativeIdleApplicationContext context)
        {
            void FirstTick()
            {
                this.Initialize();
                var now = Stopwatch.GetTimestamp();
                this.timestamp = now;
                context.Idle -= FirstTick;
                context.Idle += Tick;
                this.Tick(TimeSpan.Zero);
            }

            void Tick()
            {
                var now = Stopwatch.GetTimestamp();
                var elapsed = Stopwatch.GetElapsedTime(this.timestamp, now);
                this.timestamp = now;
                this.Tick(elapsed);
            }

            context.Idle += FirstTick;

            this.run = (CancellationToken cancel) =>
            {
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
