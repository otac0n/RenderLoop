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

    internal class RenderLoopApplication : BackgroundService
    {
        private readonly ILogger<RenderLoopApplication> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IHostApplicationLifetime lifetime;

        public RenderLoopApplication(IHostApplicationLifetime lifetime, ILogger<RenderLoopApplication> logger, IServiceProvider serviceProvider)
        {
            this.lifetime = lifetime;
            this.logger = logger;
            this.serviceProvider = serviceProvider;

            this.lifetime.ApplicationStarted.Register(() => this.logger.LogInformation("started"));
            this.lifetime.ApplicationStopping.Register(() => this.logger.LogInformation("stopping"));
            this.lifetime.ApplicationStopped.Register(() => this.logger.LogInformation("stopped"));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => this.Run(stoppingToken));

        private void Run(CancellationToken cancel)
        {
            this.lifetime.StopApplication();
        }
    }
}
