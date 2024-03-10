namespace RenderLoop.SoftwareRenderer
{
    public class MultiFormApplicationContext : ApplicationContext
    {
        private int displayCount;

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
