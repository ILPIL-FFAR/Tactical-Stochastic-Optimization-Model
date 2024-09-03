using Gurobi;
using ModelStochastic6.Models;
using ModelStochastic6.Models.Inputs;
using ModelStochastic6.Models.MasterModelParams;
using ModelStochastic6.Models.Outputs;
using ModelStochastic6.Models.Outputs.SubModel;
using ModelStochastic6.Models.SubModelParams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WebApiDemoNetCore.Models;
using WebApiDemoNetCore.Models.Model3S;
using GeneralOutputsM = ModelStochastic6.Models.Outputs.MasterModel;

/* Main Changes since previous version
 *  Production { h in WEEKH,k in PROD,q in QUAL,f in FAC}: 
 * SINCE v0 (sent by Miguel on 06.01.2022): 
 * - Added "change in the optimality GAP" as stop criteria (parameter: gap_change_limit)
 * - Added selection of DETERMINISTIC or STOCHASTIC as boolean from user (paramter "deterministic" from Program.cs class) and uses either prices/yields scenarios or expected prices/yields (different inputs that are automatically read by ReadDataModel3S.cs class)
 * - Disposing the Gurobi GRBModel gModel after it is used.  No need to dispose GRBenv as opening a new one will create a new job in the cloud server.
 * - Use scenario probabilities to master model using inputData.ScenList[scl].Prob (rather than assuming uniform distribution of scenarios via 1/nScen) 
 * - Use SQL to read Prices and Yields tables based on user selection (Abhay developed)
 * - Use crop budget to differentiate resources and costs by location. The original input was CROP.xlsx, now we use also CROPBUDGET.xlsx
 * - Read CropBudget from database using SQL (Abhay developed)
 * 
 * SINCE v3 (sent to Luis on 07.06.2022):
 *  - Optimized the ReadDataModel3S.cs class to be more efficient querying/storing data
 *  - Removed the Salv parameter from the YieldModel.cs object as we do not use it.
 *  - Yield Data read only contains the values - see YIELDModel.cs (we already have the index position by using the index functions)... this reduced the RAM usage from 8GB (for 50 scen) to 1 GB just in terms of data.
 * 
 * SINCE v4 (intermediate internal version)
 * - Added the constant terms from the duals into a single value, rather than an array with all the values
 * - store part of the duals information for the cuts otuside the calcValue function, this allows to clean the object storing all the constraints data before calling the calcValue function
 * 
 * PENDING MODEL FUNCTIONALITY:
 * - Review Labor Constraints (Omar is working on this)
 * - Update Labor Parameters (LabH, LabP, LaborH, LaborP, costs, etc.)
 * - Update Logistics Parameters (estimated transportation costs)
 * - Fix the magicNumbers in the SubModel.cs and MasterModel.cs (they are hardcoded parameters)
 * - There are many restrictions/variables disabled by multiplying by zero in the Subproblem Objective Function (either add the proper parameter or remove if not used)
 * 
 * PENDING PERFORMANCE: 
 * - Based on the user selection of planning horizon we will "trim/filter" the inputData objects (WEEK,WEEKP,WEEKH,Price,Yields, etc.) to the corresponding weeks This will optimize the RAM memory used by the project, as now we are keeping too much data that is never used.
 * - Change string inputs (ie: location names) to integers ID to use less memory in the backend (ie: use a dictionary where we can match the corresponding ID to the name)
 * - ScenConDuals contains the duals of all the constraints, but not all are used in the calcValue function. Maybe just keep the values needed to optimiza space.
 * 
 * V5
 * - Added the use_random_fixed_scenarios parameter to use the same random scenarios throughout the entire process, rather than resampling at each iteration
 * - Should add some user input valdation e.g. can't enter true for both use_random_scnearios and use_random_fixed_scenarios
 * - Added conditions to when UB / LB are updated.
 * 
 * 
 * 
 * 
 * - Changed the gap_change_limit in Program.cs
 */
namespace ModelStochastic6
{
    class TacticalModelProgramModel3S
    {
        public double init_week = 1;
        public TacticalModelProgramModel3S(double init_week)
        {
            this.init_week = init_week;
        }
        public Model3SResponse RunModel(Model3SRequest model3SRequest, UserInputs userInputs)
        {
            Stopwatch runningWatch = new Stopwatch();
            runningWatch.Start();

            int nIterations = userInputs.maxIterations;
            int num_scenarios = userInputs.num_scenarios;
            double gap_change_limit = userInputs.gap_change_limit;

            int write_all_files = 1; // 1: write variables at every iteration

            DateTime Time_Program_Start = DateTime.Now;

            Console.Write("READING DATA...");

            string inputFolder = "..//..//..//Input_Files//InputsFiles_7crops//";
            string outputFolder = "..//..//..//Output_Files//OutputFiles_7crops//";

            ReadDataModel3S readData = new ReadDataModel3S();
            InputData inputData = readData.readData(inputFolder, model3SRequest);
            System.GC.Collect();
            if (userInputs.deterministic) // If running deterministic model, clean the information of scenarios
            {
                inputData.PriceList = null;
                inputData.YieldList = null;
            }
            else // if not deterministic, then clean information of expected values
            {
                inputData.PriceList_exp = null;
                inputData.YieldList_exp = null; 
            }

            //var non_zero = inputData.YieldList_exp.Where(x => x.Yield > 0).ToList();
  

            bool export_duals = false; //turn on or off this feature

            cleanFiles(outputFolder);


            // Create an empty environment, set options and start
            GRBEnv env = new GRBEnv(true);
            env.Presolve = -1;
            env.LogToConsole = 0; // 0 to Hide log : 1 to show
            env.Start(); // Start gurobi cloud servers

            //GLOBAL VARS
            int nCut = 0;
            int Type_cut;
            IndexesVar ixVar = new IndexesVar();

            //Length of lists
            int nWeekS = inputData.WeekSList.Count;
            int nProd = inputData.ProdList.Count;
            int nQual = inputData.QualList.Count;
            int nFac = inputData.FacList.Count;
            int nCust = inputData.CustList.Count;
            int nTrans = inputData.TransList.Count;
            int nWeek1 = inputData.Week1List.Count;
            int nDC = inputData.DistList.Count;
            int nWare = inputData.WareList.Count;
            int nScen = inputData.ScenList.Count;
            int nScenTotal = inputData.ScenList.Count;
            if (userInputs.deterministic)
            {
                nScen = 1;
                num_scenarios = 1;
            }
            if (num_scenarios > nScen)  // input validation
            {
                num_scenarios = nScen;
            }
            else
            {
                nScen = num_scenarios; // wasnt before
            }
            int nCrop = inputData.CropList.Count;
            int nLoc = inputData.LocList.Count;
            int nWeekP = inputData.WeekPList.Count;
            int nWeekH = inputData.WeekHList.Count;

            // Adjust number of scenarios and their probability (in this case uniform)
            inputData.ScenList = new List<SCENModel>();
            for (int sc = 0; sc < nScen; sc++)
            {
                SCENModel scM = new SCENModel { SCEN = sc + 1, Prob = 1.0 / nScen };
                inputData.ScenList.Add(scM);
            }
            //nScen = inputData.ScenList.Count;

            Console.Write(" OK.\n");

            SubModelParameters subModelParameters = new SubModelParameters()
            {
                Target_Rev = 4500000,  //Target value required to be satisfied
                nCut = nCut,
                cutsModList = new List<CutsMod_param>(),
                demandPrice2List = new List<DemandPrice2_param>(),
                harvestPriceList = new List<HarvestPrice_param>(),
                Inv1PriceList = new List<Inv1Price_param>(),
                Inv2PriceList = new List<Inv2Price_param>(),
                InvDPriceList = new List<InvdPrice_param>(),
                InvWPriceList = new List<InvwPrice_param>(),
                packingPriceList = new List<PackingPrice_param>(),
                productionPriceList = new List<ProductionPrice_param>(),
                priceList = new List<PriceCust_param>(),
                CpriceList = new List<CPriceCust_param>(),
                SCPriceList = new List<SCPrice_param>(),
                ScrapPriceList = new List<ScrapPrice_param>(),
                selectionPriceList = new List<SelectionPrice_param>(),
                SDPriceList = new List<SDPrice_param>(),
                SPD2riceList = new List<SPD2Price_param>(),
                SPDPriceList = new List<SPDPrice_param>(),
                SWDPriceList = new List<SWDPrice_param>(),
                valueScenList = new List<SCENModel>(),
                plantList = new List<Plant_param>(),
                facOpenList = new List<FOpen_param>(),
                dcOpenList = new List<dcOpen_param>(),

            };
            MasterModelParameters masterModelParameters = new MasterModelParameters()
            {
                nCut = nCut,
                Target = 0,
                cutsModList = new List<CutsMod_param>(),
                demandPrice2List = new List<DemandPrice2_param>(),
                harvestPriceList = new List<HarvestPrice_param>(),
                Inv1PriceList = new List<Inv1Price_param>(),
                Inv2PriceList = new List<Inv2Price_param>(),
                InvDPriceList = new List<InvdPrice_param>(),
                InvWPriceList = new List<InvwPrice_param>(),
                packingPriceList = new List<PackingPrice_param>(),
                productionPriceList = new List<ProductionPrice_param>(),
                SCPriceList = new List<SCPrice_param>(),
                ScrapPriceList = new List<ScrapPrice_param>(),
                selectionPriceList = new List<SelectionPrice_param>(),
                SDPriceList = new List<SDPrice_param>(),
                SPD2riceList = new List<SPD2Price_param>(),
                SPDPriceList = new List<SPDPrice_param>(),
                SWDPriceList = new List<SWDPrice_param>(),
                valueScenList = new List<SCENModel>(),
                plantList = new List<Plant_param>(),
                facOpenList = new List<FOpen_param>(),
                dcOpenList = new List<dcOpen_param>(),

            };
            masterModelParameters.init_week = inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("Initial_Week")).Value;

            MasterModelOutputs masterModelOutputs = new MasterModelOutputs()
            {
                Cost_Tot_Output = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                Fire = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                Hire = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                Min_Stage = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                Min_Var = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                OPF = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                OPL = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                OPT = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                Plant = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                OpenF = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                OpenD = new List<Models.Outputs.MasterModel.GeneralOutputs>(),
                Y = new List<Models.Outputs.MasterModel.GeneralOutputs>()
            };

