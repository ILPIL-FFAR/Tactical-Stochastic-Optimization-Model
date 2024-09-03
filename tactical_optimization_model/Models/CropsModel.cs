using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class CropsModel
    {
        public int id { get; set; }
        public string abbr_name { get; set; }
        public string crop_name { get; set; }
    }

    public class CropsConfigurationModel
    {
        public string id { get; set; }
        public string crop { get; set; }
        public double cplant { get; set; }
        public double pslav { get; set; }
        public double labp { get; set; }
        public double labh { get; set; }
        public string ccrop { get; set; }
        public double dharv { get; set; }
        public double water { get; set; }
        public double minl { get; set; }
        public double maxl { get; set; }
        public double laborH { get; set; }
        public double laborP { get; set; }

        public bool? active { get; set; }
        public string rlt_acc_crops_id { get; set; }
    }

    public class GrowerCropsConfigurationModel
    {
        public string cropId { get; set; }
        public string crop { get; set; }
        public string growerId { get; set; }
        public string grower { get; set; }
        public double cplant { get; set; }
        public double pslav { get; set; }
        public double labp { get; set; }
        public double labh { get; set; }
        public string ccrop { get; set; }
        public double dharv { get; set; }
        public double water { get; set; }
        public double minl { get; set; }
        public double maxl { get; set; }
        public double laborH { get; set; }
        public double laborP { get; set; }
    }
}
