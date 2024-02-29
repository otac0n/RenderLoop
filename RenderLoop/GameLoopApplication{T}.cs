namespace RenderLoop
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public partial class GameLoopApplication<TGameLoop> : BackgroundService
        where TGameLoop : GameLoop
    {
        private readonly ILogger<GameLoopApplication<TGameLoop>> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IHostApplicationLifetime lifetime;

        public GameLoopApplication(IHostApplicationLifetime lifetime, ILogger<GameLoopApplication<TGameLoop>> logger, IServiceProvider serviceProvider)
        {
            this.lifetime = lifetime;
            this.logger = logger;
            this.serviceProvider = serviceProvider;

            this.lifetime.ApplicationStarted.Register(() => LogMessages.ApplicationStarted(this.logger, typeof(TGameLoop)));
            this.lifetime.ApplicationStopping.Register(() => LogMessages.ApplicationStopping(this.logger));
            this.lifetime.ApplicationStopped.Register(() => LogMessages.ApplicationStopped(this.logger));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => this.Run(stoppingToken), stoppingToken);

        private void Run(CancellationToken cancel)
        {
            var gameLoop = this.serviceProvider.GetRequiredService<TGameLoop>();
            gameLoop.Run(cancel);
            this.lifetime.StopApplication();
        }

        private static partial class LogMessages
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Application Started: {gameLoopType}")]
            public static partial void ApplicationStarted(ILogger logger, Type gameLoopType);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Application Stopping")]
            public static partial void ApplicationStopping(ILogger logger);

            [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Application Stopped")]
            public static partial void ApplicationStopped(ILogger logger);
        }
    }
}
