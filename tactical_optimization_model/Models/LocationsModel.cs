using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class LocationsModel
    {

    }

    public class LocationsConfigurationModel
    {
        public string id { get; set; }
        public string location { get; set; }
        public string abbr { get; set; }
        public int la { get; set; }
        public double m { get; set; }
        public double w { get; set; }
        public double clabor { get; set; }
        public double ctemp { get; set; }
        public double chire { get; set; }
        public double mtemp { get; set; }
        public double mfix { get; set; }
        public double maxi { get; set; }
        public bool? active { get; set; }
        public string rlt_acc_location_id { get; set; }
        public double? amountGrowers { get; set; }
    }

    public class GrowerLocationsConfigurationModel
    {
        public string locationId { get; set; }
        public string location { get; set; }
        public string growerId { get; set; }
        public string grower { get; set; }
        public string abbr { get; set; }
        public int la { get; set; }
        public double m { get; set; }
        public double w { get; set; }
        public double clabor { get; set; }
        public double ctemp { get; set; }
        public double chire { get; set; }
        public double mtemp { get; set; }
        public double mfix { get; set; }
        public double maxi { get; set; }
    }

    public class GrowerLocations
    {
        public string id { get; set; }
        public string location { get; set; }
        public string abbr { get; set; }
        public int la { get; set; }
        public double w { get; set; }
        public double clabor { get; set; }
        public double ctemp { get; set; }
        public double chire { get; set; }
        public double mtemp { get; set; }
        public double mfix { get; set; }
        public double maxi { get; set; }
        public double amountGrowers { get; set; }

    }
}
