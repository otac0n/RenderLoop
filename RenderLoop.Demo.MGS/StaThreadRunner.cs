namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class StaThreadRunner
    {
        public static Task RunAsync(Action action)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = false;
            thread.Start();

            return tcs.Task;
        }
    }
}
