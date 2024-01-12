namespace RenderLoop
{
    using System;
    using System.Windows.Forms;

    internal static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (!control.IsDisposed)
            {
                if (control.InvokeRequired)
                {
                    try
                    {
                        control.Invoke(action);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
                else
                {
                    action();
                }
            }
        }
    }
}
