using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVT.Agent.DTO;

namespace AVT.Agent.Services
{
    public abstract class TaskRunnerBase : IDisposable
    {
        protected readonly SortedDictionary<double, SchedulerDataModel> JobQueue;
       // protected CancellationTokenSource Cts;
        private Task _runningTask;
        private readonly bool _intializeQueue;

        public abstract string TaskName { get; }
        protected TaskRunnerBase(bool intializeQueue = false)
        {
            this._intializeQueue = intializeQueue;
            JobQueue = new SortedDictionary<double, SchedulerDataModel>(
                Comparer<double>.Create((x, y) => x.CompareTo(y))); // sort ascendingly
        }

        protected abstract Task AlwaysRunner(CancellationToken cancellationToken);

        protected virtual void CreateItem(string vin, DateTime previuosDate, Action<string, double> callback = null)
        {
            this.CreateItem(vin, previuosDate, 60, callback);
        }

        protected virtual void CreateItem(string vin, DateTime previuosDate, int nextTimeInSecond, Action<string, double> callback = null)
        {
            var prevDate = previuosDate;
            var nextDate = prevDate.AddSeconds(nextTimeInSecond);
            var key = nextDate.Subtract(Shared.FIXED_DATE).TotalMilliseconds; // since it is sorting by the key, the first element will be always with the closest schedule due time to the moment

            JobQueue.Add(key, new SchedulerDataModel()
            {
                VehicleId = vin,
                NextSchedule = nextDate,
                PreviousSchedule = prevDate,
                State = 1
            });

            callback?.Invoke(vin, key);
        }

        private async Task Init(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var rnd = new Random();
                foreach (var vin in Shared.AllVehicles)
                {
                    CreateItem(vin, DateTime.UtcNow);
                    Task.Delay(TimeSpan.FromSeconds(rnd.Next(0, 2)), cancellationToken); // delay for better simulation of sending future pings
                }
            }, cancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_intializeQueue)
                await Init(cancellationToken);

            _runningTask = AlwaysRunner(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_runningTask == null)
            {
                return;
            }

            await Task.WhenAny(_runningTask, Task.Delay(-1, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            JobQueue.Clear();
            _runningTask = null;
        }
    }
}