using Newtonsoft.Json;

namespace Avt.Agents.Services.DTO
{
    public class VehicleStatusModel
    {
        [JsonProperty("vin")]
        public string VehicleId { get; set; }

        [JsonProperty("status")]
        public int VehicleStatus { get; set; }

        [JsonProperty("date")]
        public string StatusDate { get; set; }
    }
}