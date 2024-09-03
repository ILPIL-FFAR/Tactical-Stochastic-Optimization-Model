using Gurobi;
using ModelStochastic6.Models.Outputs.SubModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.Outputs
{
    class SubModelOutputs
    {
        //public List<GeneralOutputs> Harvest_s { get; set; }
        public List<GeneralOutputs> Pack { get; set; }
        public List<GeneralOutputs> SP { get; set; }
        public List<GeneralOutputs> SEL { get; set; }
        public List<GeneralOutputs> SC { get; set; }
        public List<GeneralOutputs> SD { get; set; }
        public List<GeneralOutputs> SW { get; set; }
        public List<GeneralOutputs> SPD { get; set; }
        public List<GeneralOutputs> SPW { get; set; }
        public List<GeneralOutputs> SWD { get; set; }
        public List<GeneralOutputs> Invd { get; set; }
        public List<GeneralOutputs> Invw { get; set; }
        public List<GeneralOutputs> TC { get; set; }
        public List<GeneralOutputs> TD { get; set; }
        public List<GeneralOutputs> TW { get; set; }
        public List<GeneralOutputs> TPD { get; set; }
        public List<GeneralOutputs> TPW { get; set; }
        public List<GeneralOutputs> TWD { get; set; }
        public List<GeneralOutputs> K { get; set; }
        public List<GeneralOutputs> Z { get; set; }
        public List<GeneralOutputs> HARVEST { get; set; }
        public List<GeneralOutputs> EXCESS { get; set; }
        public List<Constraint> Constraints { get; set; }

        public double Sub_rev { get; set; }
        public bool hasError { get; set; }
        public int status { get; set; }
    }
}
