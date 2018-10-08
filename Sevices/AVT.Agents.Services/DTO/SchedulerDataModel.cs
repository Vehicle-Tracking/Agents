using System;

namespace Avt.Agents.Services.DTO
{
    public class SchedulerDataModel
    {
        public string VehicleId { get; set; }
        public DateTime PreviousSchedule { get; set; }
        public DateTime NextSchedule { get; set; }
    }
}