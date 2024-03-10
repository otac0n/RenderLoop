namespace RenderLoop.SoftwareRenderer
{
    public class CooperativeIdleApplicationContext : ApplicationContext
    {
        private int displayCount;
        private int pendingOperations;

        public CooperativeIdleApplicationContext()
        {
            Application.Idle += this.Application_Idle;
        }

        public event Action? Idle;

        public int PendingOperations => this.pendingOperations;

        public void AddPendingOperation()
        {
            Interlocked.Increment(ref this.pendingOperations);
        }

        public void CompleteOperation()
        {
            var pendingOperations = Interlocked.Decrement(ref this.pendingOperations);
            if (pendingOperations <= 0)
            {
                this.Application_Idle(this, EventArgs.Empty);
            }
        }

        public Display CreateDisplay() =>
            new()
            {
                CooperativeIdleContext = this,
            };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.Idle -= this.Application_Idle;
            }

            base.Dispose(disposing);
        }

        private void Application_Idle(object? sender, EventArgs e)
        {
            Interlocked.Exchange(ref this.pendingOperations, 0);
            this.Idle?.Invoke();
        }

        internal void AddDisplay(Display display)
        {
            Interlocked.Increment(ref this.displayCount);
        }

        internal void RemoveDisplay(Display display)
        {
            var remaining = Interlocked.Decrement(ref this.displayCount);
            if (remaining <= 0)
            {
                this.ExitThread();
            }
        }
    }
}
