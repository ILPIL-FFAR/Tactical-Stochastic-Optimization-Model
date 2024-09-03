using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct CROPBUDGETModel
    {
        public string LOC { get; set; }
        public string CROP { get; set; }
        public double Cplant { get; set; }
        public double LabP { get; set; }
        public double LabH { get; set; }
        public double water { get; set; }
        public double minl { get; set; }
        public double maxl { get; set; }
        public double LaborH { get; set; }
        public double LaborP { get; set; }
    }
}
