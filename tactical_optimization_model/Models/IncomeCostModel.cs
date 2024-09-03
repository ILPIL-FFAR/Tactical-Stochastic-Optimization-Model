using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class IncomeCostModel
    {
        public string crop { get; set; }
        public string location { get; set; }
        public double income { get; set; }
        public double costPlant { get; set; }
        public double costLog { get; set; }
        public double costTrans { get; set; }
    }

    public class IncomeCostResult
    {
        public double plantCost { get; set; }
        public double laborCost { get; set; }
        public List<IncomeCostModel> listIncomeCostResult { get; set; }

    }

    public class IncomeResult
    {
        public string crop { get; set; }
        public string grower { get; set; }
        public string location { get; set; }
        public double income { get; set; }
    }

    public class CostPlantResult
    {
        public string crop { get; set; }
        public string grower { get; set; }
        public string location { get; set; }
        public double costPlant { get; set; }
    }

    public class CostLogResult
    {
        public string crop { get; set; }
        public string grower { get; set; }
        public string location { get; set; }
        public double costLog { get; set; }
    }

    public class CostTransResult
    {
        public string crop { get; set; }
        public string grower { get; set; }
        public string location { get; set; }
        public double costTrans { get; set; }
    }

    public class CostLaborResult
    {
        public string grower { get; set; }
        public string location { get; set; }
        public double costLabor { get; set; }
    }

    public class IncomeCost2MGResult
    {
        public List<IncomeResult> listIncomeResult { get; set; }
        public List<CostLogResult> listCostLogResult { get; set; }
        public List<CostTransResult> listCostTransResult { get; set; }
        public List<CostPlantResult> listCostPlantResult { get; set; }
        public List<CostLaborResult> listCostLaborResult { get; set; }

    }

    public class Incomes3SProdScen
    {
        public double scen { get; set; }
        public string prod { get; set; }
        public double income { get; set; }
    }

    public class Incomes3SLocScen
    {
        public double scen { get; set; }
        public string loc { get; set; }
        public double income { get; set; }
    }

    public class Incomes3SLandAllocation
    {
        public string loc { get; set; }
        public double land { get; set; }
    }

    public class CostLog3SResult
    {
        public string prod { get; set; }
        public double scen { get; set; }
        public double costLog { get; set; }
    }

    public class CostTrans3SResult
    {
        public string prod { get; set; }
        public double scen { get; set; }
        public double costTrans { get; set; }
    }

    public class CostPlant3SResult
    {
        public string crop { get; set; }
        public string location { get; set; }
        public double costPlant { get; set; }
    }


    public class IncomesCost3sResult
    {
        public List<Incomes3SProdScen> listIncomeProdScenResult { get; set; }
        public List<Incomes3SLocScen> listIncomeLocScenResult { get; set; }
        public List<Incomes3SLandAllocation> listIncomeLandAllocationResult { get; set; }
        public List<CostPlant3SResult> listCostPlantResult { get; set; }
        public List<CostLog3SResult> listCostLogResult { get; set; }
        public List<CostTrans3SResult> listCostTransResult { get; set; }
    }

}
