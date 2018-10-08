using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AVT.Agent.Services
{
    public class Simulator : TaskRunnerBase
    {
        private readonly ILogger<Simulator> _logger;
        public Simulator(ILogger<Simulator> logger) : base(true)
        {
            _logger = logger;
        }
        public override string TaskName => "Simulator";
        protected override async Task AlwaysRunner(CancellationToken cancellationToken)
        {
            Random rnd = new Random();
            while (!cancellationToken.IsCancellationRequested)
            {
                while (true)
                {
                    try
                    {
                        var topKey = this.JobQueue.Keys.FirstOrDefault(); // null pointer for no key

                        if (JobQueue[topKey].NextSchedule.AddSeconds(-1) > DateTime.UtcNow &&
                            JobQueue[topKey].NextSchedule.Subtract(DateTime.UtcNow).TotalMilliseconds > 999.99d)
                            break;

                        var item = JobQueue[topKey];

                        if (rnd.Next(0, 11) % 5 == 0) //0, 5, 10 simulate unavailablity
                        {
                            JobQueue.Remove(topKey);
                            CreateItem(item.VehicleId, DateTime.UtcNow.AddSeconds(45));
                            continue;
                        }

                        while (!(await HttpHelper.SendStatus(item.VehicleId, 1, DateTime.UtcNow, cancellationToken)))
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
                        }

                        _logger.LogDebug($"successfully sent ping with status 1 for vehicle: {item.VehicleId} at around {DateTime.UtcNow}");
                        JobQueue.Remove(topKey);
                        CreateItem(item.VehicleId, DateTime.UtcNow);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in Simulator");
                        break; // give it a try in the next round
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}