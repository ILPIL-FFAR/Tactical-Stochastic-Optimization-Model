using System;
using System.Collections.Generic;
using System.Text;


namespace ModelStochastic6.Models.SubModelParams
{
    class HarvestScenario_param
    {
        public WEEKPModel weekP { get; set; }

        public WEEKHModel weekHP { get; set; }
        public CROPModel crop { get; set; }
        public LOCModel loc { get; set; }
        public SCENModel scen { get; set; }
        public double Harvest { get; set; }
    }
}