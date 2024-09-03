using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class YieldInputModel
    {
        public string state { get; set; }
        public string crop { get; set; }
        public int city { get; set; }
        public string tempScenario { get; set; }
        public int startPW { get; set; }
        public int endPW { get; set; }
        public double? harvestRate { get; set; }
        public double? minimalHarvest { get; set; }
        public double? standartYield { get; set; }
        public double? percentStandardYield { get; set; }
        public double? minimumTime { get; set; }
        public double? maximumHarvestTime { get; set; }

    }
}