            // Get initial solution (Planting) set as value in Plant0.xlsx
            inputData.WeekPList.ForEach(wpl =>
            {
                int ixWP = inputData.WeekPList.IndexOf(wpl);
                inputData.CropList.ForEach(cl =>
                {
                    int ixC = inputData.CropList.IndexOf(cl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);

                        int ix3 = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                        double plantValue = inputData.Plant0List[ix3].Plant;
                        if (plantValue > 0)
                        {
                            string s = "";
                        }
                        Plant_param pp = new Plant_param()
                        {
                            weekP = wpl,
                            crop = cl,
                            loc = ll,
                            plant = plantValue
                        };
                        masterModelParameters.plantList.Add(pp);
                    });
                });
            });
            plantParamStaticList(inputData, masterModelParameters.plantList);

            // Start with all facilities open = 1
            inputData.FacList.ForEach(fl =>
            {
                int ixF = inputData.FacList.IndexOf(fl);
                FOpen_param fac = new FOpen_param()
                {
                    fac = fl,
                    open = 1
                };
                masterModelParameters.facOpenList.Add(fac);

            });

            // Start with all dist. centers open = 1
            inputData.DistList.ForEach(dl =>
            {
                int ixD = inputData.DistList.IndexOf(dl);
                dcOpen_param dist = new dcOpen_param()
                {
                    dist = dl,
                    open = 1
                };
                masterModelParameters.dcOpenList.Add(dist);
            });

            //Min_Stage[s]:= Infinity;
            inputData.ScenList.ForEach(scl =>
            {
                int ixSC = inputData.ScenList.IndexOf(scl);
                GeneralOutputsM.GeneralOutputs go = new GeneralOutputsM.GeneralOutputs
                {
                    SCEN = scl.SCEN,
                    Value = 50000000000
                };
                masterModelOutputs.Min_Stage.Add(go);
            });

            //GLOBAL VARS
            double LB = double.MinValue;
            double UB = double.MaxValue;
            double Valuecut = 0.0;
            double Termination_Threshold = 1000; //Threshold to stop algorithm (When (UB-LB) falls below this, the algorithm stops)

            double Beta = 0;
            masterModelParameters.Beta = Beta;

            //Lists that store the master model decisions at each iteraiton
            MasterModelOutputs masterOutput_toPrint = new MasterModelOutputs()
            {
                Plant = new List<GeneralOutputsM.GeneralOutputs>(),
                Hire = new List<GeneralOutputsM.GeneralOutputs>(),
                Fire = new List<GeneralOutputsM.GeneralOutputs>(),
                OPF = new List<GeneralOutputsM.GeneralOutputs>(),
                OPL = new List<GeneralOutputsM.GeneralOutputs>(),
                OPT = new List<GeneralOutputsM.GeneralOutputs>(),
                Y = new List<GeneralOutputsM.GeneralOutputs>(),
                OpenF = new List<GeneralOutputsM.GeneralOutputs>(),
                OpenD = new List<GeneralOutputsM.GeneralOutputs>()
            };

            // Create lists to store the results
            List<LandAvailableResult> landAvailableListResult = new List<LandAvailableResult>();
            List<PlantResult> plantingListResult = new List<PlantResult>();
            List<OpenFResult> openfListResult = new List<OpenFResult>();
            List<OpenDResult> opendListResult = new List<OpenDResult>();
            List<OPFResult> opfListResult = new List<OPFResult>();
            List<OPLResult> oplListResult = new List<OPLResult>();
            List<HireResult> hireListResult = new List<HireResult>();
            List<FireResult> fireListResult = new List<FireResult>();
            List<OPTResult> optListResult = new List<OPTResult>();
            IncomesCost3sResult incomeCostResult = new IncomesCost3sResult()
            {
                listIncomeProdScenResult = new List<Incomes3SProdScen>(),
                listIncomeLocScenResult = new List<Incomes3SLocScen>(),
                listIncomeLandAllocationResult = new List<Incomes3SLandAllocation>(),
                listCostPlantResult = new List<CostPlant3SResult>(),
                listCostTransResult = new List<CostTrans3SResult>(),
                listCostLogResult = new List<CostLog3SResult>()
            };
            List<VariabilityAssessmentResult> variailityAssessmentListResult = new List<VariabilityAssessmentResult>();
            List<SCResult> scListResult = new List<SCResult>();

            //Lists that store submodel decisions for each scenario
            SubModelOutputs[] subOutput_toPrint = new SubModelOutputs[inputData.ScenList.Count];
            for (int j = 0; j < inputData.ScenList.Count; j++)
            {
                subOutput_toPrint[j] = new SubModelOutputs();
                subOutput_toPrint[j].Invd = new List<GeneralOutputs>();
                subOutput_toPrint[j].Invw = new List<GeneralOutputs>();
                subOutput_toPrint[j].SC = new List<GeneralOutputs>();
                subOutput_toPrint[j].SD = new List<GeneralOutputs>();
                subOutput_toPrint[j].SW = new List<GeneralOutputs>();
                subOutput_toPrint[j].Pack = new List<GeneralOutputs>();
                subOutput_toPrint[j].SPD = new List<GeneralOutputs>();
                subOutput_toPrint[j].SPW = new List<GeneralOutputs>();
                subOutput_toPrint[j].SWD = new List<GeneralOutputs>();
                subOutput_toPrint[j].SP = new List<GeneralOutputs>();
                subOutput_toPrint[j].SEL = new List<GeneralOutputs>();
                subOutput_toPrint[j].TC = new List<GeneralOutputs>();
                subOutput_toPrint[j].TD = new List<GeneralOutputs>();
                subOutput_toPrint[j].TW = new List<GeneralOutputs>();
                subOutput_toPrint[j].TPD = new List<GeneralOutputs>();
                subOutput_toPrint[j].TPW = new List<GeneralOutputs>();
                subOutput_toPrint[j].TWD = new List<GeneralOutputs>();
                subOutput_toPrint[j].K = new List<GeneralOutputs>();
                subOutput_toPrint[j].Z = new List<GeneralOutputs>();
                subOutput_toPrint[j].HARVEST = new List<GeneralOutputs>();
                subOutput_toPrint[j].EXCESS = new List<GeneralOutputs>();
            }
            // Stores the cut information as the alorithm progresses
            List<List<SubCut>> Cuts = new List<List<SubCut>>();

            List<DownDev_param> DownDev = new List<DownDev_param>();
            for (int s = 0; s < num_scenarios; s++)
            {
                DownDev_param ddp = new DownDev_param()
                {
                    SCEN = s + 1,
                    DownDev = 0
                };
                DownDev.Add(ddp);
            }

            double currentGAP = 0.0;
            double previousGAP = 0.00001;

            //For collecting random indices to use for running the model
            Random rnd = new Random();
            List<int> Random_Scenarios = new List<int>();

            //Draw random numbers in case use_random_scenarions is fixed
            
            List<int> Fixed_Random_Scenarios = new List<int>();
            for (int r = 0; r < num_scenarios; r++)
            {
                int fixed_scenario = rnd.Next(0, nScenTotal);
                Fixed_Random_Scenarios.Add(fixed_scenario);

            }
           


            /************************************************** 
             *      START THE ALGORITHM
             **************************************************
             */
            for (int i = 0; i < nIterations; i++)
            {
                Console.WriteLine("\n############# ITERATION NUMBER: " + (i + 1) + " #############");
                //Reset the number of solved scenarios
                int SolvedScenarios = new int();
                //reset the counter for the number of Solved Scenarios
                SolvedScenarios = 0;

                List<SubCut> SubCuts = new List<SubCut>();
                int newCuts = 0;
                ArrayList SubModelDecisions2 = new ArrayList();
                List<SubModelOutputs[]> SubModelDecisions = new List<SubModelOutputs[]>();

                // Create empty Gurobi model
                GRBModel gModel = new GRBModel(env);
                gModel.Parameters.MIPGap = 0.01 / 100; //set the GAP to 0.01%

                //list to store the harvest values for each scenario
                masterModelParameters.MasterHarvestList = new double[nWeekP * nWeekH * nCrop * nLoc * num_scenarios];

                // Update master and subproblem cut number
                nCut += 1;
                subModelParameters.nCut = nCut;
                masterModelParameters.nCut = nCut;
                masterModelParameters.valueScenList = new List<SCENModel>();

                // reset counters
                Valuecut = 0;
                Type_cut = 1;
                // Collects the values needed to for the cuts
                double Values = 0;
                double MeanSol = 0;

                //If we are using the same random scenarios for the entire process, set the list here, else reinitiate the list
                if (userInputs.use_random_fixed_scenarios)
                {
                    Random_Scenarios = new List<int>();
                    Random_Scenarios = Fixed_Random_Scenarios;
                }
                else
                {
                    Random_Scenarios = new List<int>();
                }

                /* *****************************************
                 * RUN EACH OF THE SCENARIOS (Subproblems)
                 * *****************************************
                 */
                for (int scl = 0; scl < num_scenarios; scl++)
                {
                    
                   
                    // Create new Cut object to store information for this scenario             
                    SubCut new_cut = new SubCut();
                    new_cut.CutsMods = new double();
                    new_cut.duals = new Duals();

                    // Create a new list to store all the duals for the constraints for each scen in THIS CUT (we only want to store  values for constraints where there is a variable in cut_defn in the master problem)
                    List<double> ScenConDuals = new List<double>();
                    List<string> ScenConNames = new List<string>();
                    ConstInfo ScenConstInfo = new ConstInfo();
                    Duals ScenDuals = new Duals();

                    //random integer between 0 and nScenTotal (index of the randomly selected scnenario)
                    int random_scenario = rnd.Next(0, nScenTotal);
                    int ixSC = 0;
                    
                    // CHECK
                    //Select scenerio from the pre-drawm random scenarios that will be used for the duration of the algorithm
                    if (userInputs.use_random_fixed_scenarios)
                    {
                        ixSC = Fixed_Random_Scenarios[scl];
                    }
                  
                    else if (userInputs.use_random_scenarios && !userInputs.deterministic)
                    {
                        ixSC = random_scenario;
                        Random_Scenarios.Add(ixSC);
                    }
                    else
                    {
                        ixSC = scl;
                        Random_Scenarios.Add(ixSC);
                    }
                    Console.WriteLine("\n--------- Subproblem " + nCut + " - " + (scl + 1).ToString() + " (scen: " + (ixSC+1).ToString() + ") ---------");

                   

                    // Get estimated yield for this scenario
                    subModelParameters.HarvestList = new double[nWeekP * nWeekH * nCrop * nLoc];
                    for (int p = 0; p < nWeekP; p++)
                    {
                        for (int h = 0; h < nWeekH; h++)
                        {
                            for (int j = 0; j < nCrop; j++)
                            {
                                for (int l = 0; l < nLoc; l++)
                                {
                                    double yield = 0;
                                    int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                    if (userInputs.deterministic)  // use expected yield if the model is deterministic
                                    {
                                        yield = inputData.YieldList_exp.ElementAt(ix4).Yield;
                                    }
                                    else  // use scenario yield if the model is stochastic
                                    {
                                        int ix5 = ixVar.getIx5(p, h, j, l, ixSC, nWeekP, nWeekH, nCrop, nLoc, nScenTotal);
                                        yield = inputData.YieldList[ix5].Yield;

                                    }
                                    int ix3 = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                                    double plant = masterModelParameters.plantList[ix3].plant;


                                    double total = 1;
                                    subModelParameters.HarvestList[ix4] = plant * yield * total;
                                }
                            }
                        }
                    }

                    var non_zero2 = subModelParameters.HarvestList.Where(x => x > 0).ToList();

                    subModelParameters.priceList = new List<PriceCust_param>();
                    // get the prices for this scenario
                    for (int t = 0; t < nWeekS; t++)
                    {
                        for (int k = 0; k < nProd; k++)
                        {
                            for (int c = 0; c < nCust; c++)
                            {
                                double priceS = 0;
                                try
                                {
                                    if (userInputs.deterministic)
                                    {
                                        int ix3 = ixVar.getIx3(t, k, c, nWeekS, nProd, nCust);
                                        priceS = inputData.PriceList_exp[ix3].Price;
                                    }
                                    else
                                    {
                                        int ix4 = ixVar.getIx4(t, k, c, ixSC, nWeekS, nProd, nCust, nScenTotal);
                                        priceS = inputData.PriceList[ix4].Price;
                                    }
                                }
                                catch { Console.WriteLine("Error getting the prices.\n"); }
                                PriceCust_param pcp = new PriceCust_param()
                                {
                                    weeks = inputData.WeekSList[t],
                                    prod = inputData.ProdList[k],
                                    cust = inputData.CustList[c],
                                    price = priceS
                                };
                                subModelParameters.priceList.Add(pcp);
                            }
                        }
                    }

                    subModelParameters.facOpenList = new List<FOpen_param>();
                    for (int f = 0; f < nFac; f++)
                    {
                        double open = masterModelParameters.facOpenList[f].open;
                        FOpen_param fac = new FOpen_param()
                        {
                            fac = inputData.FacList[f],
                            open = open
                        };
                        subModelParameters.facOpenList.Add(fac);
                    }

                    //read in DC decisions
                    subModelParameters.dcOpenList = new List<dcOpen_param>();
                    for (int d = 0; d < nDC; d++)
                    {
                        double open = masterModelParameters.dcOpenList[d].open;
                        dcOpen_param dist = new dcOpen_param()
                        {
                            dist = inputData.DistList[d],
                            open = open
                        };
                        subModelParameters.dcOpenList.Add(dist);
                    }

                    // Solve the subproblem                    
                    gModel = new GRBModel(env);
                    SubModel model = new SubModel();
                    SubModelOutputs submodelOutputs = model.buildModel(gModel, inputData, subModelParameters, outputFolder, scl, userInputs);

                    // Initialize
                    ScenConstInfo.Constraints = new List<Constraint>();
                    ScenDuals.Sum_harvest = new List<double>();
                    ScenDuals.Init_FAC = new List<double>();
                    ScenDuals.Tot_packaging = new List<double>();
                    ScenDuals.Cap_PF = new List<double>();
                    ScenDuals.Constants = 0.0;//To hold the summation of all dual values that are not associated with a master problem variable

                    //if solve_result = 'infeasible' (3) or Infeasible or unbounded (4)
                    if (submodelOutputs.status == 4 || submodelOutputs.status == 3)
                    {
                        Console.WriteLine("* Problem infeasible.");
                        var csvFile = new StringBuilder();
                        csvFile.Clear();
                        csvFile.AppendLine("Con_Name,Dual");

                        // Get the constraints for this current scenario and add it to the list of constraints
                        foreach (Constraint cr in submodelOutputs.Constraints)
                        {
                            ScenConstInfo.Constraints.Add(cr); //add the info for this scenario
                        }
                        GRBConstr[] scen_constr1 = gModel.GetConstrs();
                        for (int ix = 0; ix < scen_constr1.Length; ix++)
                        {
                            ScenConDuals.Add(scen_constr1[ix].Get(GRB.DoubleAttr.FarkasDual));
                        }
                        // Get Farkas Dual for Sum-harvest constraint
                        for (int p = 0; p < nWeekP; p++)
                        {
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    for (int l = 0; l < nLoc; l++)
                                    {
                                        int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                        var Sum_harvest = ScenConstInfo.Constraints.Find(c => c.name == "Sum_harvest");
                                        var dl = scen_constr1[Sum_harvest.startAt + ix4].Get(GRB.DoubleAttr.FarkasDual);
                                        ScenDuals.Sum_harvest.Add(dl);
                                        string cname = scen_constr1[Sum_harvest.startAt + ix4].Get(GRB.StringAttr.ConstrName);
                                        csvFile.AppendLine($"{cname},{Convert.ToString(dl)}");
                                    }
                                }
                            }
                        }

                       

                        // Get Farkas Dual for Cap_PF constraint
                        for (int h = 0; h < nWeekH; h++)
                        {
                            for (int f = 0; f < nFac; f++)
                            {
                                int ix = ixVar.getIx2(h, f, nWeekH, nFac);
                                var Cap_pf = ScenConstInfo.Constraints.Find(c => c.name == "Cap_PF");
                                var dl = scen_constr1[Cap_pf.startAt + ix].Get(GRB.DoubleAttr.FarkasDual);

                                ScenDuals.Cap_PF.Add(dl);
                                string cname = scen_constr1[Cap_pf.startAt + ix].Get(GRB.StringAttr.ConstrName);
                                csvFile.AppendLine($"{cname},{Convert.ToString(dl)}");
                            }
                        }
                        scen_constr1 = null; // clean information not used anymore

                        //Export duals 
                        if (export_duals)
                        {
                            string path = outputFolder + "Subproblem//Duals//";
                            File.WriteAllText(path + "Duals_" + nCut + "_" + (scl + 1) + ".csv", csvFile.ToString());
                        }
                        else { csvFile.Clear(); }

                        //SolvedScenarios = 0;

                        // Calculate the value of "Value" and store the duals to be sent to the masterproblem
                        var tuple = calcValues(inputData, ScenConDuals, ScenConstInfo, ref ScenDuals, subModelParameters, masterModelOutputs, SolvedScenarios, ixSC, scl, userInputs.activate_target, userInputs);
                        Values = tuple.Item1;
                        Valuecut = Valuecut + tuple.Item2;

                        double dCutsMod = 2; // 0 = no cut added, 1 = optimality cut added, 2 = feasibility cut added
                        new_cut.CutsMods = dCutsMod;
                        new_cut.duals = ScenDuals; // Add the duals for each scenario (in if no cut, they are zero)
                        newCuts = newCuts + 1;

                        Console.Write("\nNew Cut: Feasibility\n");
                        SubCuts.Add(new_cut);
                    }
                    else // if the subproblem was solved to optimality
                    {
                        Console.Write("Obtaining duals:");
                        GRBConstr[] scen_constr1 = gModel.GetConstrs();
                        var csvFile = new StringBuilder();

                        ////FOR TROUBLESHOOTING CUTS
                        csvFile.Clear();
                        csvFile.AppendLine("Con_Name,Dual");
                        double[] scen_constr = new double[scen_constr1.Length];
                        for (int ix = 0; ix < scen_constr1.Length; ix++)
                        {
                            scen_constr[ix] = scen_constr1[ix].Get(GRB.DoubleAttr.Pi);
                            string cname = scen_constr1[ix].Get(GRB.StringAttr.ConstrName);
                            if(scen_constr[ix]!=0)
                            {
                                csvFile.AppendLine($"{cname},{scen_constr[ix]}");
                            }
                        }
                        if (export_duals)
                        {
                            File.WriteAllText(outputFolder + "SubProblem//Duals//FullDuals_" + nCut + "_" + (scl + 1) + ".csv", csvFile.ToString());
                        }
                        else { csvFile.Clear(); }



                        
                        csvFile.Clear();
                        csvFile.AppendLine("Con_Name,Dual");
                        foreach (Constraint cr in submodelOutputs.Constraints)
                        {
                            ScenConstInfo.Constraints.Add(cr);
                        }
                        // Get all duals
                        for (int ix = 0; ix < scen_constr1.Length; ix++)
                        {
                            
                            ScenConDuals.Add(scen_constr1[ix].Get(GRB.DoubleAttr.Pi));
                        }
                        // Get Duals for Sum_harvest constraint
                        for (int p = 0; p < nWeekP; p++)
                        {
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    for (int l = 0; l < nLoc; l++)
                                    {
                                        int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                        var Sum_harvest = ScenConstInfo.Constraints.Find(c => c.name == "Sum_harvest");
                                        var dl = scen_constr1[Sum_harvest.startAt + ix4].Get(GRB.DoubleAttr.Pi);
                                        ScenDuals.Sum_harvest.Add(dl);
                                        string cname = scen_constr1[Sum_harvest.startAt + ix4].Get(GRB.StringAttr.ConstrName);
                                        csvFile.AppendLine($"{cname},{Convert.ToString(dl)}");
                                    }
                                }
                            }
                        }
                        // Get Duals for Cap_PF constraint
                        for (int h = 0; h < nWeekH; h++)
                        {
                            for (int f = 0; f < nFac; f++)
                            {
                                int ix = ixVar.getIx2(h, f, nWeekH, nFac);
                                var Cap_pf = ScenConstInfo.Constraints.Find(c => c.name == "Cap_PF");
                                var dl = scen_constr1[Cap_pf.startAt + ix].Get(GRB.DoubleAttr.Pi);
                                ScenDuals.Cap_PF.Add(dl);
                                string cname = scen_constr1[Cap_pf.startAt + ix].Get(GRB.StringAttr.ConstrName);
                                csvFile.AppendLine($"{cname},{Convert.ToString(dl)}");
                            }
                        }
                        scen_constr1 = null; // not needed anymore
                        if (export_duals)
                        {
                            string path = outputFolder + "Subproblem//Duals//";
                            File.WriteAllText(path + "Duals_" + nCut + "_" + (scl + 1) + ".csv", csvFile.ToString());
                        }
                        else { csvFile.Clear(); }
                        Console.Write("\tOK.\n");

                        SolvedScenarios = SolvedScenarios + 1;
                        // Calculate the value of "Value" and store the duals
                        var tuple = calcValues(inputData, ScenConDuals, ScenConstInfo, ref ScenDuals, subModelParameters, masterModelOutputs, SolvedScenarios, ixSC, scl, userInputs.activate_target, userInputs);
                        Values = tuple.Item1;

                        ScenConstInfo = new ConstInfo();
                        ScenConDuals = null;

                        // Create an optimality cut if the estimate of the second stage scenario is still greater than the actual objective function plus some small value .... CHECK may need this condition to be compared to Values, instead of sub_rev...
                        if (masterModelOutputs.Min_Stage[scl].Value > submodelOutputs.Sub_rev + 0.0001)
                        {
                            double dCutsMod = 1; // 0 = no cut added, 1 = optimality cut added, 2 = feasibility cut added
                            new_cut.CutsMods = dCutsMod;
                            new_cut.duals = ScenDuals; // Add the duals for each scenario (in if no cut, they are zero)   //
                            newCuts = newCuts + 1;
                            Valuecut = Valuecut + tuple.Item2; //add value to the cut
                            Console.Write("\nNew Cut: Optimality\n");
                        }
                        else // if the estimate of the second stage scenario and the corresponding subproblem objective function have converged, no not add an optimality cut
                        {
                            double dCutsMod = 0; // 0 = no cut added, 1 = optimality cut added, 2 = feasibility cut added
                            new_cut.CutsMods = dCutsMod;

                        };

                        // Add the new cut with its info.
                        SubCuts.Add(new_cut);

                        // remove all rows of zeros from the decisions
                        // TEST THIS

                        submodelOutputs.Invd.RemoveAll(v => v.Value == 0);
                        submodelOutputs.Invw.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SC.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SD.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SW.RemoveAll(v => v.Value == 0);
                        submodelOutputs.Pack.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SPD.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SPW.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SWD.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SP.RemoveAll(v => v.Value == 0);
                        submodelOutputs.SEL.RemoveAll(v => v.Value == 0);
                        submodelOutputs.TC.RemoveAll(v => v.Value == 0);
                        submodelOutputs.TD.RemoveAll(v => v.Value == 0);
                        submodelOutputs.TW.RemoveAll(v => v.Value == 0);
                        submodelOutputs.TPD.RemoveAll(v => v.Value == 0);
                        submodelOutputs.TPW.RemoveAll(v => v.Value == 0);
                        submodelOutputs.TWD.RemoveAll(v => v.Value == 0);
                        submodelOutputs.K.RemoveAll(v => v.Value == 0);
                        submodelOutputs.Z.RemoveAll(v => v.Value == 0);
                        submodelOutputs.EXCESS.RemoveAll(v => v.Value == 0);
                        submodelOutputs.HARVEST.RemoveAll(v => v.Value == 0);

                        // add to the output object
                        subOutput_toPrint[scl] = submodelOutputs;
                        subOutput_toPrint[scl].Invd = submodelOutputs.Invd;
                        subOutput_toPrint[scl].Invw = submodelOutputs.Invw;
                        subOutput_toPrint[scl].SC = submodelOutputs.SC;
                        subOutput_toPrint[scl].SD = submodelOutputs.SD;
                        subOutput_toPrint[scl].SW = submodelOutputs.SW;
                        subOutput_toPrint[scl].Pack = submodelOutputs.Pack;
                        subOutput_toPrint[scl].SPD = submodelOutputs.SPD;
                        subOutput_toPrint[scl].SPW = submodelOutputs.SPW;
                        subOutput_toPrint[scl].SWD = submodelOutputs.SWD;
                        subOutput_toPrint[scl].SP = submodelOutputs.SP;
                        subOutput_toPrint[scl].SEL = submodelOutputs.SEL;
                        subOutput_toPrint[scl].TC = submodelOutputs.TC;
                        subOutput_toPrint[scl].TD = submodelOutputs.TD;
                        subOutput_toPrint[scl].TW = submodelOutputs.TW;
                        subOutput_toPrint[scl].TPD = submodelOutputs.TPD;
                        subOutput_toPrint[scl].TPW = submodelOutputs.TPW;
                        subOutput_toPrint[scl].TWD = submodelOutputs.TWD;
                        subOutput_toPrint[scl].K = submodelOutputs.K;
                        subOutput_toPrint[scl].Z = submodelOutputs.Z;
                        subOutput_toPrint[scl].HARVEST = submodelOutputs.HARVEST;
                        subOutput_toPrint[scl].EXCESS = submodelOutputs.EXCESS;

                        MeanSol = MeanSol + submodelOutputs.Sub_rev * inputData.ScenList[scl].Prob;
                        DownDev_param downDev = DownDev.Find(ddl => ddl.SCEN.Equals(scl + 1));
                        downDev.DownDev = Math.Max(masterModelParameters.Target - submodelOutputs.Sub_rev, 0) * inputData.ScenList[scl].Prob;

                        // Update the harvest corresponding to this scenario
                        for (int p = 0; p < nWeekP; p++)
                        {
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    for (int l = 0; l < nLoc; l++)
                                    {
                                        int ix5 = ixVar.getIx5(p, h, j, l, scl, nWeekP, nWeekH, nCrop, nLoc, nScen);
                                        int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                        double harv = subModelParameters.HarvestList[ix4];
                                        if (harv > 0)
                                        {
                                            int check = 0;
                                        }

                                        masterModelParameters.MasterHarvestList[ix5] = harv;  //PENDING: we can optimize this parameter as it gets TOO big as the scenarios progress. We are storing a lot of zero values (ie: for planting weeks before the season start)
                                    }
                                }
                            }
                        }
                        // Display value of Objective Function
                        Console.WriteLine("OF Value (Sub_rev):\t" + Math.Round(submodelOutputs.Sub_rev, 0));
                        File.AppendAllText(outputFolder + "SubProblem//Subproblem_OF.txt", nCut + "_" + scl.ToString() + ": " + submodelOutputs.Sub_rev + "\n");
                    }
                    submodelOutputs = null;
                    gModel.Dispose();
                    System.GC.Collect();

                } // END OF SCEN LOOP

                Cuts.Add(SubCuts);

                DateTime start = DateTime.Now;
                Console.WriteLine("\n-> Scenarios completed in " + Math.Round((start - Time_Program_Start).TotalSeconds, 0) + "s. \n");

                DateTime end = DateTime.Now;

                //Only compute a new lb if we found a new solution for all the scenarios
                Console.WriteLine("ITERATION: \t\t" +i);
                Console.WriteLine("SOLVED SCENARIOS: \t\t" + SolvedScenarios);
                if (SolvedScenarios == num_scenarios && i > 0)
                {
                        LB = Math.Max(MeanSol - masterModelOutputs.Cost_Tot, LB);
                        Console.WriteLine("Updated LB: \t\t" + Math.Round(LB, 1));
                        Console.WriteLine("MeanSol: \t" + Math.Round(MeanSol, 1));
                }

                

                double change_in_Gap = 100000000;  // reset value 

                // if new cuts were added or we are above the termination threshold, resolve the master problem
                if (newCuts != 0 || (Math.Abs(UB - LB) > Termination_Threshold))
                {
                    // Resolve the master problem if new cuts have been added and our gap is not below the termination threshold
                    Console.WriteLine("#############################################\n");
                    Console.WriteLine("\tRE-SOLVING MASTER PROBLEM\n");

                    /*
                     * Solve Master Problem
                     */
                    gModel = new GRBModel(env);
                    gModel.Parameters.NonConvex = 2;
                    MasterModel model = new MasterModel();
                    masterModelOutputs = model.buildModel(gModel, inputData, masterModelParameters, Cuts, outputFolder, Random_Scenarios,nScenTotal,userInputs);

                    end = DateTime.Now;
                    File.AppendAllText(outputFolder + "MasterProblem//Masterproblem_OF.txt", nCut + ": " + masterModelOutputs.M_rev + "\n");
                    Console.WriteLine("Total time: " + Math.Round((end - Time_Program_Start).TotalSeconds, 0) + "s. \n");

                    // update UpperBound //(only if all subproblems were solved to optimality?)

                    UB =  Math.Min(masterModelOutputs.M_rev, UB);
                    Console.WriteLine("Updated UB: \t\t" + Math.Round(UB, 1));

                    previousGAP = 1.0 * currentGAP;
                    if (nCut > 2)
                    {
                        currentGAP = Math.Abs(UB - LB);
                        // Calculate termination criteria based on changes in the GAP
                        change_in_Gap = Math.Abs((currentGAP - previousGAP) / previousGAP);
                    }
                    else
                    {
                        currentGAP = 1000000000;
                        change_in_Gap = 100000000;
                    }

                    //Console.WriteLine("Cost_Tot: \t" + Math.Round(masterModelOutputs.Cost_Tot, 1));
                    Console.WriteLine("Updated Gap |UB-LB|: \t" + Math.Round(currentGAP, 0));

                    gModel.Dispose();
                    if (write_all_files == 1 && !masterModelOutputs.hasError)
                    {
                        string path = outputFolder + "MasterProblem//Variables//";
                        string YValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Y);
                        File.WriteAllText(path + "Y_" + nCut + ".csv", YValues);
                        string MinVarValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Min_Var);
                        File.WriteAllText(path + "MinVar_" + nCut + ".csv", MinVarValues);
                        string HireVarValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Hire);
                        File.WriteAllText(path + "Hire_" + nCut + ".csv", HireVarValues);
                        string FireValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Fire);
                        File.WriteAllText(path + "Fire_" + nCut + ".csv", FireValues);
                        string PlantValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Plant);
                        File.WriteAllText(path + "Plant_" + nCut + ".csv", PlantValues);
                        string MinStageValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Min_Stage);
                        File.WriteAllText(path + "MinStage_" + nCut + ".csv", MinStageValues);
                        string OPTValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OPT);
                        File.WriteAllText(path + "OPT_" + nCut + ".csv", OPTValues);
                        string OPLValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OPL);
                        File.WriteAllText(path + "OPL_" + nCut + ".csv", OPLValues);
                        string OPFValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OPF);
                        File.WriteAllText(path + "OPF_" + nCut + ".csv", OPFValues);
                        string OpenFValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OpenF);
                        File.WriteAllText(path + "OpenF_" + nCut + ".csv", OpenFValues);
                        string OpenDValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OpenD);
                        File.WriteAllText(path + "OpenD_" + nCut + ".csv", OpenDValues);

                        
                        //Print the last set of second stage decisions
                        string path2 = outputFolder + "SubProblem//Variables//";
                        for (int z = 0; z < inputData.ScenList.Count; z++)
                        {
                            string PACKValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Pack);
                            File.WriteAllText(path2 + "PACK_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", PACKValues);
                            string SPValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SP);
                            File.WriteAllText(path2 + "SP_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SPValues);
                            string SELValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SEL);
                            File.WriteAllText(path2 + "SEL_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SELValues);
                            string SCValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SC);
                            File.WriteAllText(path2 + "SC_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SCValues);
                            string SWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SW);
                            File.WriteAllText(path2 + "SW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SWValues);
                            string SPDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SPD);
                            File.WriteAllText(path2 + "SPD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SPDValues);
                            string SDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SD);
                            File.WriteAllText(path2 + "SD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SDValues);
                            string SPWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SPW);
                            File.WriteAllText(path2 + "SPW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SPWValues);
                            string SWDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SWD);
                            File.WriteAllText(path2 + "SWD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SWDValues);
                            string InvdValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Invd);
                            File.WriteAllText(path2 + "Invd_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", InvdValues);
                            string InvwValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Invw);
                            File.WriteAllText(path2 + "Invw_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", InvwValues);
                            string TCValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TC);
                            File.WriteAllText(path2 + "TC_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TCValues);
                            string TDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TD);
                            File.WriteAllText(path2 + "TD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TDValues);
                            string TWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TW);
                            File.WriteAllText(path2 + "TW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TWValues);
                            string TPDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TPD);
                            File.WriteAllText(path2 + "TPD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TPDValues);
                            string TPWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TPW);
                            File.WriteAllText(path2 + "TPW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TPWValues);
                            string TWDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TWD);
                            File.WriteAllText(path2 + "TWD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TWDValues);
                            string KValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].K);
                            File.WriteAllText(path2 + "K_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", KValues);
                            string ZValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Z);
                            File.WriteAllText(path2 + "Z_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", ZValues);
                            string HARVESTValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].HARVEST);
                            File.WriteAllText(path2 + "HARVEST_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", HARVESTValues);
                            string ExcessValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].EXCESS);
                            File.WriteAllText(path2 + "EXCESS_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", ExcessValues);
                        }
                    }
                    // There was an else here from Miguel's version to export results

                    

                    Console.WriteLine("---- Master Model Summary ----");
                    
                    
                   
                    //Console.WriteLine("Change in Gap: \t" + Math.Round(change_in_Gap, 1));

                    //if (nCut < 10) { change_in_Gap = 1000000; }

                    // Update value of Beta
                    if (Beta > 200)
                    {
                        Beta -= 200;
                    }

                    // And get the results stored to print in the web Interface
                    masterOutput_toPrint = masterModelOutputs;

                    // Send to the website object (for interface)
                    masterModelOutputs.Plant.ForEach(pl =>
                    {
                        plantingListResult.Add(new PlantResult()
                        {
                            week = (int)pl.WEEKP,
                            crop = pl.CROP,
                            location = pl.LOC,
                            acres = pl.Value
                        });
                    });
                    masterModelOutputs.OpenF.ForEach(fl =>
                    {
                        openfListResult.Add(new OpenFResult()
                        {
                            fac = fl.FAC,
                            open = fl.Value
                        });
                    });
                    masterModelOutputs.OpenD.ForEach(dl =>
                    {
                        opendListResult.Add(new OpenDResult()
                        {
                            dist = dl.DC,
                            open = dl.Value
                        });
                    });
                    masterModelOutputs.OPL.ForEach(opl =>
                    {
                        oplListResult.Add(new OPLResult()
                        {
                            week = (int)opl.WEEK,
                            location = opl.LOC,
                            operatorsRequired = opl.Value
                        });
                    });
                    masterModelOutputs.OPF.ForEach(opf =>
                    {
                        opfListResult.Add(new OPFResult()
                        {
                            week = (int)opf.WEEK,
                            facility = opf.FAC,
                            location = opf.LOC,
                            operatorsHoursRequired = opf.Value
                        });
                    });
                    masterModelOutputs.Hire.ForEach(hl =>
                    {
                        hireListResult.Add(new HireResult()
                        {
                            week = (int)hl.WEEK,
                            location = hl.LOC,
                            operatorsToHire = hl.Value
                        });
                    });
                    masterModelOutputs.Fire.ForEach(fl =>
                    {
                        fireListResult.Add(new FireResult()
                        {
                            week = (int)fl.WEEK,
                            location = fl.LOC,
                            operatorsToFire = fl.Value
                        });
                    });
                    masterModelOutputs.OPT.ForEach(fl =>
                    {
                        optListResult.Add(new OPTResult()
                        {
                            week = (int)fl.WEEK,
                            location = fl.LOC,
                            temporalOperatorsHired = fl.Value
                        });
                    });
                    inputData.LocList.ForEach(ll =>
                    {
                        landAvailableListResult.Add(new LandAvailableResult()
                        {
                            location = ll.LOC,
                            land = ll.LA
                        });
                    });

                    // Update Plant values to be used as "previous solution" in the next iteration
                    masterModelParameters.plantList = new List<Plant_param>();
                    inputData.WeekPList.ForEach(wpl =>
                    {
                        inputData.CropList.ForEach(cl =>
                        {
                            inputData.LocList.ForEach(ll =>
                            {
                                Plant_param plant = new Plant_param();
                                double Plant = masterModelOutputs.Plant.Find(pl =>
                                pl.WEEKP.Equals(wpl.WEEKP) &&
                                pl.CROP.Equals(cl.CROP) &&
                                pl.LOC.Equals(ll.LOC)).Value;
                                plant.crop = cl;
                                plant.weekP = wpl;
                                plant.loc = ll;
                                plant.plant = Plant;
                                masterModelParameters.plantList.Add(plant);
                            });
                        });
                    });

                    // Update packaging faciliites opened to be used as "previous solution" in the next iteration
                    masterModelParameters.facOpenList = new List<FOpen_param>();
                    inputData.FacList.ForEach(pfl =>
                    {
                        FOpen_param fac = new FOpen_param();
                        double Open = masterModelOutputs.OpenF.Find(fl =>
                        fl.FAC.Equals(pfl.FAC)).Value;
                        fac.fac = pfl;
                        fac.open = Open;// Open;
                        masterModelParameters.facOpenList.Add(fac);
                    });

                    // Update DCs opened to be used as "previous solution" in the next iteration
                    masterModelParameters.dcOpenList = new List<dcOpen_param>();
                    inputData.DistList.ForEach(dcl =>
                    {
                        dcOpen_param dist = new dcOpen_param();
                        double Open = masterModelOutputs.OpenD.Find(dl =>
                        dl.DC.Equals(dcl.DIST)).Value;
                        dist.dist = dcl;
                        dist.open = Open;// Open;
                        masterModelParameters.dcOpenList.Add(dist);
                    });

                    Console.WriteLine("Total time: \t" + Math.Round((end - Time_Program_Start).TotalSeconds, 2) + "seconds. \n");

                    Console.WriteLine("###############################################");
                }

                File.AppendAllText(outputFolder + "GAP.txt", nCut + " _ " + currentGAP + "\n");
                File.AppendAllText(outputFolder + "UB.txt", nCut + " _ " + UB + "\n");
                File.AppendAllText(outputFolder + "LB.txt", nCut + " _ " + LB + "\n");

                // If no new cuts OR
                // Max number of iterations met OR
                // UB-LB is below some threshold OR
                // if change in gap is too small then STOP
                if (newCuts == 0 || nCut == nIterations - 1 || (Math.Abs(UB - LB)) <= Termination_Threshold || UB < LB) // || change_in_Gap <= gap_change_limit
                {
                    if (newCuts == 0)
                    {
                        Console.WriteLine("\nProblem Finished: No new cuts.");
                    }
                    else if (nCut == nIterations - 1)
                    {
                        Console.WriteLine("\nProblem Finished: Max. iterations reached.");
                    }
                    else if ((Math.Abs(UB - LB)) <= Termination_Threshold)
                    {
                        Console.WriteLine("\nProblem Finished: Termination Threshold (GAP).");
                    }
                    //else if (change_in_Gap <= gap_change_limit)
                    //{
                    //    Console.WriteLine("\nProblem Finished: Change in GAP below " + gap_change_limit);
                    //}
                    else if (UB < LB)
                    {
                        Console.WriteLine("\nProblem Finished: UB < LB ");
                    }
                    else
                    {
                        Console.WriteLine("\nProblem Finished: No apparent reason.");
                    }

                    // EXPORT MASTER PROBLEM DECISION VARIABLES
                    try
                    {
                        string path = outputFolder + "MasterProblem//Variables//";
                        string YValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Y);
                        File.WriteAllText(path + "Y_" + nCut + ".csv", YValues);
                        string MinVarValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Min_Var);
                        File.WriteAllText(path + "MinVar_" + nCut + ".csv", MinVarValues);
                        string HireVarValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Hire);
                        File.WriteAllText(path + "Hire_" + nCut + ".csv", HireVarValues);
                        string FireValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Fire);
                        File.WriteAllText(path + "Fire_" + nCut + ".csv", FireValues);
                        string PlantValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Plant);
                        File.WriteAllText(path + "Plant_" + nCut + ".csv", PlantValues);
                        string MinStageValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Min_Stage);
                        File.WriteAllText(path + "MinStage_" + nCut + ".csv", MinStageValues);
                        string OPTValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OPT);
                        File.WriteAllText(path + "OPT_" + nCut + ".csv", OPTValues);
                        string OPLValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OPL);
                        File.WriteAllText(path + "OPL_" + nCut + ".csv", OPLValues);
                        string OPFValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OPF);
                        File.WriteAllText(path + "OPF_" + nCut + ".csv", OPFValues);
                        string OpenFValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OpenF);
                        File.WriteAllText(path + "OpenF_" + nCut + ".csv", OpenFValues);
                        string OpenDValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.OpenD);
                        File.WriteAllText(path + "OpenD_" + nCut + ".csv", OpenDValues);
                        string CostTotValues = ToCsv<ModelStochastic6.Models.Outputs.MasterModel.GeneralOutputs>(",", masterModelOutputs.Cost_Tot_Output);
                        File.WriteAllText(path + "CostTot_" + nCut + ".csv", CostTotValues);
                    }
                    catch { Console.WriteLine("ERROR EXPORTING MASTERPROBLEM DECISION VARIABLES."); }

                    //Print the last set of second stage decisions
                    string path2 = outputFolder + "SubProblem//Variables//";
                    try // EXPORT SUBPROBLEM DECISION VARIBLES
                    {
                        for (int z = 0; z < inputData.ScenList.Count; z++)
                        {
                            string PACKValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Pack);
                            File.WriteAllText(path2 + "PACK_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", PACKValues);
                            string SPValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SP);
                            File.WriteAllText(path2 + "SP_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SPValues);
                            string SELValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SEL);
                            File.WriteAllText(path2 + "SEL_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SELValues);
                            string SCValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SC);
                            File.WriteAllText(path2 + "SC_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SCValues);
                            string SWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SW);
                            File.WriteAllText(path2 + "SW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SWValues);
                            string SPDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SPD);
                            File.WriteAllText(path2 + "SPD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SPDValues);
                            string SDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SD);
                            File.WriteAllText(path2 + "SD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SDValues);
                            string SPWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SPW);
                            File.WriteAllText(path2 + "SPW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SPWValues);
                            string SWDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].SWD);
                            File.WriteAllText(path2 + "SWD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", SWDValues);
                            string InvdValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Invd);
                            File.WriteAllText(path2 + "Invd_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", InvdValues);
                            string InvwValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Invw);
                            File.WriteAllText(path2 + "Invw_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", InvwValues);
                            string TCValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TC);
                            File.WriteAllText(path2 + "TC_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TCValues);
                            string TDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TD);
                            File.WriteAllText(path2 + "TD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TDValues);
                            string TWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TW);
                            File.WriteAllText(path2 + "TW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TWValues);
                            string TPDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TPD);
                            File.WriteAllText(path2 + "TPD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TPDValues);
                            string TPWValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TPW);
                            File.WriteAllText(path2 + "TPW_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TPWValues);
                            string TWDValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].TWD);
                            File.WriteAllText(path2 + "TWD_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", TWDValues);
                            string KValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].K);
                            File.WriteAllText(path2 + "K_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", KValues);
                            string ZValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].Z);
                            File.WriteAllText(path2 + "Z_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", ZValues);
                            string HARVESTValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].HARVEST);
                            File.WriteAllText(path2 + "HARVEST_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", HARVESTValues);
                            string ExcessValues = ToCsv<ModelStochastic6.Models.Outputs.SubModel.GeneralOutputs>(",", subOutput_toPrint[z].EXCESS);
                            File.WriteAllText(path2 + "EXCESS_" + nCut + "_ " + Convert.ToString(z + 1) + ".csv", ExcessValues);
                        }
                    }
                    catch { Console.WriteLine("ERROR EXPORTING SUBPROBLEM DECISION VARIABLES."); }

                    break; // FINISH THE ALGORITHM
                }
                gModel.Dispose();
            } // End of iterations (cuts)
            env.Dispose();

            Console.WriteLine("\nElapsed Time to run the algorithm was {0} seconds", 1.00 * runningWatch.ElapsedMilliseconds / 1000);

            Console.WriteLine("Exporting results..");

            // Compile results to be used in the interface
            compileResults(inputData, outputFolder, subOutput_toPrint, masterOutput_toPrint, incomeCostResult, userInputs, num_scenarios);

            Console.WriteLine("Done.");
            DateTime end2 = DateTime.Now;
            Console.WriteLine("Total running time: " + Math.Round((end2 - Time_Program_Start).TotalSeconds, 2) + "seconds. \n");

            return new Model3SResponse()
            {
                listLandAvailableResult = landAvailableListResult,
                listPlantResult = plantingListResult,
                listOpenFResult = openfListResult,
                listOpenDResult = opendListResult,
                listOPFResult = opfListResult,
                listOPLResult = oplListResult,
                listHireResult = hireListResult,
                listFireResult = fireListResult,
                listOPTResult = optListResult,
                incomeCostResult = incomeCostResult,
                objToOptimize = model3SRequest,
                listVariabilityAssesmmentResult = variailityAssessmentListResult,
                typeModel = 5,
                hasError = false
            };
        }

        public static void plantParamStaticList(InputData inputData, List<Plant_param> plantList0)
        {
            IndexesVar ixVar = new IndexesVar();
            // Read initial solution
            int nWEEKP = inputData.WeekPList.Count;
            int nCROP = inputData.CropList.Count;
            int nLOC = inputData.LocList.Count;
            for (int p = 0; p < nWEEKP; p++)
            {
                for (int j = 0; j < nCROP; j++)
                {
                    for (int l = 0; l < nLOC; l++)
                    {
                        int ix3 = ixVar.getIx3(p, j, l, nWEEKP, nCROP, nLOC);

                        if (inputData.Plant0List[ix3].Plant > 0)
                        {
                            double plantValue = inputData.Plant0List[ix3].Plant;
                            plantList0[ix3].plant = plantValue;
                        }
                    }
                }
            }
        }

        public static string ToCsv<T>(string separator, IEnumerable<T> objectlist)
        {
            Type t = typeof(T);
            PropertyInfo[] fields = t.GetProperties();
            string header = String.Join(separator, fields.Select(f => f.Name).ToArray());
            StringBuilder csvdata = new StringBuilder();
            csvdata.AppendLine(header);
            try
            {
                foreach (var o in objectlist)
                    csvdata.AppendLine(ToCsvFields(separator, fields, o));
            }
            catch
            { }
            return csvdata.ToString();
        }

        public static string ToCsvFields(string separator, PropertyInfo[] fields, object o)
        {
            StringBuilder linie = new StringBuilder();
            foreach (var f in fields)
            {
                if (linie.Length > 0)
                    linie.Append(separator);
                var x = f.GetValue(o);
                if (x != null)
                    linie.Append(x.ToString());
            }
            return linie.ToString();
        }

        public static Tuple<double, double> calcValues(InputData ip, List<double> ScenConDuals, ConstInfo ScenConDualsInfo, ref Duals ScenDuals,
                                        SubModelParameters subModelParameters, MasterModelOutputs masterModelOutputs, int SolvedScenarios, int sc, int sc_index, bool shoot_for_target, UserInputs userInputs)
        {
            double Values = 0;
            double ValueCut = 0;
            IndexesVar ixVar = new IndexesVar();
            int nWeekS = ip.WeekSList.Count;
            int nProd = ip.ProdList.Count;
            int nQual = ip.QualList.Count;
            int nFac = ip.FacList.Count;
            int nCust = ip.CustList.Count;
            int nTrans = ip.TransList.Count;
            int nWeek1 = ip.Week1List.Count;
            int nDC = ip.DistList.Count;
            int nWare = ip.WareList.Count;
            int nScen = ip.ScenList.Count;
            int nCrop = ip.CropList.Count;
            int nLoc = ip.LocList.Count;
            int nWeekP = ip.WeekPList.Count;
            int nWeekH = ip.WeekHList.Count;
            DateTime startT = DateTime.Now;

            if (shoot_for_target)
            {
                double totall = 0;
                var avoid_loss = ScenConDualsInfo.Constraints.Find(c => c.name == "Avoid_Loss");
                var dll = ScenConDuals[avoid_loss.startAt];
                totall = dll * subModelParameters.Target_Rev;
                Values += totall;
                ScenDuals.Constants += totall;
            }

            for (int p = 0; p < nWeekP; p++)
            {
                for (int h = 0; h < nWeekH; h++)
                {
                    for (int j = 0; j < nCrop; j++)
                    {
                        for (int l = 0; l < nLoc; l++)
                        {
                            int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);

                            var Sum_harvest = ScenConDualsInfo.Constraints.Find(c => c.name == "Sum_harvest");
                            var dl = ScenConDuals[Sum_harvest.startAt + ix4];
                            //ScenDuals.Sum_harvest.Add(dl);    // Already added before calling the calcValues function
                            double harvest = subModelParameters.HarvestList[ix4];
                            double total = dl * harvest * -1;
                            Values += total;
                        }
                    }
                }
            }
            for (int h = 0; h < nWeekH; h++)
            {

                for (int f = 0; f < nFac; f++)
                {
                    int ix = ixVar.getIx2(h, f, nWeekH, nFac);
                    var Cap_pf = ScenConDualsInfo.Constraints.Find(c => c.name == "Cap_PF");
                    var dl = ScenConDuals[Cap_pf.startAt + ix];
                    //ScenDuals.Cap_PF.Add(dl); // Already added before calling the calcValues function
                    double CapPF = ip.FacList[f].PFcap;
                    double openf = subModelParameters.facOpenList[f].open;
                    double total = dl * CapPF * openf;
                    Values += total;
                }
            }
            for (int ixWS = 0; ixWS < ip.WeekSList.Count; ixWS++)
            {
                WEEKSModel wsl = ip.WeekSList[ixWS];
                for (int ixP = 0; ixP < ip.ProdList.Count; ixP++)
                {
                    PRODModel pl = ip.ProdList[ixP];
                    for (int ixC = 0; ixC < ip.CustList.Count; ixC++)
                    {
                        CUSTModel cl = ip.CustList[ixC];
                        int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                        double Dem2 = ip.DemList[ix].contractDem;
                        if (wsl.WEEKS <= ip.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_start")).Value || wsl.WEEKS > ip.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_end")).Value || !userInputs.useContract)  //////
                        {
                            Dem2 = 0;
                        }

                        var ContractDemand = ScenConDualsInfo.Constraints.Find(c => c.name == "ContractDemand");
                        var dl = ScenConDuals[ContractDemand.startAt + ix];
                        
                        double total = dl * Dem2 / pl.Weight;
                        Values += total;
                        ScenDuals.Constants += total;

                        var excess = ScenConDualsInfo.Constraints.Find(c => c.name == "EXCESS_DEMAND");
                        var dl2 = ScenConDuals[excess.startAt + ix];
                        Values += dl2 * Dem2 / pl.Weight;
                        ScenDuals.Constants += dl2 * Dem2 / pl.Weight;
                    }
                }
            }
            if (masterModelOutputs.Min_Stage[sc_index].Value > Values)
            {
                ValueCut = ValueCut + masterModelOutputs.Min_Stage[sc_index].Value - Values;
            }
            return new Tuple<double, double>(Values, ValueCut);
        }

        public void compileResults(InputData inputData, string outputFolder, SubModelOutputs[] subModelOutputs, MasterModelOutputs masterModelOutputs, IncomesCost3sResult incomeCostResult, UserInputs userInputs, int num_scenarios)
        {
            var csvFile = new StringBuilder();
            IndexesVar ixVar = new IndexesVar();

            int nWeekS = inputData.WeekSList.Count;
            int nProd = inputData.ProdList.Count;
            int nQual = inputData.QualList.Count;
            int nFac = inputData.FacList.Count;
            int nCust = inputData.CustList.Count;
            int nTrans = inputData.TransList.Count;
            int nWeek1 = inputData.Week1List.Count;
            int nDC = inputData.DistList.Count;
            int nWare = inputData.WareList.Count;
            int nScen = inputData.ScenList.Count;
            int nCrop = inputData.CropList.Count;
            int nLoc = inputData.LocList.Count;
            int nWeekP = inputData.WeekPList.Count;
            int nWeekH = inputData.WeekHList.Count;

            double IncomeSC = 0;
            double IncomeSD = 0;
            double IncomeSW = 0;

            double[] income_ProdScen = new double[nCrop * nScen];

            //INCOMES
            // Export Income calculation, coming from variables SC, SD, and SW. Details below:
            csvFile.Clear();
            csvFile.AppendLine("Prod,Scen,Income");
            for (int scen = 0; scen < num_scenarios; scen++)
            {
                SubModelParameters subModelParameters = new SubModelParameters();
                subModelParameters.priceList = new List<PriceCust_param>();
                for (int t = 0; t < nWeekS; t++)
                {
                    for (int k = 0; k < nProd; k++)
                    {
                        for (int c = 0; c < nCust; c++)
                        {
                            double priceS = 0;
                            if (userInputs.deterministic)
                            {
                                int ix3 = ixVar.getIx3(t, k, c, nWeekS, nProd, nCust);
                                priceS = inputData.PriceList_exp[ix3].Price;
                            }
                            else
                            {
                                int ix4 = ixVar.getIx4(t, k, c, scen, nWeekS, nProd, nCust, nScen);
                                priceS = inputData.PriceList[ix4].Price;
                            }
                            PriceCust_param pcp = new PriceCust_param()
                            {
                                weeks = inputData.WeekSList[t],
                                prod = inputData.ProdList[k],
                                cust = inputData.CustList[c],
                                price = priceS
                            };
                            subModelParameters.priceList.Add(pcp);
                        }
                    }
                }
                double lastWeekH = inputData.WeekHList.LastOrDefault().WEEKH;
                inputData.ProdList.ForEach(pl =>
                {
                    IncomeSC = 0;
                    IncomeSD = 0;
                    IncomeSW = 0;
                    double income_js = 0;
                    int ixP = inputData.ProdList.IndexOf(pl);
                    double SL = pl.SL;
                    inputData.QualList.ForEach(ql =>
                    {
                        int ixQ = inputData.QualList.IndexOf(ql);
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            double qMin = cl.Qmin;
                            inputData.WeekSList.ForEach(wsl =>
                            {
                                int ixWS = inputData.WeekSList.IndexOf(wsl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        if (wsl.WEEKS < lastWeekH && ql.QUAL <= qMin)
                                        {
                                            int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                            double price = subModelParameters.priceList[ixPrice].price;
                                            int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                            double weight = inputData.ProdList[ixP].Weight;
                                            double sc = 0;
                                            try
                                            {
                                                sc = subModelOutputs[scen].SC[ix].Value;
                                            }
                                            catch
                                            { }
                                            IncomeSC += sc * price * weight; // keep adding to the total (will be added to the CSV file below)
                                            income_js += sc * price * weight;
                                        }
                                    });
                                });
                            });
                        });
                    });

                    // ADD THE INCOME COMING FROM SD 
                    // SD IS INNACTIVE in the Subproblem this section is not needed to be run for now
                    if (false)
                    {
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                double qMin = cl.Qmin;
                                inputData.Week1List.ForEach(w1l =>
                                {
                                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            string r = inputData.TransList.FirstOrDefault().TRANS;
                                            if (w1l.WEEKS >= w1l.WEEKH && w1l.WEEKH >= (w1l.WEEKS - SL) && tl.TRANS.Equals(r) && ql.QUAL <= qMin)
                                            {
                                                WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(w1l.WEEKS));
                                                int ixWS = inputData.WeekSList.IndexOf(weekS);
                                                int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                                double price = subModelParameters.priceList[ixPrice].price;
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                                double sd = 0.0;
                                                double weight = 0.0;
                                                try
                                                {
                                                    sd = subModelOutputs[scen].SD[ix].Value;
                                                    weight = inputData.ProdList[ixP].Weight;
                                                }
                                                catch { }
                                                IncomeSD += sd * price * weight; // keep adding to the total (will be added to the CSV file below)
                                                income_js += sd * price * weight;
                                            }
                                        });
                                    });
                                });
                            });
                        });
                    }


                    // ADD THE INCOME COMING FROM SW 
                    // SW IS INNACTIVE in the Subproblem this section is not needed to be run for now
                    if (false)
                    {
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                double qMin = cl.Qmin;
                                inputData.Week1List.ForEach(w1l =>
                                {
                                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixW = inputData.WareList.IndexOf(wl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            if (w1l.WEEKS >= w1l.WEEKH && w1l.WEEKH >= (w1l.WEEKS - SL) && ql.QUAL <= qMin)
                                            {
                                                WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(w1l.WEEKS));
                                                int ixWS = inputData.WeekSList.IndexOf(weekS);
                                                int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                                double price = subModelParameters.priceList[ixPrice].price;
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                                double sw = 0;
                                                double weight = 0;
                                                try
                                                {
                                                    sw = subModelOutputs[scen].SW[ix].Value;
                                                    weight = inputData.ProdList[ixP].Weight;
                                                }
                                                catch { }
                                                IncomeSW += sw * price * weight;
                                                income_js += sw * price * weight;
                                            }
                                        });
                                    });
                                });
                            });
                        });
                    }

                    int idx2 = ixVar.getIx2(ixP, scen, nCrop, nScen);
                    income_ProdScen[idx2] = income_js;

                    // Add to the Excel file after the calculations are done
                    csvFile.AppendLine($"{pl.PROD},{scen},{IncomeSC + IncomeSD + IncomeSW}");

                    //ADD OBJECT STRUCTURES TO WEBSITE
                    incomeCostResult.listIncomeProdScenResult.Add(new Incomes3SProdScen()
                    {
                        prod = pl.PROD,
                        scen = scen,
                        income = income_js
                    });
                });
            }
            // Store the Excel file with the Income Data
            File.WriteAllText(outputFolder + "Income.csv", csvFile.ToString());

            // LAND USE
            csvFile.Clear();
            csvFile.AppendLine("Loc,Crop,LandAllocated");
            double[] land_Allocation = new double[nLoc * nCrop];
            for (int l = 0; l < nLoc; l++)
            {
                Incomes3SLandAllocation la = new Incomes3SLandAllocation();
                la.loc = inputData.LocList[l].LOC;
                double plant_l = 0;
                for (int j = 0; j < nCrop; j++)
                {
                    double plant_lj = 0;
                    for (int p = 0; p < nWeekP; p++)
                    {
                        int ix3 = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                        try
                        {
                            plant_l += masterModelOutputs.Plant[ix3].Value;
                            plant_lj += masterModelOutputs.Plant[ix3].Value;
                        }
                        catch { }
                    }
                    int ix2 = ixVar.getIx2(l, j, nLoc, nCrop);
                    land_Allocation[ix2] = plant_lj;
                    csvFile.AppendLine($"{la.loc},{inputData.CropList[j].CROP},{plant_lj}");
                }
                la.land = plant_l;
                incomeCostResult.listIncomeLandAllocationResult.Add(la);
            }
            File.WriteAllText(outputFolder + "LandAllocated.csv", csvFile.ToString());

            //PLANTING COSTS
            csvFile.Clear();
            csvFile.AppendLine("Crop,Loc,CostPlant");
            double plantingCost = 0;
            inputData.CropList.ForEach(cl =>
            {
                int ixC = inputData.CropList.IndexOf(cl);
                inputData.LocList.ForEach(ll =>
                {
                    int ixL = inputData.LocList.IndexOf(ll);
                    plantingCost = 0;
                    int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                    double Cplant = inputData.CropBudgetList[ix2].Cplant;
                    inputData.WeekPList.ForEach(wpl =>
                    {
                        int ixWP = inputData.WeekPList.IndexOf(wpl);
                        int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                        double plant = 0;
                        try
                        {
                            plant = masterModelOutputs.Plant[ix].Value;
                        }
                        catch { }
                        plantingCost += plant * Cplant;
                    });
                    // Add to the Excel file after the calculations are done
                    csvFile.AppendLine($"{cl.CROP},{ll.LOC},{plantingCost}");
                    incomeCostResult.listCostPlantResult.Add(new CostPlant3SResult()
                    {
                        crop = cl.CROP,
                        location = ll.LOC,
                        costPlant = plantingCost
                    });
                });
            });
            // Store the Excel file with the CostPlant Data
            File.WriteAllText(outputFolder + "CostPlant.csv", csvFile.ToString());

            //LOGISTIC COSTS      
            csvFile.Clear();
            csvFile.AppendLine("Prod,Scen,CostLogistic");
            double logisticCostPACK = 0;
            double logisticCostInvw = 0;
            double logisticCostInvd = 0;
            for (int scen = 0; scen < num_scenarios; scen++)
            {
                inputData.ProdList.ForEach(pl =>
                {
                    int ixP = inputData.ProdList.IndexOf(pl);
                    double Ccase = pl.Ccase;
                    double LabF = pl.LabF;
                    double tCL = Ccase + LabF;
                    double Pallet = pl.Pallet;
                    logisticCostPACK = 0; // restart to 0
                    logisticCostInvw = 0; // restart to 0
                    logisticCostInvd = 0; // restart to 0
                    inputData.FacList.ForEach(fl =>
                    {
                        int ixF = inputData.FacList.IndexOf(fl);
                        inputData.WeekHList.ForEach(whl =>
                        {
                            int ixWH = inputData.WeekHList.IndexOf(whl);
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                try
                                {
                                    logisticCostPACK += (subModelOutputs[scen].Pack[ix].Value * tCL);
                                }
                                catch { }
                            });
                        });
                    });
                    inputData.Week1List.ForEach(w1l =>
                    {
                        int ixW1 = inputData.Week1List.IndexOf(w1l);
                        double ws = w1l.WEEKS;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                double Chw = inputData.ChwList.Find(chwl =>
                                chwl.WARE.Equals(wl.WARE) &&
                                chwl.PROD.Equals(pl.PROD)).Chw;
                                double totalPCHW = Chw / Pallet;
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                try
                                {
                                    logisticCostInvw += (subModelOutputs[scen].Invw[ix].Value * totalPCHW);
                                }
                                catch { }
                            });
                        });
                    });
                    inputData.Week1List.ForEach(w1l =>
                    {
                        int ixW1 = inputData.Week1List.IndexOf(w1l);
                        double ws = w1l.WEEKS;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                double Chd = inputData.ChdList.Find(chdl =>
                                chdl.DC.Equals(dl.DIST) &&
                                chdl.PROD.Equals(pl.PROD)).Chd;
                                double totalPCHD = Chd / Pallet;
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                try
                                {
                                    logisticCostInvd += (subModelOutputs[scen].Invd[ix].Value * totalPCHD);
                                }
                                catch { }
                            });
                        });
                    });
                    // Add to the Excel file after the calculations are done
                    csvFile.AppendLine($"{pl.PROD},{scen},{logisticCostPACK + logisticCostInvw + logisticCostInvd}");
                    //ADD OBJECT STRUCTURES TO WEBSITE
                    incomeCostResult.listCostLogResult.Add(new CostLog3SResult()
                    {
                        prod = pl.PROD,
                        scen = scen,
                        costLog = logisticCostPACK + logisticCostInvw + logisticCostInvd
                    });
                });
            }
            File.WriteAllText(outputFolder + "CostLogistic.csv", csvFile.ToString());


            //TRANSPORTATION COSTS
            csvFile.Clear();
            csvFile.AppendLine("Prod,Scen,CostTransportation");
            double transportationCostSCCT = 0;
            double transportationCostSWCTW = 0;
            double transportationCostSDCTD = 0;
            double transportationCostSPWCTPW = 0;
            double transportationCostSPDCTPD = 0;
            double transportationCostSWDCTWD = 0;
            for (int scen = 0; scen < num_scenarios; scen++)
            {
                inputData.ProdList.ForEach(pl =>
                {
                    int ixP = inputData.ProdList.IndexOf(pl);
                    transportationCostSCCT = 0; // restart to 0
                    transportationCostSWCTW = 0; // restart to 0
                    transportationCostSDCTD = 0; // restart to 0
                    transportationCostSPWCTPW = 0; // restart to 0
                    transportationCostSPDCTPD = 0; // restart to 0
                    transportationCostSWDCTWD = 0; // restart to 0
                    inputData.WeekSList.ForEach(wsl =>
                    {
                        int ixWS = inputData.WeekSList.IndexOf(wsl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.CustList.ForEach(cl =>
                                {
                                    int ixC = inputData.CustList.IndexOf(cl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int id3 = ixVar.getIx3(ixF, ixC, ixT, nFac, nCust, nTrans);
                                        double CT = inputData.CtList[id3].CT;
                                        int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                        try
                                        {
                                            transportationCostSCCT += (subModelOutputs[scen].SC[ix].Value * CT);
                                        }
                                        catch { }
                                    });
                                });
                            });
                        });
                    });
                    // SW INNACTIVE in subproblem
                    if (false)
                    {
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            double ws = w1l.WEEKS;
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int id3 = ixVar.getIx3(ixW, ixC, ixT, nWare, nCust, nTrans);
                                            double CTW = inputData.CtwList[id3].CTW;
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                            try
                                            {
                                                transportationCostSWCTW += (subModelOutputs[scen].SW[ix].Value * CTW);
                                            }
                                            catch { }
                                        });
                                    });
                                });
                            });
                        });
                    }
                    // SD innactive in subproblem
                    if (false)
                    {
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            double ws = w1l.WEEKS;
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            string r = inputData.TransList.FirstOrDefault().TRANS;
                                            if (tl.TRANS.Equals(r))
                                            {
                                                int id3 = ixVar.getIx2(ixD, ixC, nDC, nCust);
                                                double CTD = inputData.CtdList[id3].CTD;
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                                try
                                                {
                                                    transportationCostSDCTD += (subModelOutputs[scen].SD[ix].Value * CTD);
                                                }
                                                catch { }
                                            }
                                        });
                                    });
                                });
                            });
                        });
                    }

                    inputData.Week1List.ForEach(w1l =>
                    {
                        int ixW1 = inputData.Week1List.IndexOf(w1l);
                        double ws = w1l.WEEKS;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        double CTPW = inputData.CtpwList.Find(ctpwl =>
                                        ctpwl.FAC.Equals(fl.FAC) &&
                                        ctpwl.WARE.Equals(wl.WARE) &&
                                        ctpwl.TRANS.Equals(tl.TRANS)).CTPW;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                        try
                                        {
                                            transportationCostSPWCTPW += (subModelOutputs[scen].SPW[ix].Value * CTPW);
                                        }
                                        catch { }
                                    });
                                });
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        double CTPD = inputData.CtpdList.Find(ctpdl =>
                                        ctpdl.FAC.Equals(fl.FAC) &&
                                        ctpdl.DC.Equals(dl.DIST) &&
                                        ctpdl.TRANS.Equals(tl.TRANS)).CTPD;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        try
                                        {
                                            transportationCostSPDCTPD += (subModelOutputs[scen].SPD[ix].Value * CTPD);
                                        }
                                        catch { }
                                    });
                                });
                            });
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        double CTWD = inputData.CtwdList.Find(ctwdl =>
                                        ctwdl.WARE.Equals(wl.WARE) &&
                                        ctwdl.DC.Equals(dl.DIST) &&
                                        ctwdl.TRANS.Equals(tl.TRANS)).CTWD;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        try
                                        {
                                            transportationCostSWDCTWD += (subModelOutputs[scen].SWD[ix].Value * CTWD);
                                        }
                                        catch { }
                                    });
                                });
                            });
                        });
                    });

                    // Add to the Excel file after the calculations are done
                    csvFile.AppendLine($"{pl.PROD},{scen},{transportationCostSCCT + transportationCostSWCTW + transportationCostSDCTD + transportationCostSPWCTPW + transportationCostSPDCTPD + transportationCostSWDCTWD}");
                    //ADD OBJECT STRUCTURES TO WEBSITE
                    incomeCostResult.listCostTransResult.Add(new CostTrans3SResult()
                    {
                        prod = pl.PROD,
                        scen = scen,
                        costTrans = transportationCostSCCT + transportationCostSWCTW + transportationCostSDCTD + transportationCostSPWCTPW + transportationCostSPDCTPD + transportationCostSWDCTWD
                    });
                });
            }
            File.WriteAllText(outputFolder + "CostTransportation.csv", csvFile.ToString());
        }


        public static void cleanFiles(string outputFolder)
        {
            List<string> foldersList = new List<string>();
            foldersList.Add(outputFolder + "SubProblem//Variables//");
            foldersList.Add(outputFolder + "SubProblem//Models//");
            foldersList.Add(outputFolder + "SubProblem//Duals//");
            foldersList.Add(outputFolder + "MasterProblem//Variables//");
            foldersList.Add(outputFolder + "MasterProblem//Models//");
            foreach (string fold_ in foldersList)
            {
                DirectoryInfo di = new DirectoryInfo(fold_);
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            if (File.Exists(outputFolder + "MasterProblem//Masterproblem_OF.txt"))
            {
                File.Delete(outputFolder + "MasterProblem//Masterproblem_OF.txt");
                File.AppendAllText(outputFolder + "MasterProblem//Masterproblem_OF.txt", "");
            }
            if (File.Exists(outputFolder + "SubProblem//Subproblem_OF.txt"))
            {
                File.Delete(outputFolder + "SubProblem//Subproblem_OF.txt");
                File.AppendAllText(outputFolder + "SubProblem//Subproblem_OF.txt", "");
            }
            if (File.Exists(outputFolder + "GAP.txt"))
            {
                File.Delete(outputFolder + "GAP.txt");
                File.AppendAllText(outputFolder + "GAP.txt", "");
            }
            if (File.Exists(outputFolder + "UB.txt"))
            {
                File.Delete(outputFolder + "UB.txt");
                File.AppendAllText(outputFolder + "UB.txt", "");
            }
            if (File.Exists(outputFolder + "LB.txt"))
            {
                File.Delete(outputFolder + "LB.txt");
                File.AppendAllText(outputFolder + "LB.txt", "");
            }
        }
    }

    public struct SubCut { public double CutsMods; public Duals duals; }

    public struct Duals
    {
        public List<double> Sum_harvest; public List<double> Init_FAC; public List<double> ContractDemand; public List<double> Production; public List<double> Tot_packaging; public List<double> Tot_scrap;
        public List<double> Initial_hold; public List<double> Inventw; public List<double> Initial_DC; public List<double> Inv_DC; public List<double> SC_Prod; public List<double> SPD_Prod;
        public List<double> SWD_Prod; public List<double> SD_Inv; public List<double> SPD_Inv; public List<double> Cap_PF; public double Constants;
    }

    public struct ConstInfo { public List<Constraint> Constraints; }
}
