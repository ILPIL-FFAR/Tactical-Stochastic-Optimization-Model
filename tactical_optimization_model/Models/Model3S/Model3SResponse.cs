using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models.Model3S
{
    public class Model3SResponse
    {
        public Model3SRequest objToOptimize { get; set; }
        public List<LandAvailableResult> listLandAvailableResult { get; set; }
        public List<PlantResult> listPlantResult { get; set; }

        public List<OpenFResult> listOpenFResult { get; set; }
        public List<OpenDResult> listOpenDResult { get; set; }
        public List<HarvestResult> listHarvestResult { get; set; }
        public List<OPLResult> listOPLResult { get; set; }
        public List<OPFResult> listOPFResult { get; set; }
        public List<HireResult> listHireResult { get; set; }
        public List<FireResult> listFireResult { get; set; }
        public List<OPTResult> listOPTResult { get; set; }
        public IncomesCost3sResult incomeCostResult { get; set; }
        public List<VariabilityAssessmentResult> listVariabilityAssesmmentResult { get; set; }
        public int typeModel { get; set; }
        public bool hasError { get; set; }
        public string messageError { get; set; }
    }
}
