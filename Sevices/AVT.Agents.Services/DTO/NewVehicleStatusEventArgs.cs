using System;

namespace Avt.Agents.Services.DTO
{
    public class NewVehicleStatusEventArgs : EventArgs
    {
        public VehicleStatusModel VehicleStatus { get; protected set; }
        public NewVehicleStatusEventArgs(VehicleStatusModel vehicleStatus)
        {
            this.VehicleStatus = vehicleStatus;
        }
    }
}