using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class OperatorsResult
    {

    }

    public class OPFResult
    {
        public int week { get; set; }
        public string facility { get; set; }
        public double operatorsHoursRequired { get; set; }
        public string location { get; set; }

    }

    public class OPLResult
    {
        public int week { get; set; }
        public string location { get; set; }
        public double operatorsRequired { get; set; }
        public string grower { get; set; }

    }

    public class HireResult
    {
        public int week { get; set; }
        public string location { get; set; }
        public double operatorsToHire { get; set; }
        public string grower { get; set; }

    }

    public class FireResult
    {
        public int week { get; set; }
        public string location { get; set; }
        public double operatorsToFire { get; set; }
        public string grower { get; set; }
    }

    public class OPTResult
    {
        public int week { get; set; }
        public string location { get; set; }
        public double temporalOperatorsHired { get; set; }
        public string grower { get; set; }
    }
}
