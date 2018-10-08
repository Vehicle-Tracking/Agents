using System;

namespace AVT.Agent.DTO
{
    public class SchedulerDataModel
    {
        public string VehicleId { get; set; }
        public DateTime PreviousSchedule { get; set; }
        public DateTime NextSchedule { get; set; }
        public int State { get; set; }
    }
}