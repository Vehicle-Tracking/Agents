using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AVT.Agent.Services
{
    public class DaemonService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly TaskRunnerBase _task;

        public DaemonService(
            ILogger<DaemonService> logger, 
            Simulator simulator, 
            Scheduler scheduler)
        {
            _logger = logger;
            var serviceName = Shared.Configuration["ServiceName"];
            if ("simulator".Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                _task = simulator;
            else
                _task = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _task.StartAsync(cancellationToken);
            _logger.LogDebug($"{DateTime.Now:yyy-MM-dd HH:mm:ss.ttt}: deamon [{_task.TaskName}] started ...");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _task.StopAsync(cancellationToken);
            _logger.LogDebug($"{DateTime.Now:yyy-MM-dd HH:mm:ss.ttt}: deamon [{_task.TaskName}] stopped ...");
        }

        public void Dispose()
        {
            _task.Dispose();
        }
    }
}