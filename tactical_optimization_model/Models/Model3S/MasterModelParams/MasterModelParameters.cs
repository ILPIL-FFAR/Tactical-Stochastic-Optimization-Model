using ModelStochastic6.Models.SubModelParams;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.MasterModelParams
{
    class MasterModelParameters
    {
        public double init_week;
        public int nCut { get; set; }
        public double Target { get; set; }
        public List<CutsMod_param> cutsModList { get; set; }
        public List<HarvestPrice_param> harvestPriceList { get; set; }
        public List<SelectionPrice_param> selectionPriceList { get; set; }
        public List<DemandPrice2_param> demandPrice2List { get; set; }
        public List<ProductionPrice_param> productionPriceList { get; set; }
        public List<PackingPrice_param> packingPriceList { get; set; }
        public List<ScrapPrice_param> ScrapPriceList { get; set; }
        public List<SCENModel> valueScenList { get; set; }
        public List<Inv1Price_param> Inv1PriceList { get; set; }
        public List<InvwPrice_param> InvWPriceList { get; set; }
        public List<Inv2Price_param> Inv2PriceList { get; set; }
        public List<InvdPrice_param> InvDPriceList { get; set; }
        public List<SCPrice_param> SCPriceList { get; set; }
        public List<SPDPrice_param> SPDPriceList { get; set; }
        public List<SWDPrice_param> SWDPriceList { get; set; }
        public List<SDPrice_param> SDPriceList { get; set; }
        public List<SPD2Price_param> SPD2riceList { get; set; }

        public double[] MasterHarvestList;
        //public List<HarvestScenario_param> HarvestScenarioList { get; set;}
        public double Beta { get; set; }
        public List<Plant_param> plantList { get; set; }

        public List<FOpen_param> facOpenList { get; set; }

        public List<dcOpen_param> dcOpenList { get; set; }
    }
}
