using Gurobi;
using ModelStochastic6.Models;
using ModelStochastic6.Models.MasterModelParams;
using ModelStochastic6.Models.Outputs;
using ModelStochastic6.Models.Outputs.MasterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ModelStochastic6
{
    class MasterModel
    {
        public MasterModelOutputs buildModel(
            GRBModel gModel,
            InputData inputData,
            MasterModelParameters masterModelParameters,
            List<List<SubCut>> Cuts,
            string outputFolder,
            List<int> Random_Scenarios,
            int total_available_scenarios,
            UserInputs userInputs
            )
        {
            MasterModelOutputs masterModelOutputs = new MasterModelOutputs();
            try
            {
                bool deterministic = userInputs.deterministic;
                bool activate_target = userInputs.activate_target;

                //GUROBI EXPR
                GRBLinExpr expr1 = 0.0;
                GRBLinExpr expr2 = 0.0;
                GRBLinExpr expr3 = 0.0;
                GRBLinExpr expr4 = 0.0;
                GRBQuadExpr expr1Quad = 0.00;

                //GLOBAL VARS
                IndexesVar ixVar = new IndexesVar();
                double Target = masterModelParameters.Target;

                double Clabor = inputData.MList[0].Clabor;
                double Chire = inputData.MList[0].Chire;
                double Ctemp = inputData.MList[0].Ctemp;
                double lambda = 1;
                double W = inputData.MList[0].W;
                double M = inputData.MList[0].M;
                double MFix = inputData.MList[0].MFix;
                double MTemp = inputData.MList[0].MTemp;

                //Length of lists
                int nWeekS = inputData.WeekSList.Count;
                int nProd = inputData.ProdList.Count;
                int nQual = inputData.QualList.Count;
                int nFac = inputData.FacList.Count;
                int nCust = inputData.CustList.Count;
                int nTrans = inputData.TransList.Count;
                int nDC = inputData.DistList.Count;
                int nWare = inputData.WareList.Count;
                int nScen = Random_Scenarios.Count;
                int nScenTotal = total_available_scenarios;
                int nCrop = inputData.CropList.Count;
                int nLoc = inputData.LocList.Count;
                int nWeekP = inputData.WeekPList.Count;
                int nWeekH = inputData.WeekHList.Count;
                int nWeek = inputData.WeekList.Count;

                double[] harvSol = new double[nWeekP * nWeekH * nCrop * nLoc];

                //GUROBI VARS
                // Whether or not to open each packaging facility
                GRBVar[] OpenF = gModel.AddVars(nFac, GRB.BINARY);
                inputData.FacList.ForEach(fac =>
                {
                    int ixF = inputData.FacList.IndexOf(fac);
                    OpenF[ixF].VarName = "OpenF[" + fac.FAC + "]";
                });

                // Whether or not to open distribution center
                GRBVar[] OpenD = gModel.AddVars(nDC, GRB.BINARY);
                inputData.DistList.ForEach(DC =>
                {
                    int ixDC = inputData.DistList.IndexOf(DC);
                    OpenD[ixDC].VarName = "OpenD[" + DC.DIST + "]";
                });

                GRBVar[] Y = gModel.AddVars(nWeekP * nCrop * nLoc, GRB.BINARY);
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            Y[ix].VarName = "Y[" + wpl.WEEKP + "," + cl.CROP + "," + ll.LOC + "]";
                        });
                    });
                });

                GRBVar[] I = gModel.AddVars(nCrop * nLoc, GRB.BINARY);
                inputData.CropList.ForEach(cl =>
                {
                    int ixC = inputData.CropList.IndexOf(cl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixC, ixL, nCrop, nLoc);

                        I[ix].VarName = "I[" + cl.CROP + "," + ll.LOC + "]";
                    });
                });

                GRBVar Cost_Tot = gModel.AddVar(0, double.MaxValue, 0, GRB.CONTINUOUS, "Cost_Tot");

                GRBVar[] Hire = gModel.AddVars(nWeek * nLoc, GRB.CONTINUOUS);
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);

                        Hire[ix].VarName = "Hire[" + wl.WEEK + "," + ll.LOC + "]";
                    });
                });

                GRBVar[] Fire = gModel.AddVars(nWeek * nLoc, GRB.CONTINUOUS);
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                        Fire[ix].VarName = "Fire[" + wl.WEEK + "," + ll.LOC + "]";
                    });
                });

                GRBVar[] Plant = gModel.AddVars(nWeekP * nCrop * nLoc, GRB.CONTINUOUS);
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            Plant[ix].VarName = "Plant[" + wpl.WEEKP + "," + cl.CROP + "," + ll.LOC + "]";
                        });
                    });
                });

                double[] lb = new double[nScen];
                for (int i = 0; i < lb.Length; i++) { lb[i] = -1 * GRB.INFINITY; }
                GRBVar[] Min_Stage = gModel.AddVars(lb, null, null, null, null); //2
                Random_Scenarios.ForEach(sl =>
                {
                    int ixM = Random_Scenarios.IndexOf(sl);
                    Min_Stage[ixM].VarName = "Min_Stage[" + sl.ToString() + "]";
                });

                GRBVar[] Min_Var = gModel.AddVars(lb, null, null, null, null); // 
                Random_Scenarios.ForEach(sl =>
                {
                    int ixM = Random_Scenarios.IndexOf(sl);

                    Min_Var[ixM].VarName = "Min_Var[" + sl.ToString() + "]";

                });

                GRBVar[] OPT = gModel.AddVars(nWeek * nLoc, GRB.CONTINUOUS);
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                        OPT[ix].VarName = "OPT[" + wl.WEEK + "," + ll.LOC + "]";
                    });
                });

                GRBVar[] OPL = gModel.AddVars(nWeek * nLoc, GRB.CONTINUOUS);
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                        OPL[ix].VarName = "OPL[" + wl.WEEK + "," + ll.LOC + "]";
                    });
                });

                GRBVar[] OPF = gModel.AddVars(nWeek * nFac, GRB.CONTINUOUS);
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.FacList.ForEach(fl =>
                    {
                        int ixF = inputData.FacList.IndexOf(fl);
                        int ix = ixVar.getIx2(ixW, ixF, nWeek, nFac);
                        OPF[ix].VarName = "OPF[" + wl.WEEK + "," + fl.FAC + "]";
                    });
                });

                // Objective Function
                expr1.Clear();
                expr2.Clear();
                expr3.Clear();

                //maximize M_rev: 
                //sum{s in SCEN} Min_Stage[s]*Prob[s]
                Random_Scenarios.ForEach(scl =>
                {
                    int ixS = Random_Scenarios.IndexOf(scl);
                    expr1.AddTerm(inputData.ScenList[ixS].Prob, Min_Stage[ixS]);
                });
                //-sum{t in WEEK,f in FAC} OPF[t,f]*Clabor 
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.FacList.ForEach(fl =>
                    {
                        int ixF = inputData.FacList.IndexOf(fl);
                        int ix = ixVar.getIx2(ixW, ixF, nWeek, nFac);
                        expr1.AddTerm(-1 * Clabor, OPF[ix]);
                    });
                });
                //-sum{t in WEEK,l in LOC} OPL[t,l]*Clabor
                //-sum{t in WEEK,l in LOC} HIRE[t,l]*Chire
                //-sum{t in WEEK,l in LOC} OPT[t,l]*Ctemp
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                        expr1.AddTerm(-1 * Clabor, OPL[ix]);
                        expr1.AddTerm(-1 * Chire, Hire[ix]);
                        expr1.AddTerm(-1 * Chire, Fire[ix]);
                        expr1.AddTerm(-1 * Ctemp, OPT[ix]);
                    });
                });

                //costs of opening packaging facilities
                inputData.FacList.ForEach(fl =>
                {
                    int ixF = inputData.FacList.IndexOf(fl);
                    double opening_cost = fl.OpenFCost;
                    expr1.AddTerm(-1 * opening_cost, OpenF[ixF]);
                });

                //costs of opening DCs
                inputData.DistList.ForEach(DCl =>
                {
                    int ixD = inputData.DistList.IndexOf(DCl);
                    double opening_cost = DCl.OpenDCost;
                    expr1.AddTerm(-1 * opening_cost, OpenD[ixD]);
                });

                //-sum {p in WEEKP,j in CROP,l in LOC} Plant[p,j,l]*(Cplant[j]+LabP[j]+LabH[j])
                //-sum {p in WEEKP,j in CROP,l in LOC} Beta*(Plant[p,j,l]-plant[p,j,l])^2
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);

                            double CPLL = inputData.CropBudgetList[ix2].Cplant + inputData.CropBudgetList[ix2].LabP + inputData.CropBudgetList[ix2].LabH; // HERE
                            expr1.AddTerm(-1 * CPLL, Plant[ix]);

                            double Beta = masterModelParameters.Beta;
                            double plant = masterModelParameters.plantList[ix].plant;

                            //expr1Quad.AddTerm(1, Plant[ix]);
                            //expr1.AddTerm(2 * plant, Plant[ix]);
                            //expr1Quad = expr1Quad - 1 * Beta * ((Plant[ix] * Plant[ix]) - (2 * Plant[ix] * plant) + (plant * plant));
                        });
                    });
                });

                //+sum{s in SCEN} lambda*Min_Var[s]*Prob[s] 
                Random_Scenarios.ForEach(scl =>
                {
                    int ixS = Random_Scenarios.IndexOf(scl);
                    expr1.AddTerm(lambda * inputData.ScenList[ixS].Prob, Min_Var[ixS]);
                });

                //-lambda * Target;
                expr4 = (-1 * lambda * Target);
                
                //Insert objective function
                gModel.SetObjective(expr1 + expr4, GRB.MAXIMIZE);
                expr1.Clear();
                expr2.Clear();
                expr3.Clear();
                expr4.Clear();
                expr1Quad.Clear();
                expr1Quad.Clear();

                //Tie in inclusion of crops at a location to decisions of wehter or not to include a crop
                inputData.WeekPList.ForEach(wp =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wp);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix1 = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                            expr1.AddTerm(1, Y[ix1]);
                            expr2.AddTerm(1, I[ix2]);
                            gModel.AddConstr(expr1, GRB.LESS_EQUAL, expr2, "subject to Include[" + wp.WEEKP + "," + cl.CROP + "," + ll.LOC + "]");
                            expr1.Clear();
                            expr2.Clear();
                        });
                    });
                });

                //# The amount to plant cannot be more than the land you have
                //subject to Tot_land { l in LOC}: 
                //sum { p in WEEKP,j in CROP}
                //Plant[p, j, l] <= LA[l];
                inputData.LocList.ForEach(ll =>
                {
                    int ixL = inputData.LocList.IndexOf(ll);
                    double LA = ll.LA;
                    inputData.WeekPList.ForEach(wpl =>
                    {
                        int ixWP = inputData.WeekPList.IndexOf(wpl);
                        inputData.CropList.ForEach(cl =>
                        {
                            int ixC = inputData.CropList.IndexOf(cl);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            expr1.AddTerm(1, Plant[ix]);
                        });
                    });
                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, LA, "subject to Tot_land[" + ll.LOC + "]");
                    expr1.Clear();
                });

                //# The amount of water cannot exceed the water you have
                //subject to Tot_water: 
                //sum { p in WEEKP,l in LOC, j in CROP}
                //Plant[p, j, l] * water[j] <= W;
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        inputData.CropList.ForEach(cl =>
                        {
                            int ixC = inputData.CropList.IndexOf(cl);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                            double water = inputData.CropBudgetList[ix2].water;
                            expr1.AddTerm(water, Plant[ix]);
                        });
                    });
                });
                gModel.AddConstr(expr1, GRB.LESS_EQUAL, W, "subject to Tot_water");
                expr1.Clear();

                //# The planting cost cannot exceed the money you have
                //subject to Tot_invest:
                //sum { p in WEEKP, j in CROP, l in LOC}
                //Plant[p, j, l] * Cplant[j] <=M;
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                            double Cplant = inputData.CropBudgetList[ix2].Cplant;
                            expr1.AddTerm(Cplant, Plant[ix]);
                        });
                    });
                });
                gModel.AddConstr(expr1, GRB.LESS_EQUAL, M, "subject to Tot_invest");
                expr1.Clear();

                //# If a crop is planted you must plant a minimum amount 
                //subject to Minimum 
                //{ j in CROP, l in LOC, p in WEEKP}:
                //Plant[p, j, l] >= Y[j, p] * minl[j];
                inputData.CropList.ForEach(cl =>
                {
                    int ixC = inputData.CropList.IndexOf(cl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                        double minl = inputData.CropBudgetList[ix2].minl;
                        inputData.WeekPList.ForEach(wpl =>
                        {
                            int ixWP = inputData.WeekPList.IndexOf(wpl);
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                            expr1.AddTerm(1, Plant[ix]);
                            expr2.AddTerm(minl, Y[ix]);
                            gModel.AddConstr(expr1 - expr2, GRB.GREATER_EQUAL, 0, "MinLand[" + cl.CROP + ", " + ll.LOC + ", " + wpl.WEEKP + "]");
                            expr1.Clear();
                            expr2.Clear();
                        });
                    });
                });

                //subject to Maximum {p in WEEKP,j in CROP,l in LOC}: Plant[p,j,l] <= Y[j,p]*maxl[j];
                inputData.WeekPList.ForEach(wp =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wp);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);

                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                            double maxl = inputData.CropBudgetList[ix2].maxl;
                            int ix1 = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            expr1.AddTerm(1, Plant[ix1]);
                            expr2.AddTerm(maxl, Y[ix1]);
                            gModel.AddConstr(expr1 - expr2, GRB.LESS_EQUAL, 0, "MaxLand[" + wp.WEEKP + "," + cl.CROP + "," + ll.LOC + "]");
                            expr1.Clear();
                            expr2.Clear();
                        });
                    });
                });

                //subject to Max_week {p in WEEKP}: sum {l in LOC,j in CROP} Plant [p,j,l]<=Maxi;
                for (int p = 0; p < nWeekP; p++)
                {
                    expr1.Clear();
                    for (int l = 0; l < nLoc; l++)
                    {
                        for (int j = 0; j < nCrop; j++)
                        {
                            int ix3 = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                            expr1.AddTerm(1, Plant[ix3]);
                        }
                    }
                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, inputData.MList[0].Maxi, "Max_week[" + inputData.WeekPList[p].WEEKP + "]");
                    expr1.Clear();
                }

                //subject to Lab_Fields {t in WEEK,l in LOC}:
                // OPL[t,l]+OPT[t,l]  >=
                //    sum{(p,h,j):h=t} (Plant[p,j,l]*LaborP[p,h,j])
                //    +sum {(p,h,j,l), s in SCEN :h=t} (Prob[s]*Harvest[p,h,j,l,s]*(1-Salv[p,h,j,l])*LaborH[p,h,j] + Plant[p,j,l]*3.5/Dharv[j]);                  
                inputData.WeekList.ForEach(wl =>
                {
                    int ixWL = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);

                        expr1.AddTerm(1, OPL[ix]);
                        expr1.AddTerm(1, OPT[ix]);

                        //sum{(p,h,j):h=t} (Plant[p,j,l]*LaborP[p,h,j])
                        for (int p = 0; p < nWeekP; p++)
                        {
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    if (h == ixWL) // (inputData.WeekHList[h].WEEKH == inputData.WeekList[ixWL].WEEK) LABOR CHECK
                                    {
                                        int ix3 = ixVar.getIx3(p, h, j, nWeekP, nWeekH, nCrop);
                                        int ix2 = ixVar.getIx2(j, ixL, nCrop, nLoc);
                                        //double LaborP = inputData.CropBudgetList[ix2].LaborP;          //CHECK
                                        double LaborP = inputData.LaborList[ix3].LaborP;


                                        int ix3P = ixVar.getIx3(p, j, ixL, nWeekP, nCrop, nLoc);
                                        expr2.AddTerm(LaborP, Plant[ix3P]);                                                                                                                                                                                                                                                                                                         
                                    }
                                }
                            }
                        }

                        //sum {(p,h,j,l), s in SCEN :h=t} Prob[s]*(Harvest[p,h,j,l,s]*(1-Salv[p,h,j,l])*LaborH[p,h,j] + Plant[p,j,l]*3.5/Dharv[j]);      
                        for (int p = 0; p < nWeekP; p++)
                        {
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    //if (inputData.WeekHList[h].WEEKH == inputData.WeekList[ixWL].WEEK) LABOR CHECK
                                    //{
                                        for (int l = 0; l < nLoc; l++)
                                        {
                                            if(h == ixWL)
                                            {
                                                Random_Scenarios.ForEach(scl =>
                                                {
                                                    int ix3 = ixVar.getIx3(p, h, j, nWeekP, nWeekH, nCrop);
                                                    int ixS = Random_Scenarios.IndexOf(scl);
                                                    int ix2 = ixVar.getIx2(j, ixL, nCrop, nLoc);
                                                    //double LaborH = inputData.CropBudgetList[ix2].LaborH;          //CHECK
                                                    double LaborH = inputData.LaborList[ix3].LaborH;

                                                    int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                                    int ix5 = ixVar.getIx5(p, h, j, l, ixS, nWeekP, nWeekH, nCrop, nLoc, nScen);
                                                    double Salv = 0;
                                                    double prob = inputData.ScenList[ixS].Prob;
                                                    double total = prob * masterModelParameters.MasterHarvestList[ix5] * (1 - Salv) * LaborH;

                                                    expr2.AddConstant(total);

                                                    if(total>0)
                                                    {
                                                        double Dharv = inputData.CropList[j].Dharv;
                                                        int ix3P = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                                                        expr2.AddTerm((3.5 / Dharv) * (prob), Plant[ix3P]);
                                                    }
                                                    

                                                    
                                                });
                                            }
                                        }
                                    //}
                                }
                            }
                        }
                        gModel.AddConstr(expr1, GRB.GREATER_EQUAL, expr2, "Lab_Fields[" + wl.WEEK + "," + ll.LOC + "]");
                        expr1.Clear();
                        expr2.Clear();
                    });
                });

                //subject to Hire_init {t in WEEK,l in LOC:t =first(WEEK)}: HIRE[t,l]=OPL[t,l];
                WEEKModel firstW = inputData.WeekList[0];
                inputData.WeekList.ForEach(wl =>
                {
                    int ixWL = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        if (wl.Equals(firstW))
                        {
                            int ix = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);
                            expr1.AddTerm(1, Hire[ix]);
                            expr2.AddTerm(1, OPL[ix]);
                            gModel.AddConstr(expr1, GRB.EQUAL, expr2, "Hire_init[" + wl.WEEK + "," + ll.LOC + "]");
                            expr1.Clear();
                            expr2.Clear();
                        }
                    });
                });

                //subject to Hire_Labor { t in WEEK,l in LOC: 16 > t > first(WEEK)}:
                //        HIRE[t, l] - FIRE[t, l] = OPL[t, l] - OPL[t - 1, l];
                inputData.WeekList.ForEach(wl =>
                {
                    int ixWL = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        double magicNumber = 16.0;
                        if ( wl.WEEK > firstW.WEEK) //  if (magicNumber > wl.WEEK && wl.WEEK > firstW.WEEK) LABOR CHECK
                        {
                            int ix = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);
                            expr1.AddTerm(1, Hire[ix]);
                            expr1.AddTerm(-1, Fire[ix]);
                            int ix2 = ixVar.getIx2(ixWL - 1, ixL, nWeek, nLoc);
                            expr2.AddTerm(1, OPL[ix]);
                            expr2.AddTerm(-1, OPL[ix2]);
                            gModel.AddConstr(expr1, GRB.EQUAL, expr2, "Hire_Labor[" + wl.WEEK + "," + ll.LOC + "]");
                            expr1.Clear();
                            expr2.Clear();
                        }
                    });
                });


                /* LABOR CHECK
                //subject to Hire_LabF {l in LOC,t in WEEK: t>=16}:
                //    HIRE[16,l]+ OPL[15,l]>=  OPL[t,l];
                inputData.LocList.ForEach(ll =>
                {
                    int ixL = inputData.LocList.IndexOf(ll);
                    int magicNumber1 = 15;
                    int magicNumber2 = 14;
                    inputData.WeekList.ForEach(wl =>
                    {
                        int ixWL = inputData.WeekList.IndexOf(wl);
                        if (wl.WEEK >= 16)
                        {
                            int ix1 = ixVar.getIx2(magicNumber1, ixL, nWeek, nLoc);
                            int ix2 = ixVar.getIx2(magicNumber2, ixL, nWeek, nLoc);
                            expr1.AddTerm(1, Hire[ix1]);
                            expr1.AddTerm(1, OPL[ix2]);
                            int ix3 = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);
                            expr2.AddTerm(1, OPL[ix3]);
                            gModel.AddConstr(expr1, GRB.GREATER_EQUAL, expr2, "Hire_LabF[" + ll.LOC + "," + wl.WEEK + "]");
                            expr1.Clear();
                            expr2.Clear();
                        }
                    });
                });
                */

                //subject to Temporal {t in WEEK}:
                //    sum {l in LOC} OPT[t,l]<= MTemp;               
                inputData.WeekList.ForEach(wl =>
                {
                    int ixWL = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);
                        expr1.AddTerm(1, OPT[ix]);
                    });
                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, MTemp, "Temporal[" + wl.WEEK + "]");
                    expr1.Clear();
                });

                //subject to Fixed_lab: sum { t in WEEK,l in LOC}
                //     HIRE[t, l] <= MFix;                
                inputData.WeekList.ForEach(wl =>
                {
                    int ixWL = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);
                        expr1.AddTerm(1, Hire[ix]);
                    });
                });
                gModel.AddConstr(expr1, GRB.LESS_EQUAL, MFix, "Fixed_lab");
                expr1.Clear();

                //subject to Fire_Labor {t in WEEK,l in LOC:t>16}:
                //         OPL[t,l]-OPL[t-1,l]+FIRE[t,l]>=0;
                inputData.WeekList.ForEach(wl =>
                {
                    int ixWL = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int magicNumber = 16;
                        if(wl.WEEK>1) //if (wl.WEEK > magicNumber) LABOR CHECK
                        {
                            int ix = ixVar.getIx2(ixWL, ixL, nWeek, nLoc);
                            int ix2 = ixVar.getIx2(ixWL - 1, ixL, nWeek, nLoc);
                            expr1.AddTerm(1, OPL[ix]);
                            expr1.AddTerm(-1, OPL[ix2]);
                            expr1.AddTerm(1, Fire[ix]);
                            gModel.AddConstr(expr1, GRB.GREATER_EQUAL, 0, "Fire_Labor[" + wl.WEEK + "," + ll.LOC + "]");
                            expr1.Clear();
                        }
                    });
                });

                //subject to Total_cost: Cost_Tot =              
                expr1.AddTerm(1, Cost_Tot);
                //sum { p in WEEKP,j in CROP,l in LOC}
                //Plant[p, j, l] * (Cplant[j] + LabP[j] + LabH[j])
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        inputData.LocList.ForEach(ll =>
                        {
                            int ixL = inputData.LocList.IndexOf(ll);
                            int ix2 = ixVar.getIx2(ixC, ixL, nCrop, nLoc);
                            double totCLL = inputData.CropBudgetList[ix2].Cplant + inputData.CropBudgetList[ix2].LabP + inputData.CropBudgetList[ix2].LabH;
                            int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                            expr2.AddTerm(1 * totCLL, Plant[ix]);
                        });
                    });
                });

                //+ sum{ t in WEEK,f in FAC}
                //OPF[t, f] * Clabor###############
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.FacList.ForEach(fl =>
                    {
                        int ixF = inputData.FacList.IndexOf(fl);
                        int ix = ixVar.getIx2(ixW, ixF, nWeek, nFac);
                        expr2.AddTerm(1 * Clabor, OPF[ix]);
                    }); 
                });
                //+ sum{ t in WEEK,l in LOC}
                //OPL[t, l] * Clabor
                //+ sum{ t in WEEK,l in LOC}
                //HIRE[t, l] * Chire
                //+ sum{ t in WEEK,l in LOC}
                //OPT[t, l] * Ctemp;
                inputData.WeekList.ForEach(wl =>
                {
                    int ixW = inputData.WeekList.IndexOf(wl);
                    inputData.LocList.ForEach(ll =>
                    {
                        int ixL = inputData.LocList.IndexOf(ll);
                        int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                        expr2.AddTerm(Clabor, OPL[ix]);
                        expr2.AddTerm(Chire, Hire[ix]);
                        expr2.AddTerm(Ctemp, OPT[ix]);
                    });
                });
                inputData.FacList.ForEach(fl =>
                {
                    int ixF = inputData.FacList.IndexOf(fl);
                    double opening_cost = fl.OpenFCost;
                    expr2.AddTerm(opening_cost, OpenF[ixF]);
                });


                gModel.AddConstr(expr1, GRB.EQUAL, expr2, "subject to Total_cost");
                expr1.Clear();
                expr2.Clear();

                /* subject to Cut_Defn {c in 1..nCUT,s in SCEN}: 
	            Min_Stage[s] <= 	
	            -sum{(p,h,j,l)} (if CutsMod[s,c]=1 then harvest_price[p,h,j,l,s,c]* Harvest[p,h,j,l])
	            +sum{(p,h,j,f)} (if CutsMod[s,c]=1 then selection_price[p,h,j,f,s,c]*(sum {l in LOC}(1-Salv[p,h,j,l])))
	            +sum{t in WEEKS,k in PROD,i in CUST} (if CutsMod[s,c]=1 then demand_price2[t,k,i,s,c]*Dem2[t,k,i])
	            +sum{h in WEEKH,k in PROD,q in QUAL,f in FAC} (if CutsMod[s,c]=1 then production_price[h,k,q,f,s,c])
	            +sum{h in WEEKH,k in PROD,q in QUAL,f in FAC} (if CutsMod[s,c]=1 then packing_price[h,k,q,f,s,c]*COL[h,k,q]*(sum {(p,h,j,k):p<=h and Ccrop[j]=Pcrop[k]}Pod[p,h,j,k]/Weight[k]))
	            +sum{h in WEEKH,j in CROP} (if CutsMod[s,c]=1 then scrap_price[h,j,s,c]*(sum {l in LOC,p in WEEKP: (p,h,j) and p<=h}Salv[p,h,j,l]))
	            +sum{h in WEEKH,k in PROD,q in QUAL,w in WARE} (if CutsMod[s,c]=1 then Inv1_price[h,k,q,w,s,c])
	            +sum{(h,t),k in PROD,q in QUAL,w in WARE:t>h} (if CutsMod[s,c]=1 then Invw_price[h,t,k,q,w,s,c])
	            +sum{h in WEEKH,k in PROD,q in QUAL,d in DC} (if CutsMod[s,c]=1 then Inv2_price[h,k,q,d,s,c])
	            +sum{(h,t),k in PROD,q in QUAL,d in DC:t>h} (if CutsMod[s,c]=1 then Invd_price[h,t,k,q,d,s,c])
	            +sum{t in WEEKS,k in PROD,q in QUAL,f in FAC} (if CutsMod[s,c]=1 then SC_price[t,k,q,f,s,c])
	            +sum{(h,t)} (if CutsMod[s,c]=1 then SPD_price [h,t,s,c])
	            +sum{(h,t)} (if CutsMod[s,c]=1 then SWD_price [h,t,s,c])
	            +sum{(h,t),k in PROD,q in QUAL} (if CutsMod[s,c]=1 then SD_price[h,t,k,q,s,c])
	            +sum{(h,t),k in PROD,q in QUAL} (if CutsMod[s,c]=1 then SPD2_price[h,t,k,q,s,c]);
                */
                for (int c = 0; c < Cuts.Count; c++) // Loop through CUTS
                {
                    for (int scen = 0; scen < nScen; scen++) // Cuts[c].Count   Loop through Scenarios
                    {
                        if (Cuts[c][scen].CutsMods != 0)
                        {
                            double totSum = 0;

                            expr1.AddTerm(1, Min_Stage[scen]);
                            totSum = 0;
                            //-sum{(p,h,j,l)} (if CutsMod[s,c]=1 then harvest_price[p,h,j,l,s,c]* Yield*Total*Plant)
                            for (int p = 0; p < nWeekP; p++)
                            {
                                for (int h = 0; h < nWeekH; h++)
                                {
                                    for (int j = 0; j < nCrop; j++)
                                    {
                                        for (int l = 0; l < nLoc; l++)
                                        {
                                            int ix3 = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                                            int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                            double yield = 0;
                                            if (deterministic) // if model is deterministic use expected value
                                            {
                                                yield = inputData.YieldList_exp.ElementAt(ix4).Yield;
                                            }
                                            else // if model is stochastic, use yield for that scenario "scen" //  CHECK, should pull yield corresponding to the true sceario
                                            {
                                                int true_scenario = Random_Scenarios[scen];
                                                int ix5 = ixVar.getIx5(p, h, j, l, true_scenario, nWeekP, nWeekH, nCrop, nLoc, nScenTotal);
                                                yield = inputData.YieldList[ix5].Yield;

                                                if(yield>0)
                                                {
                                                    double test = yield;
                                                }
                                                if(p==0 & h == 18 & j ==1 & l == 3 )
                                                {
                                                    double dual_val = Cuts[c][scen].duals.Sum_harvest[ix4];
                                                }

                                            }
                                            expr2.AddTerm(-1 * Cuts[c][scen].duals.Sum_harvest[ix4] * yield, Plant[ix3]);
                                        }
                                    }
                                }
                            }

                            // - duals cap_pf * open f
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int f = 0; f < nFac; f++)
                                {
                                    int ix2 = ixVar.getIx2(h, f, nWeekH, nFac);
                                    double pfcap = inputData.FacList[f].PFcap;
                                    expr2.AddTerm(Cuts[c][scen].duals.Cap_PF[ix2] * pfcap, OpenF[f]);
                                }
                            }
                            //Holds the sum of all dual values that are not attached to a variable here
                            totSum = totSum + Cuts[c][scen].duals.Constants;

                            if (Cuts[c][scen].CutsMods == 1) //optimality cut
                            {
                                gModel.AddConstr(expr1, GRB.LESS_EQUAL, expr2 + totSum, "Opt_Cut[" + (c + 1) + "," + (scen + 1) + "]");
                            }
                            else if (Cuts[c][scen].CutsMods == 2) //feasiblity cut
                            {
                                gModel.AddConstr(0, GRB.LESS_EQUAL, expr2 + totSum, "Feas_Cut[" + (c + 1) + "," + (scen + 1) + "]");
                            }
                            expr1.Clear();
                            expr2.Clear();
                        }
                    }
                }
                expr1.Clear();


                //subject to Cut_Defn2 {s in SCEN}: Min_Stage[s]>= Min_Var[s];
                Random_Scenarios.ForEach(sl =>
                {
                    int ixS = Random_Scenarios.IndexOf(sl);
                    expr1.AddTerm(1, Min_Var[ixS]);
                    expr2.AddTerm(1, Min_Stage[ixS]);
                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, expr2, "Cut_Defn2[" + sl.ToString() + "]");
                    expr1.Clear();
                });
                expr1.Clear();
                expr2.Clear();

                //subject to Cut_Defn3 { s in SCEN}: Min_Var[s] <= Target;

                Random_Scenarios.ForEach(sl =>
                {
                    int ixS = Random_Scenarios.IndexOf(sl);
                    expr1.AddTerm(1, Min_Var[ixS]);
                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, Target, "Cut_Defn3[" + sl.ToString() + "]");
                    expr1.Clear();
                });
                    expr1.Clear();




                // Restrict Min_Stage to avoid unboundness #CHECK
                Random_Scenarios.ForEach(sl =>
                {
                    int ixS = Random_Scenarios.IndexOf(sl);
                    expr1.AddTerm(1, Min_Stage[ixS]);
                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, 100000000000, "MaxMinStage[" + sl.ToString() + "]");
                    expr1.Clear();
                });
                expr1.Clear();

                // Restrict OPL variable (this will result on restricting Hire and Fire too)
                
                /*
                for (int i = 0; i < OPL.Length; i++)
                {
                    expr1.AddTerm(1, OPL[i]);
                    gModel.AddConstr(expr1, GRB.EQUAL, 0, "noOPL[" + i + "]");
                    expr1.Clear();
                }
                */

                // Only plant between init_week+4 and init_week+56  (52 planning weeks in total)
                expr1.Clear();
                for (int p = 0; p < nWeekP; p++)
                {
                    if (inputData.WeekPList[p].WEEKP < masterModelParameters.init_week + 4 || inputData.WeekPList[p].WEEKP > masterModelParameters.init_week + 56)
                    {
                        for (int j = 0; j < nCrop; j++)
                        {
                            for (int l = 0; l < nLoc; l++)
                            {
                                int ix3 = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                                expr1.AddTerm(1, Plant[ix3]);
                            }
                        }
                    }
                }
                gModel.AddConstr(expr1, GRB.EQUAL, 0, "plantSeason");
                expr1.Clear();

                //force all facilities to be open
                expr1.Clear();
                for (int f = 0; f < nFac; f++)
                {
                    gModel.AddConstr(OpenF[f], GRB.EQUAL, 1, "FACILITYOPEN");
                }


                // Store the model as .lp file
                gModel.Write(outputFolder + "MasterProblem//Models//Master_Model_" + masterModelParameters.nCut + ".lp");

                try
                {
                    gModel.Parameters.MIPGap = 0.001; // reduce the GAP to 0.1%
                    gModel.Optimize();
                    gModel.Parameters.MIPGap = 0.00001; //return the GAP to 0.001%
                    Console.WriteLine("Status: " + gModel.Status + "\n");

                    bool hasError = false;
                    if (true) // Export the variables != to disable
                    {
                        if (gModel.Status == 2) // || gModel.Status ==3 || gModel.Status == 4)
                        {
                            Console.WriteLine("OF Value (M_Rev): " + Math.Round(gModel.ObjVal, 0));
                            Console.WriteLine("Costs " + Math.Round(Cost_Tot.X, 0));
                            List<GeneralOutputs> yOutputs = new List<GeneralOutputs>();

                            for (int p = 0; p < nWeekP; p++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    for (int l = 0; l < nLoc; l++)
                                    {
                                        int ix3 = ixVar.getIx3(p, j, l, nWeekP, nCrop, nLoc);
                                        GeneralOutputs yo = new GeneralOutputs()
                                        {
                                            WEEKP = inputData.WeekPList[p].WEEKP,
                                            LOC = inputData.LocList[l].LOC,
                                            CROP = inputData.CropList[j].CROP,
                                            Value = Y[ix3].X
                                        };
                                        yOutputs.Add(yo);
                                    }
                                }
                            }

                            List<GeneralOutputs> minVarOutputs = new List<GeneralOutputs>();
                            Random_Scenarios.ForEach(sl =>
                            {
                                int ix = Random_Scenarios.IndexOf(sl);
                                GeneralOutputs mvo = new GeneralOutputs()
                                {
                                    SCEN = sl,
                                    Value = Min_Var[ix].X
                                };
                                minVarOutputs.Add(mvo);
                            });

                            double CostTot = Cost_Tot.X;

                            List<GeneralOutputs> hireOutputs = new List<GeneralOutputs>();
                            inputData.WeekList.ForEach(wl =>
                            {
                                int ixW = inputData.WeekList.IndexOf(wl);
                                inputData.LocList.ForEach(ll =>
                                {
                                    int ixL = inputData.LocList.IndexOf(ll);
                                    int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                                    GeneralOutputs ho = new GeneralOutputs()
                                    {
                                        WEEK = wl.WEEK,
                                        LOC = ll.LOC,
                                        Value = Hire[ix].X
                                    };
                                    hireOutputs.Add(ho);
                                });
                            });

                            List<GeneralOutputs> fireOutputs = new List<GeneralOutputs>();
                            inputData.WeekList.ForEach(wl =>
                            {
                                int ixW = inputData.WeekList.IndexOf(wl);
                                inputData.LocList.ForEach(ll =>
                                {
                                    int ixL = inputData.LocList.IndexOf(ll);
                                    int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                                    GeneralOutputs fo = new GeneralOutputs()
                                    {
                                        WEEK = wl.WEEK,
                                        LOC = ll.LOC,
                                        Value = Fire[ix].X
                                    };
                                    fireOutputs.Add(fo);
                                });
                            });

                            List<GeneralOutputs> plantOutputs = new List<GeneralOutputs>();
                            inputData.WeekPList.ForEach(wpl =>
                            {
                                int ixWP = inputData.WeekPList.IndexOf(wpl);
                                inputData.CropList.ForEach(cl =>
                                {
                                    int ixC = inputData.CropList.IndexOf(cl);
                                    inputData.LocList.ForEach(ll =>
                                    {
                                        int ixL = inputData.LocList.IndexOf(ll);
                                        int ix = ixVar.getIx3(ixWP, ixC, ixL, nWeekP, nCrop, nLoc);
                                        GeneralOutputs po = new GeneralOutputs()
                                        {
                                            WEEKP = wpl.WEEKP,
                                            CROP = cl.CROP,
                                            LOC = ll.LOC,
                                            Value = Plant[ix].X
                                        };
                                        plantOutputs.Add(po);
                                    });
                                });
                            });

                            List<GeneralOutputs> OpenFOutputs = new List<GeneralOutputs>();
                            inputData.FacList.ForEach(fl =>
                            {
                                int iF = inputData.FacList.IndexOf(fl);

                                GeneralOutputs fo = new GeneralOutputs()
                                {
                                    FAC = fl.FAC,
                                    Value = OpenF[iF].X
                                };
                                OpenFOutputs.Add(fo);
                            });

                            List<GeneralOutputs> OpenDOutputs = new List<GeneralOutputs>();
                            inputData.DistList.ForEach(dl =>
                            {
                                int iD = inputData.DistList.IndexOf(dl);
                                GeneralOutputs di = new GeneralOutputs()
                                {
                                    DC = dl.DIST,
                                    Value = OpenD[iD].X
                                };
                                OpenDOutputs.Add(di);
                            });


                            List<GeneralOutputs> minStageOutputs = new List<GeneralOutputs>();
                            Random_Scenarios.ForEach(sl =>
                            {
                                int ix = Random_Scenarios.IndexOf(sl);
                                GeneralOutputs po = new GeneralOutputs()
                                {
                                    SCEN = sl,
                                    Value = Min_Stage[ix].X
                                };
                                minStageOutputs.Add(po);
                            });

                            List<GeneralOutputs> optOutputs = new List<GeneralOutputs>();
                            inputData.WeekList.ForEach(wl =>
                            {
                                int ixW = inputData.WeekList.IndexOf(wl);
                                inputData.LocList.ForEach(ll =>
                                {
                                    int ixL = inputData.LocList.IndexOf(ll);
                                    int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                                    GeneralOutputs opto = new GeneralOutputs()
                                    {
                                        WEEK = wl.WEEK,
                                        LOC = ll.LOC,
                                        Value = OPT[ix].X
                                    };
                                    optOutputs.Add(opto);
                                });
                            });

                            List<GeneralOutputs> oplOutputs = new List<GeneralOutputs>();
                            inputData.WeekList.ForEach(wl =>
                            {
                                int ixW = inputData.WeekList.IndexOf(wl);
                                inputData.LocList.ForEach(ll =>
                                {
                                    int ixL = inputData.LocList.IndexOf(ll);
                                    int ix = ixVar.getIx2(ixW, ixL, nWeek, nLoc);
                                    GeneralOutputs oplo = new GeneralOutputs()
                                    {
                                        WEEK = wl.WEEK,
                                        LOC = ll.LOC,
                                        Value = OPL[ix].X
                                    };
                                    oplOutputs.Add(oplo);
                                });
                            });

                            List<GeneralOutputs> opfOutputs = new List<GeneralOutputs>();
                            inputData.WeekList.ForEach(wl =>
                            {
                                int ixW = inputData.WeekList.IndexOf(wl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    int ix = ixVar.getIx2(ixW, ixF, nWeek, nFac);
                                    GeneralOutputs opfo = new GeneralOutputs()
                                    {
                                        WEEK = wl.WEEK,
                                        FAC = fl.FAC,
                                        Value = OPF[ix].X
                                    };
                                    opfOutputs.Add(opfo);
                                });
                            });

                            List<GeneralOutputs> CostTotOutputs = new List<GeneralOutputs>();
                            inputData.WeekList.ForEach(wl =>
                            {
                                int ixW = inputData.WeekList.IndexOf(wl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    int ix = ixVar.getIx2(ixW, ixF, nWeek, nFac);
                                    GeneralOutputs opfo = new GeneralOutputs()
                                    { 
                                        Value = Cost_Tot.X
                                    };
                                    opfOutputs.Add(opfo);
                                });
                            });

                            double Mrev = 0;
                            if (gModel.Status == 2)
                            {
                                Mrev = gModel.ObjVal;
                            }

                            masterModelOutputs = new MasterModelOutputs()
                            {
                                Y = yOutputs,
                                Min_Var = minVarOutputs,
                                Cost_Tot = CostTot,
                                Hire = hireOutputs,
                                Fire = fireOutputs,
                                Plant = plantOutputs,
                                OpenF = OpenFOutputs,
                                OpenD = OpenDOutputs,
                                Min_Stage = minStageOutputs,
                                OPT = optOutputs,
                                OPL = oplOutputs,
                                OPF = opfOutputs,
                                hasError = hasError,
                                status = gModel.Status,
                                M_rev = Mrev,
                                Cost_Tot_Output = CostTotOutputs
                            };
                        }
                        else
                        {
                            hasError = true;
                            masterModelOutputs = new MasterModelOutputs()
                            {
                                hasError = hasError,
                                status = gModel.Status
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    masterModelOutputs = new MasterModelOutputs()
                    {
                        hasError = true,
                        status = gModel.Status
                    };
                    Console.WriteLine("ERROR SOLVING THE MASTER MODEL");
                    Console.WriteLine("Error code: " + ex.InnerException + ". " + ex.Message);
                }
            }
            catch (GRBException ex)
            {
                masterModelOutputs = new MasterModelOutputs()
                {
                    hasError = true,
                    status = gModel.Status
                };
                Console.WriteLine("Error code: " + ex.ErrorCode + ". " + ex.Message);
            }
            return masterModelOutputs;
        }
    }
}
