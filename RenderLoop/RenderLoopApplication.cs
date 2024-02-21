namespace RenderLoop
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using RenderLoop.MGS;

    internal partial class RenderLoopApplication : BackgroundService
    {
        private readonly ILogger<RenderLoopApplication> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IHostApplicationLifetime lifetime;

        public RenderLoopApplication(IHostApplicationLifetime lifetime, ILogger<RenderLoopApplication> logger, IServiceProvider serviceProvider)
        {
            this.lifetime = lifetime;
            this.logger = logger;
            this.serviceProvider = serviceProvider;

            this.lifetime.ApplicationStarted.Register(() => LogMessages.ApplicationStarted(this.logger));
            this.lifetime.ApplicationStopping.Register(() => LogMessages.ApplicationStopping(this.logger));
            this.lifetime.ApplicationStopped.Register(() => LogMessages.ApplicationStopped(this.logger));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => this.Run(stoppingToken));

        private void Run(CancellationToken cancel)
        {
            using var display = this.serviceProvider.GetRequiredService<VehicleDisplay>();

            cancel.Register(() => display.InvokeIfRequired(display.Close));
            Application.Run(display);
            this.lifetime.StopApplication();
        }

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Application Started")]
            public static partial void ApplicationStarted(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Application Stopping")]
            public static partial void ApplicationStopping(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Application Stopped")]
            public static partial void ApplicationStopped(ILogger logger);
        }
    }
}
