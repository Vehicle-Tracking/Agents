using System;

namespace AVT.Agent.DTO
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