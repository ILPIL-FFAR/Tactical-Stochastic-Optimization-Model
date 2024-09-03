using Gurobi;
using ModelStochastic6.Models.Outputs.MasterModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.Outputs
{
    class MasterModelOutputs
    {
        public List<GeneralOutputs> Y { get; set; }
        public List<GeneralOutputs> Min_Var { get; set; }
        public double Cost_Tot { get; set; }
        public List<GeneralOutputs> Cost_Tot_Output { get; set; }
        public List<GeneralOutputs> Hire { get; set; }
        public List<GeneralOutputs> Fire { get; set; }
        public List<GeneralOutputs> Plant { get; set; }
        public List<GeneralOutputs> Min_Stage { get; set; }
        public List<GeneralOutputs> OPT { get; set; }
        public List<GeneralOutputs> OPL { get; set; }
        public List<GeneralOutputs> OPF { get; set; }
        public List<GeneralOutputs> OpenF { get; set; }
        public List<GeneralOutputs> OpenD { get; set; }
        //public List<GeneralOutputs> Harvest { get; set; }
        //public double[] HarvestSol;
        public double M_rev { get; set; }
        //public GRBModel gModel { get; set; }
        public bool hasError { get; set; }
        public int status { get; set; }
    }
}
