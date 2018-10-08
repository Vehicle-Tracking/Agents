using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace AVT.Agent
{
    public class Shared
    {
        public static readonly string[] AllVehicles = new[]
        {
            "YS2R4X20005399401",
            "VLUR4X20009093588",
            "VLUR4X20009048066",
            "YS2R4X20005388011",
            "YS2R4X20005387949",
            "VLUR4X20009048065",
            "YS2R4X20005387055"
        };

        public static readonly DateTime FIXED_DATE = new DateTime(2018, 01, 01, 01, 0, 0, 0);

        public static readonly string Url = "https://j7n2b64t7b.execute-api.eu-west-1.amazonaws.com/default";

        public static IConfigurationRoot Configuration;

    }
}