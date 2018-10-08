using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AVT.Agent.DTO;
using AVT.Agent.Utils;
using Microsoft.Extensions.Logging;

namespace AVT.Agent.Services
{
    public class Scheduler : TaskRunnerBase
    {
        private readonly ILogger<Simulator> _logger;
        
        private readonly Dictionary<string, SchedulerDataModel> _waitingList;
        private readonly Dictionary<string, double> _mappingList;
        private static readonly object locker = new object();
        public override string TaskName => "Scheduler";

        public event EventHandler NewStatusArrived;

        public Scheduler(ILogger<Simulator> logger) : base(false)
        {
            _logger = logger;
            _waitingList = new Dictionary<string, SchedulerDataModel>(0);
            _mappingList = new Dictionary<string, double>(0);
            NewStatusArrived += Scheduler_NewStatusArrived;
        }

        private void Scheduler_NewStatusArrived(object statusObject, EventArgs e)
        {
            var status = (VehicleStatusModel)statusObject;
            _logger.LogTrace($"New status for VehicleId:{status.VehicleId} with status:{status.VehicleStatus}");
            Reschedule(status);
        }

        private void Reschedule(VehicleStatusModel status)
        {
            if (!DateTime.TryParse(status.StatusDate, out var date))
                date = DateTime.UtcNow.AddSeconds(-5);

            // maybe we need a better approach in real-world, because here waiting list is not processed any more and if no message is received  vehicle
            // with specific vin then that vehicle will wait forever, this happens only when the queue can not be read
            lock (locker)
            {
                if (_waitingList.ContainsKey(status.VehicleId))
                {
                    _waitingList.Remove(status.VehicleId);

                    CreateItem(status.VehicleId, date, 75, AddToMappingList);
                }
                else
                {
                    if (_mappingList.ContainsKey(status.VehicleId))
                    {
                        var key = _mappingList[status.VehicleId];
                        if (JobQueue.ContainsKey(key))
                        {
                            JobQueue.Remove(key);
                            CreateItem(status.VehicleId, date, 75, AddToMappingList);
                        }
                    }
                    else
                    {
                        CreateItem(status.VehicleId, date, 75, AddToMappingList);
                    }
                }
            }
        }

        private async Task CheckeTimeout(CancellationToken cancellationToken)
        {
            while (true)
            {
                var topKey = double.NaN;
                SchedulerDataModel item = null;

                lock (locker)
                {
                    if (JobQueue.Keys.Count == 0)
                        break;

                    topKey = this.JobQueue.Keys.FirstOrDefault(); // null pointer for no key

                    if (JobQueue[topKey].NextSchedule.AddSeconds(-1) > DateTime.UtcNow &&
                        JobQueue[topKey].NextSchedule.Subtract(DateTime.UtcNow).TotalMilliseconds > 999.99d)
                        break;

                    item = JobQueue[topKey];
                }

                while (!(await HttpHelper.SendStatus(item.VehicleId, 0, DateTime.UtcNow, cancellationToken)))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
                }

                lock (locker)
                {
                    _waitingList.Add(item.VehicleId, item);
                    JobQueue.Remove(topKey);
                    break;
                }
            }
        }

        private async Task CheckQueueForNewStatus(CancellationToken cancellationToken)
        {
            try
            {
                var sqs = new AmazonSQSClient();
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = "https://sqs.eu-west-1.amazonaws.com/166778461577/StatusCheckSchedulerNotifierQueue"
                };
                ReceiveMessageResponse receiveMessageResponse = null;

                try
                {
                    receiveMessageResponse =
                        await sqs.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduler while working with SQS");
                }

                if (receiveMessageResponse?.Messages != null && receiveMessageResponse.Messages.Any())
                {
                    int counter = -1;
                    foreach (var message in receiveMessageResponse.Messages)
                    {
                        counter++;
                        if (!string.IsNullOrEmpty(message.Body))
                        {
                            var dto = JsonHelper.ReadObject<AwsSnsMessageDto>(message.Body);
                            var status = JsonHelper.ReadObject<VehicleStatusModel>(dto.Message);
                            OnNewStatusArrived(new NewVehicleStatusEventArgs(status));
                        }

                        var messageRecieptHandle = receiveMessageResponse.Messages[counter].ReceiptHandle;

                        var deleteRequest = new DeleteMessageRequest
                        {
                            QueueUrl = "https://sqs.eu-west-1.amazonaws.com/166778461577/StatusCheckSchedulerNotifierQueue",
                            ReceiptHandle = messageRecieptHandle
                        };
                        await sqs.DeleteMessageAsync(deleteRequest, cancellationToken);
                    }
                }
            }
            catch (AmazonSQSException ex)
            {
                _logger.LogError(ex, "Error in scheduler while working with SQS");
                // do nothing, you may want to log, but let it give it another try in the next run
            }
        }
        public void AddToMappingList(string vin, double key)
        {
            if (_mappingList.ContainsKey(vin))
                _mappingList[vin] = key;
            else
                _mappingList.Add(vin, key);
        }
        protected virtual void OnNewStatusArrived(NewVehicleStatusEventArgs e)
        {
            var handler = NewStatusArrived;
            handler?.Invoke(e.VehicleStatus, e);
        }

        protected override async Task AlwaysRunner(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (true)
                {
                    try
                    {
                        await CheckeTimeout(cancellationToken);
                        await CheckQueueForNewStatus(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in Scheduler");
                        break; // give it a try in the next round
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }


    }
}