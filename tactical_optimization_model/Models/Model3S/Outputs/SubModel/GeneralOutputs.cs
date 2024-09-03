using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.Outputs.SubModel
{
    public class GeneralOutputs
    {
        public double WEEKS { get; set; }
        public double WEEKH { get; set; }
        public double WEEKP { get; set; }
        public string PROD { get; set; }
        public double QUAL { get; set; }
        public string FAC { get; set; }
        public string CUST { get; set; }
        public string TRANS { get; set; }
        public WEEK1Model WEEK1 { get; set; }
        public string DC { get; set; }
        public string WARE { get; set; }
        public double SCEN { get; set; }
        public string CROP { get; set; }
        public string LOC { get; set; }
        public WORKModel WORK { get; set; }
        public WORK3Model WORK3 { get; set; }
        public double Value { get; set; }
        public double Dual { get; set; }
        public double URC { get; set; }
    }
}
