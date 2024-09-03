using Gurobi;
using ModelStochastic6.Models;
using ModelStochastic6.Models.Inputs;
using ModelStochastic6.Models.Outputs;
using ModelStochastic6.Models.Outputs.SubModel;
using ModelStochastic6.Models.SubModelParams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ModelStochastic6
{
    class SubModel
    {
        /* Main Changes since previous version (Model3_working_09.09.2021):
         * 1. Multiplying price*weight in the objective function
         * 2. Added the convertion to boxes (weight) in the demand constraint
         */
        public SubModel()
        {
        }
        public SubModelOutputs buildModel(
            GRBModel gModel,
            InputData inputData,
            SubModelParameters subModelParameters,
            string outputFolder, int scen, UserInputs userInputs
            )
        {
            SubModelOutputs submodelOutputs = new SubModelOutputs();
            try
            {
                bool activate_target = userInputs.activate_target;
                bool useContract = userInputs.useContract;
                GRBLinExpr expr1 = 0.0;
                GRBLinExpr expr2 = 0.0;
                GRBLinExpr expr3 = 0.0;
                IndexesVar ixVar = new IndexesVar();

                double BM = 30000000; //BIG M VALUE
                int startAt;
                int endAt;
                int counter = 0;
                List<Constraint> constraints = new List<Constraint>();

                List<ProductionPrice_param> productionPriceList = new List<ProductionPrice_param>();
                List<PackingPrice_param> packingPriceList = new List<PackingPrice_param>();

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
                int nCrop = inputData.CropList.Count;
                int nLoc = inputData.LocList.Count;
                int nWeekP = inputData.WeekPList.Count;
                int nWeekH = inputData.WeekHList.Count;

                gModel.Parameters.MIPGap = 0.1 / 100; //set the GAP to 0.1%

                Console.Write("Variables: ");
                GRBVar[] PACK = gModel.AddVars(nWeekH * nProd * nQual * nFac, GRB.CONTINUOUS);
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                PACK[ix].VarName = "PACK[" + whl.WEEKH + "," + pl.PROD + "," + ql.QUAL + "," + fl.FAC + "]";
                            });
                        });
                    });
                });

                GRBVar[] SP = gModel.AddVars(nWeekP * nWeekH * nCrop * nLoc * nFac, GRB.CONTINUOUS);
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.WeekHList.ForEach(whl =>
                    {
                        int ixWH = inputData.WeekHList.IndexOf(whl);
                        inputData.CropList.ForEach(cl =>
                        {
                            int ixC = inputData.CropList.IndexOf(cl);
                            inputData.LocList.ForEach(ll =>
                            {
                                int ixL = inputData.LocList.IndexOf(ll);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    int ix = ixVar.getIx5(ixWP, ixWH, ixC, ixL, ixF, nWeekP, nWeekH, nCrop, nLoc, nFac);
                                    SP[ix].VarName = "SP[" + wpl.WEEKP + "," + whl.WEEKH + "," + cl.CROP + "," + ll.LOC + "," + fl.FAC + "]";
                                });
                            });
                        });
                    });
                });

                GRBVar[] Sel = gModel.AddVars(nWeekP * nWeekH * nCrop * nFac, GRB.CONTINUOUS);
                for (int p = 0; p < nWeekP; p++)
                {
                    for (int h = 0; h < nWeekH; h++)
                    {
                        for (int j = 0; j < nCrop; j++)
                        {
                            for (int f = 0; f < nFac; f++)
                            {
                                int ix = ixVar.getIx4(p, h, j, f, nWeekP, nWeekH, nCrop, nFac);
                                Sel[ix].VarName = "Sel[" + inputData.WeekPList[p].WEEKP + "," + inputData.WeekHList[h].WEEKH + "," + inputData.CropList[j].CROP + "," + inputData.FacList[f].FAC + "]";
                            }
                        }
                    }
                }

                GRBVar[] SC = gModel.AddVars(nWeekS * nProd * nQual * nFac * nCust * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
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
                                        int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                        SC[ix].VarName = "SC[" + wsl.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + fl.FAC + "," + cl.CUST + "," + tl.TRANS + "]";
                                    });
                                });
                            });
                        });
                    });
                });

                GRBVar[] SD = gModel.AddVars(nWeek1 * nProd * nQual * nDC * nCust * nTrans, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);

                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);

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

                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);

                                        SD[ix].VarName = "SD[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + dl.DIST + "," + cl.CUST + "," + tl.TRANS + "]";

                                    });
                                });
                            });
                        });
                    });
                });

                GRBVar[] SW = gModel.AddVars(nWeek1 * nProd * nQual * nWare * nCust * nTrans, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
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

                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                        SW[ix].VarName = "SW[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + wl.WARE + "," + cl.CUST + "," + tl.TRANS + "]";

                                    });
                                });
                            });
                        });
                    });
                });

                GRBVar[] SPD = gModel.AddVars(nWeek1 * nProd * nQual * nFac * nDC * nTrans, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);

                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);

                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);

                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        SPD[ix].VarName = "SPD[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + fl.FAC + "," + dl.DIST + "," + tl.TRANS + "]";

                                    });
                                });
                            });
                        });
                    });
                });

                GRBVar[] SPW = gModel.AddVars(nWeek1 * nProd * nQual * nFac * nWare * nTrans, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
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
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                        SPW[ix].VarName = "SPW[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + fl.FAC + "," + wl.WARE + "," + tl.TRANS + "]";
                                    });
                                });
                            });
                        });
                    });
                });

                GRBVar[] SWD = gModel.AddVars(nWeek1 * nProd * nQual * nWare * nDC * nTrans, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        SWD[ix].VarName = "SWD[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + wl.WARE + "," + dl.DIST + "," + tl.TRANS + "]";
                                    });
                                });
                            });
                        });
                    });
                });

                GRBVar[] Invw = gModel.AddVars(nWeek1 * nProd * nQual * nWare, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                Invw[ix].VarName = "Invw[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + wl.WARE + "]";
                            });
                        });
                    });
                });

                GRBVar[] Invd = gModel.AddVars(nWeek1 * nProd * nQual * nDC, GRB.CONTINUOUS);
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                Invd[ix].VarName = "Invd[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + dl.DIST + "]";
                            });
                        });
                    });
                });

                GRBVar[] TC = gModel.AddVars(nWeekS * nProd * nFac * nCust * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.FacList.ForEach(fl =>
                        {
                            int ixF = inputData.FacList.IndexOf(fl);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    int ix = ixVar.getIx5(ixWS, ixP, ixF, ixC, ixT, nWeekS, nProd, nFac, nCust, nTrans);
                                    TC[ix].VarName = "TC[" + wsl.WEEKS + "," + pl.PROD + "," + fl.FAC + "," + cl.CUST + "," + tl.TRANS + "]";
                                });
                            });
                        });

                    });
                });

                GRBVar[] TD = gModel.AddVars(nWeekS * nProd * nDC * nCust * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.DistList.ForEach(dl =>
                        {
                            int ixD = inputData.DistList.IndexOf(dl);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    int ix = ixVar.getIx5(ixWS, ixP, ixD, ixC, ixT, nWeekS, nProd, nDC, nCust, nTrans);
                                    TD[ix].VarName = "TD[" + wsl.WEEKS + "," + pl.PROD + "," + dl.DIST + "," + cl.CUST + "," + tl.TRANS + "]";
                                });
                            });
                        });

                    });
                });

                GRBVar[] TW = gModel.AddVars(nWeekS * nProd * nWare * nCust * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.WareList.ForEach(wl =>
                        {
                            int ixW = inputData.WareList.IndexOf(wl);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    int ix = ixVar.getIx5(ixWS, ixP, ixW, ixC, ixT, nWeekS, nProd, nWare, nCust, nTrans);
                                    TW[ix].VarName = "TW[" + wsl.WEEKS + "," + pl.PROD + "," + wl.WARE + "," + cl.CUST + "," + tl.TRANS + "]";
                                });
                            });
                        });
                    });
                });

                GRBVar[] TPD = gModel.AddVars(nWeekS * nProd * nFac * nDC * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.FacList.ForEach(fl =>
                        {
                            int ixF = inputData.FacList.IndexOf(fl);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    int ix = ixVar.getIx5(ixWS, ixP, ixF, ixD, ixT, nWeekS, nProd, nFac, nDC, nTrans);
                                    TPD[ix].VarName = "TPD[" + wsl.WEEKS + "," + pl.PROD + "," + fl.FAC + "," + dl.DIST + "," + tl.TRANS + "]";
                                });
                            });
                        });
                    });
                });

                GRBVar[] TPW = gModel.AddVars(nWeekS * nProd * nFac * nWare * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.FacList.ForEach(fl =>
                        {
                            int ixF = inputData.FacList.IndexOf(fl);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    int ix = ixVar.getIx5(ixWS, ixP, ixF, ixW, ixT, nWeekS, nProd, nFac, nWare, nTrans);
                                    TPW[ix].VarName = "TPW[" + wsl.WEEKS + "," + pl.PROD + "," + fl.FAC + "," + wl.WARE + "," + tl.TRANS + "]";
                                });
                            });
                        });
                    });
                });

                GRBVar[] TWD = gModel.AddVars(nWeekS * nProd * nWare * nDC * nTrans, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.WareList.ForEach(wl =>
                        {
                            int ixW = inputData.WareList.IndexOf(wl);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    int ix = ixVar.getIx5(ixWS, ixP, ixW, ixD, ixT, nWeekS, nProd, nWare, nDC, nTrans);
                                    TWD[ix].VarName = "TWD[" + wsl.WEEKS + "," + pl.PROD + "," + wl.WARE + "," + dl.DIST + "," + tl.TRANS + "]";
                                });
                            });
                        });
                    });
                });

                GRBVar[] K = gModel.AddVars(nWeekH * nCrop, GRB.CONTINUOUS);
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        int ix = ixVar.getIx2(ixWH, ixC, nWeekH, nCrop);
                        K[ix].VarName = "K[" + whl.WEEKH + "," + cl.CROP + "]";
                    });
                });

                GRBVar[] Z = gModel.AddVars(nWeekS * nProd * nCust, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            Z[ix].VarName = "Z[" + wsl.WEEKS + "," + pl.PROD + "," + cl.CUST + "]";
                        });
                    });
                });

                GRBVar[] EXCESS = gModel.AddVars(nWeekS * nProd * nCust, GRB.CONTINUOUS);
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            EXCESS[ix].VarName = "EXCESS[" + wsl.WEEKS + "," + pl.PROD + "," + cl.CUST + "]";
                        });
                    });
                });




                // Auxillary variable for the realized income
                GRBVar real_income = gModel.AddVar(double.MinValue, double.MaxValue, 1, GRB.CONTINUOUS, "real_income");

                Console.Write("\t\tOK.\t\n");

                DateTime time0 = DateTime.Now;
                DateTime startT = DateTime.Now;

                Console.Write("Objetive Function: ");
                expr1.Clear();
                expr2.Clear();
                expr3.Clear();

                double lastWeekH = inputData.WeekHList.LastOrDefault().WEEKH;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        int ix2 = ixVar.getIx2(ixWS, ixP, nWeekS, nProd);
                        double weight = pl.Weight;
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix3 = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double price = 0;
                            if (useContract)  // If the contract is active
                            {
                                price = inputData.DemList[ixPrice].contractPrice;
                            }
                            else
                            {
                                price = subModelParameters.priceList[ixPrice].price;
                            }

                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                        expr1.AddTerm(price * weight, SC[ix]);
                                    });
                                });
                            });
                           
                        });
                    });
                });

                //Z[s, p, C] * Pavg[t, k] * 10 
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        int ixZ = ixVar.getIx2(ixWS, ixP, nWeekS, nProd);
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix3 = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double contract_price = inputData.DemList[ix3].contractPrice;
                            double spot_price = subModelParameters.priceList[ix3].price;

                            double spot_scalar = 1 + inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("Percent_Increase_Spot")).Value; 
                            double spot_scalar2 = 1 - inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("Percent_Decrease_Spot")).Value;
                            double weight = inputData.ProdList[ixP].Weight;
                            double net_price = 0;

                            //For anything you purchase, the cost should be the difference between the value received and the cost to acquire
                            if (useContract)  // If the contract is active
                            {
                                net_price = spot_price * spot_scalar - contract_price;

                                //if (net_price < 0) // This disallows arbitrage for the contract prices
                                //{
                                //    net_price = .00001;
                                //}
                            }
                            else
                            {
                                net_price = spot_price * spot_scalar - spot_price * spot_scalar2;
                            }

                            double totalPMN = net_price * weight;
                            expr1.AddTerm(-1 * totalPMN, Z[ix3]);
                        });
                    });
                });

                //Cost of renumeration in excess of the contract demand
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        int ixZ = ixVar.getIx2(ixWS, ixP, nWeekS, nProd);
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double contract_price = inputData.DemList[ixPrice].contractPrice;
                            double spot_price = subModelParameters.priceList[ixPrice].price;

                            /*
                            if (useContract)  // If the contract is active
                            {
                                price = inputData.DemList[ixPrice].contractPrice;
                            }
                            else
                            {
                                price = subModelParameters.priceList[ixPrice].price;
                            }
                            */

                            double spot_scalar = inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("Percent_Decrease_Spot")).Value;
                            double weight = inputData.ProdList[ixP].Weight;
                            double net_price = 0;

                            //For anything that is excess, we have to remove some of the revenue captured by SC so that all Excess amount is only compensated according
                            // to the spot market
                            if (useContract)  // If the contract is active
                            {
                                net_price = contract_price - spot_price * (1- spot_scalar);
                            }
                            else
                            {
                                net_price = 0;
                            }
                            double totalPMN = net_price* weight;
                            expr1.AddTerm(-1 * totalPMN, EXCESS[ixPrice]);
                        });
                    });
                });



                //+sum{ k in PROD,q in QUAL,i in CUST,(h, t) in WEEK1,w in WARE,r in TRANS
                //: t >= h >= t - SL[k] and q<= Qmin[i]}
                //SW[h, t, k, q, w, i, r] * price[t, k, i] ---->  x0
                inputData.ProdList.ForEach(pl =>
                {
                    int ixP = inputData.ProdList.IndexOf(pl);
                    double SL = pl.SL;
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
                                double wh = w1l.WEEKH;
                                double ws = w1l.WEEKS;
                                WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(ws));
                                int ixWS = inputData.WeekSList.IndexOf(weekS);
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);      
                                        if (ws >= wh && wh >= (ws - SL) && ql.QUAL <= qMin)
                                        {
                                            int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                            double price = subModelParameters.priceList[ixPrice].price;
                                            double weight = inputData.ProdList[ixP].Weight;
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                            expr1.AddTerm(0 * price * weight, SW[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                });

                //+sum{ k in PROD,q in QUAL,i in CUST,(h, t) in WEEK1,d in DC,r in TRANS
                //: t >= h >= t - SL[k] and r = 'TM1' and q<= Qmin[i]}
                //SD[h, t, k, q, d, i, r] * price[t, k, i] ---->  x0
                inputData.ProdList.ForEach(pl =>
                {
                    int ixP = inputData.ProdList.IndexOf(pl);
                    double SL = pl.SL;
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
                                double wh = w1l.WEEKH;
                                double ws = w1l.WEEKS;

                                WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(ws));
                                int ixWS = inputData.WeekSList.IndexOf(weekS);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        string r = inputData.TransList.FirstOrDefault().TRANS;  
                                        if (ws >= wh && wh >= (ws - SL) && tl.TRANS.Equals(r) && ql.QUAL <= qMin)
                                        {
                                            int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                            double price = subModelParameters.priceList[ixPrice].price;
                                            double weight = inputData.ProdList[ixP].Weight;
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                            expr1.AddTerm(0 * price * weight, SD[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum{ f in FAC,h in WEEKH,k in PROD,q in QUAL}
                //PACK[h, k, q, f] * (Ccase[k] + LabF[k])
                inputData.FacList.ForEach(fl =>
                {
                    int ixF = inputData.FacList.IndexOf(fl);
                    inputData.WeekHList.ForEach(whl =>
                    {
                        int ixWH = inputData.WeekHList.IndexOf(whl);
                        inputData.ProdList.ForEach(pl =>
                        {
                            int ixP = inputData.ProdList.IndexOf(pl);
                            double Ccase = pl.Ccase;
                            double LabF = pl.LabF;
                            double tCL = Ccase + LabF;
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                expr1.AddTerm(-1 * tCL, PACK[ix]);
                            });
                        });
                    });
                });

                //-sum{ k in PROD,q in QUAL,i in CUST,t in WEEKS,f in FAC,r in TRANS
                //: t < last(WEEKH)}
                //SC[t, k, q, f, i, r] * price[t, k, i] * (Time[f, i, r] / SL[k]) ---->  x0
                inputData.ProdList.ForEach(pl =>
                {
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
                                        if (wsl.WEEKS < lastWeekH)
                                        {
                                            int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                            double price = subModelParameters.priceList[ixPrice].price;

                                            int ix3 = ixVar.getIx3(ixF, ixC, ixT, nFac, nCust, nTrans);
                                            double Time = inputData.CtList[ix3].Time;
                                            double totalPTSL = price * (Time / SL);
                                            double weight = inputData.ProdList[ixP].Weight;
                                            int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                            expr1.AddTerm(-0 * totalPTSL * weight, SC[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum{ (h, t) in WEEK1,k in PROD,q in QUAL,f in FAC,w in WARE,r in TRANS}
                //SPW[h, t, k, q, f, w, r] * Pavg[t, k] * (TimePW[f, w, r] / SL[k]) ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
                        double Pavg = 0;
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
                                        int ix3 = ixVar.getIx3(ixF, ixW, ixT, nFac, nWare, nTrans);
                                        double TimePW = inputData.CtpwList.Find(ctpwl =>
                                        ctpwl.FAC.Equals(fl.FAC) &&
                                        ctpwl.WARE.Equals(wl.WARE) &&
                                        ctpwl.TRANS.Equals(tl.TRANS)).TimePW;
                                        double totalTSL = Pavg * (TimePW / SL);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                        expr1.AddTerm(-0 * totalTSL, SPW[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum{ (h, t) in WEEK1,k in PROD,q in QUAL,f in FAC,d in DC,r in TRANS}
                //SPD[h, t, k, q, f, d, r] * Pavg[t, k] * (TimePD[f, d, r] / SL[k])  ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
                        double Pavg = 0;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        double TimePD = inputData.CtpdList.Find(ctpdl =>
                                        ctpdl.FAC.Equals(fl.FAC) &&
                                        ctpdl.DC.Equals(dl.DIST) &&
                                        ctpdl.TRANS.Equals(tl.TRANS)).TimePD;
                                        double totalTSL = Pavg * (TimePD / SL);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        expr1.AddTerm(-0 * totalTSL, SPD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE,i in CUST,r in TRANS}
                //SW[h, t, k, q, w, i, r] * price[t, k, i] * (TimeW[w, i, r] / SL[k]) ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(ws));
                    int ixWS = inputData.WeekSList.IndexOf(weekS);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
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
                                        int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                        double price = subModelParameters.priceList[ixPrice].price;
                                        int id3 = ixVar.getIx3(ixW, ixC, ixT, nWare, nCust, nTrans);
                                        double TimeW = inputData.CtwList[id3].TimeW;
                                        double totalTSL = price * (TimeW / SL);
                                        double weight = inputData.ProdList[ixP].Weight;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                        expr1.AddTerm(-0 * totalTSL * weight, SW[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE,d in DC,r in TRANS}
                //SWD[h, t, k, q, w, d, r] * Pavg[t, k] * (TimeWD[w, d, r] / SL[k]) ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
                        double Pavg = 0;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int id3 = ixVar.getIx3(ixW, ixD, ixT, nWare, nDC, nTrans);
                                        double TimeWD = inputData.CtwdList[id3].TimeWD;
                                        double totalTSL = Pavg * (TimeWD / SL);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        expr1.AddTerm(-0 * totalTSL, SWD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,d in DC,i in CUST,r in TRANS
                //: r = 'TM1'}
                //SD[h, t, k, q, d, i, r] * price[t, k, i] * (TimeD[d, i, r] / SL[k]) ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(ws));
                    int ixWS = inputData.WeekSList.IndexOf(weekS);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
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
                                        int id3 = ixVar.getIx2(ixD, ixC, nDC, nCust);
                                        double TimeD = inputData.CtdList[id3].TimeD;
                                        int ixPrice = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                        double price = subModelParameters.priceList[ixPrice].price;
                                        double totalTSL = price * (TimeD / SL);
                                        double weight = inputData.ProdList[ixP].Weight;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                        expr1.AddTerm(-0 * totalTSL * weight, SD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                // - the cost of shipping from the fields to the packaging facilities
                //SP { WEEKP,WEEKH,CROP,LOC,FAC, TRANS}
                // CHECK : May want to add the TRANS index to the SP variable later if more than one modes of transportation are later considered
                inputData.WeekPList.ForEach(wpl =>
                {
                    int ixWP = inputData.WeekPList.IndexOf(wpl);
                    inputData.WeekHList.ForEach(whl =>
                    {
                        int ixWH = inputData.WeekHList.IndexOf(whl);
                        inputData.CropList.ForEach(cl =>
                        {
                            int ixC = inputData.CropList.IndexOf(cl);
                            inputData.LocList.ForEach(ll =>
                            {
                                int ixL = inputData.LocList.IndexOf(ll);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixR = inputData.TransList.IndexOf(tl);

                                        int ix3 = ixVar.getIx3(ixL, ixF, ixR, nLoc, nFac, nTrans);
                                        int ix = ixVar.getIx5(ixWP, ixWH, ixC, ixL, ixF, nWeekP, nWeekH, nCrop, nLoc, nFac);
                                        double CTLF = inputData.CtlfList[ix3].CTLF;
                                        expr1.AddTerm(-1 * CTLF, SP[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { t in WEEKS,k in PROD,q in QUAL,f in FAC,i in CUST,r in TRANS}   CHECK
                //SC[t, k, q, f, i, r] * CT[f, i, r] * TraF[k]
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
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
                                        double totalCTT = CT * TraF;
                                        double weight = inputData.ProdList[ixP].Weight;
                                        int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                        expr1.AddTerm(-1 * totalCTT * weight, SC[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE,i in CUST,r in TRANS}
                //SW[h, t, k, q, w, i, r] * CTW[w, i, r] * TraF[k] ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
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
                                        double totalTSL = CTW * TraF;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                        expr1.AddTerm(-0 * totalTSL, SW[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,d in DC,i in CUST,r in TRANS
                //: r = 'TM1'}
                //SD[h, t, k, q, d, i, r] * CTD[d, i, r] * TraF[k] ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
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

                                            double totalTSL = CTD * TraF;
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                            expr1.AddTerm(-0 * totalTSL, SD[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,f in FAC,w in WARE,r in TRANS}
                //SPW[h, t, k, q, f, w, r] * CTPW[f, w, r] * TraF[k] ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
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
                                        double totalTSL = TraF * CTPW;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                        expr1.AddTerm(-0 * totalTSL, SPW[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,f in FAC,d in DC,r in TRANS}
                //SPD[h, t, k, q, f, d, r] * CTPD[f, d, r] * TraF[k] ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
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
                                        double totalTSL = TraF * CTPD;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        expr1.AddTerm(-0 * totalTSL, SPD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE,d in DC,r in TRANS}
                //SWD[h, t, k, q, w, d, r] * CTWD[w, d, r] * TraF[k] ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
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
                                        double totalCTWDT = CTWD * TraF;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        expr1.AddTerm(-0 * totalCTWDT, SWD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE}
                //Invw[h, t, k, q, w] * Chw[k, w]
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double TraF = pl.TraF;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                double Chw = inputData.ChwList.Find(chwl =>
                                chwl.WARE.Equals(wl.WARE) &&
                                chwl.PROD.Equals(pl.PROD)).Chw;
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                expr1.AddTerm(-1 * Chw, Invw[ix]);
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,d in DC} 
                //(Invd[h, t, k, q, d] / Pallet[k]) * Chd[k, d]
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double Pallet = pl.Pallet;
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
                                expr1.AddTerm(-1 * totalPCHD, Invd[ix]);
                            });
                        });
                    });
                });

                //+sum { h in WEEKH,j in CROP}
                //K[h, j] * Psalv[j] ---->  x0
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.CropList.ForEach(cl =>
                    {
                        int ixC = inputData.CropList.IndexOf(cl);
                        double Psalv = cl.Psalv;
                        int ix = ixVar.getIx2(ixWH, ixC, nWeekH, nCrop);
                        expr1.AddTerm(0, K[ix]);
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,f in FAC,d in DC,r in TRANS}
                //(SPD[h, t, k, q, f, d, r] / Pallet[k]) * 13 ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double Pallet = pl.Pallet;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int magicNumber = 13;
                                        double totalPMG = magicNumber / Pallet;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        expr1.AddTerm(-0 * totalPMG, SPD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                //-sum { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE,d in DC,r in TRANS}
                //(SWD[h, t, k, q, w, d, r] / Pallet[k]) * 13; ---->  x0
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    double ws = w1l.WEEKS;
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double Pallet = pl.Pallet;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int magicNumber = 13;
                                        double totalPMG = magicNumber / Pallet;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        expr1.AddTerm(-0 * totalPMG, SWD[ix]);
                                    });
                                });
                            });
                        });
                    });
                });

                DateTime utime = DateTime.Now;
                Console.Write("\tOK.\t" + Math.Round((utime - startT).TotalSeconds, 2) + "\n");
                startT = DateTime.Now;

                //Insertar funcion objetivo
                gModel.SetObjective(expr1, GRB.MAXIMIZE);

                /* ***************
                * ADD Constraints
                *  ***************
                */
                int ct = 1;
                Console.Write("Constraints: ");
                startAt = 0;
                gModel.AddConstr(real_income, GRB.EQUAL, expr1, "Calc_Rev");
                expr1.Clear();

                counter++;
                endAt = counter;
                Constraint Calc_Rev = new Constraint()
                {
                    name = "Calc_Rev",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Calc_Rev);
                utime = DateTime.Now;

                if (activate_target)
                {
                    startAt = counter;
                    gModel.AddConstr(real_income, GRB.GREATER_EQUAL, subModelParameters.Target_Rev, "Avoid_Loss");
                    counter++;
                    endAt = counter;
                    Constraint Avoid_Loss = new Constraint()
                    {
                        name = "Avoid_Loss",
                        startAt = startAt,
                        endAt = endAt,
                        length = endAt - startAt
                    };
                    constraints.Add(Avoid_Loss);
                    utime = DateTime.Now;
                }

                //# C1: Transportation of amount harvested
                //subject to Sum_harvest { (p, h, j, l)}:
                //harvest[p, h, j, l] = sum { f in FAC} SP[p, h, j, l, f];
                startAt = counter;
                for (int p = 0; p < nWeekP; p++)
                {
                    for (int h = 0; h < nWeekH; h++)
                    {
                        for (int j = 0; j < nCrop; j++)
                        {
                            for (int l = 0; l < nLoc; l++)
                            {
                                int ix4 = ixVar.getIx4(p, h, j, l, nWeekP, nWeekH, nCrop, nLoc);
                                double harvest = subModelParameters.HarvestList[ix4];
                                for (int f = 0; f < nFac; f++)
                                {
                                    int ix5 = ixVar.getIx5(p, h, j, l, f, nWeekP, nWeekH, nCrop, nLoc, nFac);
                                    double openf = subModelParameters.facOpenList[f].open;
                                    expr1.AddTerm(1, SP[ix5]);
                                }
                                gModel.AddConstr(harvest, GRB.EQUAL, expr1, "Sum_harvest[" + inputData.WeekPList[p].WEEKP + "," + inputData.WeekHList[h].WEEKH + "," + inputData.CropList[j].CROP + "," + inputData.LocList[l].LOC + "]");
                                expr1.Clear();
                                counter++;
                            }
                        }
                    }
                }

                endAt = counter;
                Constraint Sum_harvest = new Constraint()
                {
                    name = "Sum_harvest",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Sum_harvest);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C2: Initial inspection in the facility f
                //subject to Init_FAC {p, h, j, f}:
                //Sel[p, h, j, f] = sum{ l in LOC}
                //SP[p, h, j, l, f] * (1 - Salv[p, h, j, l]); //SALV REMOVED
                startAt = counter;
                for (int p = 0; p < nWeekP; p++)
                {
                    for (int h = 0; h < nWeekH; h++)
                    {
                        for (int j = 0; j < nCrop; j++)
                        {
                            for (int f = 0; f < nFac; f++)
                            {
                                int ix1 = ixVar.getIx4(p, h, j, f, nWeekP, nWeekH, nCrop, nFac);
                                expr1.AddTerm(1, Sel[ix1]);
                                for (int l = 0; l < nLoc; l++)
                                {
                                    int ix5 = ixVar.getIx5(p, h, j, l, f, nWeekP, nWeekH, nCrop, nLoc, nFac);

                                    expr2.AddTerm(1, SP[ix5]);
                                }
                                double openf = subModelParameters.facOpenList[f].open;
                                gModel.AddConstr(expr1, GRB.EQUAL, openf * expr2, "Init_FAC[" + inputData.WeekPList[p].WEEKP + "," + inputData.WeekHList[h].WEEKH + "," + inputData.CropList[j].CROP + "," + inputData.FacList[f].FAC + "]");
                                expr1.Clear();
                                expr2.Clear();
                                counter++;
                            }
                        }
                    }
                }
                endAt = counter;
                Constraint Init_FAC = new Constraint()
                {
                    name = "Init_FAC",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Init_FAC);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                
                //# C3: Packaging quantity depends on amount harvested
                //subject to Tot_packaging { h in WEEKH,k in PROD,q in QUAL,f in FAC}:
                //PACK[h, k, q, f] = COL[h, k, q] * (sum { (p, h, j, k): p <= h and Ccrop[j] = Pcrop[k]}                
                //(Sel[p, h, j, f] * Pod[p, h, j, k] / Weight[k]));
                startAt = counter;
                for (int ixWH = 0; ixWH < nWeekH; ixWH++)
                {
                    double WH = inputData.WeekHList[ixWH].WEEKH;
                    for (int ixP = 0; ixP < nProd; ixP++)
                    {
                        string PR_PROD = inputData.ProdList[ixP].PROD;
                        string PR_Pcrop = inputData.ProdList[ixP].Pcrop;
                        double Weight = inputData.ProdList[ixP].Weight;
                        for (int ixQ = 0; ixQ < nQual; ixQ++)
                        {
                            double QL = inputData.QualList[ixQ].QUAL;
                            for (int ixF = 0; ixF < nFac; ixF++)
                            {
                                string FAC = inputData.FacList[ixF].FAC;
                                int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                double openf = subModelParameters.facOpenList[ixF].open;
                                expr1.AddTerm(1 * openf, PACK[ix]);

                                int ix3 = ixVar.getIx3(ixWH, ixP, ixQ, nWeekH, nProd, nQual);
                                double COL = 1;
                                for (int p = 0; p < nWeekP; p++)
                                {
                                    for (int j = 0; j < nCrop; j++)
                                    {
                                        if (inputData.WeekPList[p].WEEKP <= inputData.WeekHList[ixWH].WEEKH && inputData.CropList[j].CROP.Equals(inputData.ProdList[ixP].Pcrop))
                                        {
                                            int ix4 = ixVar.getIx4(p, ixWH, j, ixP, nWeekP, nWeekH, nCrop, nProd);
                                            double Pod = 1;
                                            double totalPW = COL * Pod / Weight;
                                            int ix1 = ixVar.getIx4(p, ixWH, j, ixF, nWeekP, nWeekH, nCrop, nFac);
                                            expr1.AddTerm(-1 * totalPW, Sel[ix1]);
                                        }
                                    }
                                }
                                gModel.AddConstr(expr1, GRB.EQUAL, 0, "Tot_packaging[" + WH + "," + PR_PROD + "," + QL + "," + FAC + "]");
                                expr1.Clear();
                                counter++;
                            }
                        }
                    }
                }
                endAt = counter;
                Constraint Tot_packaging = new Constraint()
                {
                    name = "Tot_packaging",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Tot_packaging);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;
                

                //# C4: Capacity at the Packing plant
                //subject to Cap_PF { h in WEEKH, f in FAC}: 
                //sum{ k in PROD,q in QUAL}
                //PACK[h, k, q, f] <= PFcap[f];
                startAt = counter;
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.FacList.ForEach(fl =>
                    {
                        int ixF = inputData.FacList.IndexOf(fl);
                        inputData.ProdList.ForEach(pl =>
                        {
                            int ixP = inputData.ProdList.IndexOf(pl);
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                expr1.AddTerm(1, PACK[ix]);
                            });
                        });
                        double PFCap = inputData.FacList[ixF].PFcap;
                        double openf = subModelParameters.facOpenList[ixF].open;
                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, PFCap * openf, "Cap_PF[" + whl.WEEKH + "," + fl.FAC + "]");
                        expr1.Clear();
                        counter++;
                    });
                });
                endAt = counter;
                Constraint Cap_PF = new Constraint()
                {
                    name = "Cap_PF",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Cap_PF);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C5: Removed


                // C6: subject to Production { h in WEEKH,k in PROD,q in QUAL,f in FAC}: 
                //    sum{ i in CUST,r in TRANS}
                //                SC[h, k, q, f, i, r] +
                //    sum{ d in DC,r in TRANS}
                //                    SPD[h, h + T4[f, d, r], k, q, f, d, r] +
                //    sum{ w in WARE,r in TRANS}
                //                    SPW[h, h + T3[f, w, r], k, q, f, w, r] = PACK[h, k, q, f];
                startAt = counter;
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                expr1.AddTerm(-1, PACK[ix]);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        int ix = ixVar.getIx6(ixWH, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                        expr1.AddTerm(1, SC[ix]);
                                    });
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        int T4 = (int)inputData.CtpdList.Find(ctpd =>
                                        ctpd.FAC.Equals(fl.FAC) &&
                                        ctpd.DC.Equals(dl.DIST) &&
                                        ctpd.TRANS.Equals(tl.TRANS)).T4;
                                        WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                        w1l.WEEKH.Equals(whl.WEEKH) &&
                                        w1l.WEEKS.Equals(whl.WEEKH + T4));
                                        int ixW = inputData.Week1List.IndexOf(week1);
                                        int ix = ixVar.getIx6(ixW, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        double opend = subModelParameters.dcOpenList[ixD].open;
                                        double openf = subModelParameters.facOpenList[ixF].open;
                                        expr1.AddTerm(opend, SPD[ix]);
                                    });
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixWa = inputData.WareList.IndexOf(wl);
                                        double T3 = inputData.CtpwList.Find(ctpd =>
                                        ctpd.FAC.Equals(fl.FAC) &&
                                        ctpd.WARE.Equals(wl.WARE) &&
                                        ctpd.TRANS.Equals(tl.TRANS)).T3;

                                        WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                        w1l.WEEKH.Equals(whl.WEEKH) &&
                                        w1l.WEEKS.Equals(whl.WEEKH + T3));

                                        int ixW1 = inputData.Week1List.IndexOf(week1);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixWa, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                        expr1.AddTerm(1, SPW[ix]);
                                    });
                                });
                                gModel.AddConstr(expr1, GRB.EQUAL, 0, "Production[" + whl.WEEKH + "," + pl.PROD + "," + ql.QUAL + "," + fl.FAC + "]");
                                expr1.Clear();
                                counter++;
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint Production = new Constraint()
                {
                    name = "Production",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Production);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //C7: subject to SC_Prod { t in WEEKS,k in PROD,q in QUAL}:
                //      sum{ ,f in FAC, i in CUST,r in TRANS: t > last(WEEKH)}
                //              SC[t, k, q, f, i, r] <= 0;
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    double lastWeekH = inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_end")).Value; //inputData.WeekHList.LastOrDefault().WEEKH;
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
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
                                        if (wsl.WEEKS > lastWeekH)
                                        {
                                            int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                            expr1.AddTerm(1, SC[ix]);
                                        }
                                    });
                                });
                            });
                            gModel.AddConstr(expr1, GRB.LESS_EQUAL, 0, "SC_Prod[" + wsl.WEEKS + "," + pl.PROD + "," + ql.QUAL + "]");
                            expr1.Clear();
                            counter++;
                        });
                    });
                });
                endAt = counter;
                Constraint SC_Prod = new Constraint()
                {
                    name = "SC_Prod",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SC_Prod);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C8: subject to SPD_Prod { (h, t) in WEEK1}: 
                //    sum { k in PROD,q in QUAL,f in FAC,d in DC,r in TRANS: t < h + T4[f, d, r]}
                //        SPD[h, t, k, q, f, d, r] +
                //    sum { k in PROD,q in QUAL,f in FAC,d in DC,r in TRANS: t > h + T4[f, d, r]}
                //        SPD[h, t, k, q, f, d, r] = 0;
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T4 = (int)inputData.CtpdList.Find(ctpd =>
                                        ctpd.FAC.Equals(fl.FAC) &&
                                        ctpd.DC.Equals(dl.DIST) &&
                                        ctpd.TRANS.Equals(tl.TRANS)).T4;
                                        if (w1l.WEEKS < (w1l.WEEKH + T4))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                            double opend = subModelParameters.dcOpenList[ixD].open;
                                            expr1.AddTerm(opend, SPD[ix]);
                                        }
                                        else if (w1l.WEEKS > (w1l.WEEKH + T4))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                            double opend = subModelParameters.dcOpenList[ixD].open;
                                            expr1.AddTerm(opend, SPD[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                    gModel.AddConstr(expr1, GRB.EQUAL, 0, "SPD_Prod[" + w1l.WEEKH + "," + w1l.WEEKS + "]");
                    expr1.Clear();
                    counter++;
                });
                endAt = counter;
                Constraint SPD_Prod = new Constraint()
                {
                    name = "SPD_Prod",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SPD_Prod);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //C9: subject to SWD_Prod { (h, t) in WEEK1}: 
                //    sum{ w in WARE,k in PROD,q in QUAL,f in FAC,r in TRANS: t < h + T3[f, w, r]}
                //        SPW[h, t, k, q, f, w, r] +
                //    sum{ w in WARE,k in PROD,q in QUAL,f in FAC,r in TRANS: t > h + T3[f, w, r]}
                //      SPW[h, t, k, q, f, w, r] = 0;
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.WareList.ForEach(wl =>
                    {
                        int ixW = inputData.WareList.IndexOf(wl);
                        inputData.ProdList.ForEach(pl =>
                        {
                            int ixP = inputData.ProdList.IndexOf(pl);
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T3 = (int)inputData.CtpwList.Find(ctpd =>
                                        ctpd.FAC.Equals(fl.FAC) &&
                                        ctpd.WARE.Equals(wl.WARE) &&
                                        ctpd.TRANS.Equals(tl.TRANS)).T3;
                                        if (w1l.WEEKS < (w1l.WEEKH + T3))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                            expr1.AddTerm(1, SPW[ix]);
                                        }
                                        else if (w1l.WEEKS > (w1l.WEEKH + T3))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                            expr1.AddTerm(1, SPW[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                    gModel.AddConstr(expr1, GRB.EQUAL, 0, "SWD_Prod[" + w1l.WEEKH + "," + w1l.WEEKS + "]");
                    expr1.Clear();
                    counter++;
                });
                endAt = counter;
                Constraint SWD_Prod = new Constraint()
                {
                    name = "SWD_Prod",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SWD_Prod);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //C10: subject to Initial_hold { h in WEEKH,k in PROD,q in QUAL,w in WARE}: 
                //   Invw[h, h, k, q, w] = (if q = 3 then Z[h, k, w])
                //      +sum{ f in FAC,r in TRANS}
                //           SPW[h, h, k, q, f, w, r]
                //      -sum{ i in CUST,r in TRANS}
                //           SW[h, h + T2[w, i, r], k, q, w, i, r]
                //      -sum{ d in DC,r in TRANS}
                //           SWD[h, h + T5[w, d, r], k, q, w, d, r];
                startAt = counter;
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                        w1l.WEEKH.Equals(whl.WEEKH) &&
                                        w1l.WEEKS.Equals(whl.WEEKH));
                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                expr1.AddTerm(1, Invw[ix]);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                        expr2.AddTerm(1, SPW[ix]);
                                    });
                                });
                                inputData.CustList.ForEach(cl =>
                                {
                                    int ixC = inputData.CustList.IndexOf(cl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T2 = (int)inputData.CtwList.Find(ctw =>
                                        ctw.WARE.Equals(wl.WARE) &&
                                        ctw.CUST.Equals(cl.CUST) &&
                                        ctw.TRANS.Equals(tl.TRANS)).T2;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                        expr2.AddTerm(-1, SW[ix]);
                                    });
                                });
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T5 = (int)inputData.CtwdList.Find(ctw =>
                                        ctw.WARE.Equals(wl.WARE) &&
                                        ctw.DC.Equals(dl.DIST) &&
                                        ctw.TRANS.Equals(tl.TRANS)).T5;
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        expr2.AddTerm(-1, SWD[ix]);
                                    });
                                });
                                gModel.AddConstr(expr1, GRB.EQUAL, expr2, "Initial_hold[" + whl.WEEKH + "," + pl.PROD + "," + ql.QUAL + "," + wl.WARE + "]");
                                expr1.Clear();
                                expr2.Clear();
                                counter++;
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint Initial_hold = new Constraint()
                {
                    name = "Initial_hold",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Initial_hold);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C11: subject to In_hold { (h, t) in WEEK1}: 
                //     sum{ i in CUST,k in PROD,q in QUAL,w in WARE,r in TRANS: t < h + T2[w, i, r]}
                //                SW[h, t, k, q, w, i, r]
                //    +sum{ k in PROD,q in QUAL,w in WARE,d in DC,r in TRANS: t < h + T5[w, d, r]}
                //                SWD[h, t, k, q, w, d, r] = 0;
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.CustList.ForEach(cl =>
                    {
                        int ixC = inputData.CustList.IndexOf(cl);
                        inputData.ProdList.ForEach(pl =>
                        {
                            int ixP = inputData.ProdList.IndexOf(pl);
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T2 = (int)inputData.CtwList.Find(ctpd =>
                                        ctpd.CUST.Equals(cl.CUST) &&
                                        ctpd.WARE.Equals(wl.WARE) &&
                                        ctpd.TRANS.Equals(tl.TRANS)).T2;
                                        if (w1l.WEEKS < (w1l.WEEKH + T2))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                            expr1.AddTerm(1, SW[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });

                    //sum{ k in PROD,q in QUAL,w in WARE,d in DC,r in TRANS: t < h + T5[w, d, r]}
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T5 = (int)inputData.CtwdList.Find(ctwd =>
                                        ctwd.WARE.Equals(wl.WARE) &&
                                        ctwd.DC.Equals(dl.DIST) &&
                                        ctwd.TRANS.Equals(tl.TRANS)).T5;
                                        if (w1l.WEEKS < (w1l.WEEKH + T5))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                            expr1.AddTerm(1, SWD[ix]);
                                        }
                                    });
                                });
                            });
                        });
                    });
                    gModel.AddConstr(expr1, GRB.EQUAL, 0, "In_hold[" + w1l.WEEKH + "," + w1l.WEEKS + "]");
                    expr1.Clear();
                    counter++;
                });
                endAt = counter;
                Constraint In_hold = new Constraint()
                {
                    name = "In_hold",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(In_hold);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C12: subject to Inventw { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE: t > h}: 
                //     Invw[h, t, k, q, w] = Invw[h, t - 1, k, q, w]
                //     + sum{ f in FAC,r in TRANS}
                //                 SPW[h, t, k, q, f, w, r]
                //     - sum{ i in CUST,r in TRANS} (if (h - T2[w, i, r] + 2)>= t then SW[h, t + T2[w, i, r], k, q, w, i, r])
                //     - sum{ d in DC,r in TRANS} (if (h - T5[w, d, r] + 2)>= t then SWD[h, t + T5[w, d, r], k, q, w, d, r]);
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double Pallet = pl.Pallet;
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                if (w1l.WEEKS > w1l.WEEKH)
                                {
                                    int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                    expr1.AddTerm(1, Invw[ix]);
                                    WEEK1Model week1 = inputData.Week1List.Find(w1l2 =>
                                        w1l2.WEEKH.Equals(w1l.WEEKH) &&
                                        w1l2.WEEKS.Equals(w1l.WEEKS - 1));
                                    int ixW12 = inputData.Week1List.IndexOf(week1);
                                    ix = ixVar.getIx4(ixW12, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                    expr2.AddTerm(1, Invw[ix]);
                                    inputData.FacList.ForEach(fl =>
                                    {
                                        int ixF = inputData.FacList.IndexOf(fl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                            expr2.AddTerm(1, SPW[ix]);
                                        });
                                    });
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int T2 = (int)inputData.CtwList.Find(ctpd =>
                                                    ctpd.CUST.Equals(cl.CUST) &&
                                                    ctpd.WARE.Equals(wl.WARE) &&
                                                    ctpd.TRANS.Equals(tl.TRANS)).T2;
                                            if ((w1l.WEEKH - T2 + 2) >= w1l.WEEKS)
                                            {
                                                ix = ixVar.getIx6(ixW12, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                                expr2.AddTerm(-1, SW[ix]);
                                            }
                                        });
                                    });
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int T5 = (int)inputData.CtwdList.Find(ctwd =>
                                                    ctwd.DC.Equals(dl.DIST) &&
                                                    ctwd.WARE.Equals(wl.WARE) &&
                                                    ctwd.TRANS.Equals(tl.TRANS)).T5;
                                            if ((w1l.WEEKH - T5 + 2) >= w1l.WEEKS)
                                            {
                                                ix = ixVar.getIx6(ixW12, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                                expr2.AddTerm(-1, SWD[ix]);
                                            }
                                        });
                                    });
                                    gModel.AddConstr(expr1, GRB.EQUAL, expr2, "Inventw[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + wl.WARE + "]");
                                    expr1.Clear();
                                    expr2.Clear();
                                    counter++;
                                }
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint Inventw = new Constraint()
                {
                    name = "Inventw",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Inventw);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C13: subject to Cap_warehouse { w in WARE, t in WEEKS}: 
                //     sum{ k in PROD,q in QUAL,h in WEEKH: t >= h >= t - SL[k]}
                //     Invw[h, t, k, q, w] / Pallet[k] <= Wcap[w];
                startAt = counter;
                inputData.WareList.ForEach(wl =>
                {
                    int ixW = inputData.WareList.IndexOf(wl);
                    inputData.WeekSList.ForEach(wsl =>
                    {
                        int ixWS = inputData.WeekSList.IndexOf(wsl);
                        inputData.ProdList.ForEach(pl =>
                        {
                            int ixP = inputData.ProdList.IndexOf(pl);
                            double Pallet = pl.Pallet;
                            inputData.QualList.ForEach(ql =>
                            {
                                int ixQ = inputData.QualList.IndexOf(ql);
                                inputData.WeekHList.ForEach(whl =>
                                {
                                    int ixWH = inputData.WeekHList.IndexOf(whl);
                                    double SL = inputData.ProdList.Find(pl2 =>
                                    pl2.PROD.Equals(pl.PROD)).SL;
                                    if (wsl.WEEKS >= whl.WEEKH && whl.WEEKH >= (wsl.WEEKS - SL))
                                    {
                                        WEEK1Model week1 = inputData.Week1List.Find(w1l2 =>
                                                w1l2.WEEKH.Equals(whl.WEEKH) &&
                                                w1l2.WEEKS.Equals(wsl.WEEKS));
                                        int ixW1 = inputData.Week1List.IndexOf(week1);
                                        int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                        expr1.AddTerm(1 / Pallet, Invw[ix]);
                                    }
                                });
                            });
                        });
                        double Wcap = inputData.WareList.Find(wl2 => wl2.WARE.Equals(wl.WARE)).Wcap;
                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, Wcap, "Cap_warehouse[" + wl.WARE + "," + wsl.WEEKS + "]");
                        expr1.Clear();
                        counter++;
                    });
                });
                endAt = counter;
                Constraint Cap_warehouse = new Constraint()
                {
                    name = "Cap_warehouse",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Cap_warehouse);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C14: subject to Initial_DC { h in WEEKH,k in PROD,q in QUAL,d in DC}: 
                //         Invd[h, h, k, q, d] =
                //         sum{ f in FAC,r in TRANS}  SPD[h, h, k, q, f, d, r]
                //         - sum{ i in CUST,r in TRANS}  SD[h, h, k, q, d, i, r]
                //         + sum{ w in WARE,r in TRANS}  SWD[h, h, k, q, w, d, r];
                startAt = counter;
                inputData.WeekHList.ForEach(whl =>
                {
                    int ixWH = inputData.WeekHList.IndexOf(whl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                WEEK1Model week1 = inputData.Week1List.Find(w1l2 =>
                                                w1l2.WEEKH.Equals(whl.WEEKH) &&
                                                w1l2.WEEKS.Equals(whl.WEEKH));
                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                expr1.AddTerm(1, Invd[ix]);

                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                        double opend = subModelParameters.dcOpenList[ixD].open;
                                        double openf = subModelParameters.facOpenList[ixF].open;
                                        expr2.AddTerm(opend, SPD[ix]);
                                    });
                                });
                                inputData.CustList.ForEach(cl =>
                                {
                                    int ixC = inputData.CustList.IndexOf(cl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                        double opend = subModelParameters.dcOpenList[ixD].open;
                                        expr2.AddTerm(-1 * opend, SD[ix]);
                                    });
                                });
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                        expr2.AddTerm(1, SWD[ix]);
                                    });
                                });
                                gModel.AddConstr(expr1, GRB.EQUAL, expr2, "Initial_DC[" + whl.WEEKH + "," + pl.PROD + "," + ql.QUAL + "," + dl.DIST + "]");
                                expr1.Clear();
                                expr2.Clear();
                                counter++;
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint Initial_DC = new Constraint()
                {
                    name = "Initial_DC",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Initial_DC);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C15: subject to Inv_DC { (h, t) in WEEK1,k in PROD,q in QUAL,d in DC: t > h}:
                //           Invd[h, t, k, q, d] = Invd[h, t - 1, k, q, d]
                //         + sum{ f in FAC,r in TRANS}
                //                       SPD[h, t, k, q, f, d, r]
                //         - sum{ i in CUST,r in TRANS}
                //                       SD[h, t, k, q, d, i, r]
                //         + sum{ w in WARE,r in TRANS}
                //                       SWD[h, t, k, q, w, d, r];
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                if (w1l.WEEKS > w1l.WEEKH)
                                {
                                    int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                    expr1.AddTerm(1, Invd[ix]);
                                    WEEK1Model week1 = inputData.Week1List.Find(w1l2 =>
                                                w1l2.WEEKH.Equals(w1l.WEEKH) &&
                                                w1l2.WEEKS.Equals(w1l.WEEKS - 1));
                                    int ixW12 = inputData.Week1List.IndexOf(week1);
                                    ix = ixVar.getIx4(ixW12, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                    expr2.AddTerm(1, Invd[ix]);
                                    inputData.FacList.ForEach(fl =>
                                    {
                                        int ixF = inputData.FacList.IndexOf(fl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                            double opend = subModelParameters.dcOpenList[ixD].open;
                                            double openf = subModelParameters.facOpenList[ixF].open;
                                            expr2.AddTerm(opend, SPD[ix]);
                                        });
                                    });
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                            double opend = subModelParameters.dcOpenList[ixD].open;
                                            expr2.AddTerm(-1 * opend, SD[ix]);
                                        });
                                    });
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixW = inputData.WareList.IndexOf(wl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                            expr2.AddTerm(1, SWD[ix]);
                                        });
                                    });
                                    gModel.AddConstr(expr1, GRB.EQUAL, expr2, "Inv_DC[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + dl.DIST + "]");
                                    expr1.Clear();
                                    expr2.Clear();
                                    counter++;
                                }
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint Inv_DC = new Constraint()
                {
                    name = "Inv_DC",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Inv_DC);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C16: subject to SD_Inv { (h, t) in WEEK1, k in PROD,q in QUAL}:
                //          sum{ w in WARE,r in TRANS,d in DC: t < h + T5[w, d, r]}
                //                    SWD[h, t, k, q, w, d, r]
                //        + sum{ f in FAC,r in TRANS,w in WARE: t < h + T3[f, w, r]}
                //                    SPW[h, t, k, q, f, w, r]
                //        + sum{ f in FAC,r in TRANS,d in DC: t < h + T4[f, d, r]}
                //                    SPD[h, t, k, q, f, d, r] = 0;
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        int T5 = (int)inputData.CtwdList.Find(ctwd =>
                                                    ctwd.DC.Equals(dl.DIST) &&
                                                    ctwd.WARE.Equals(wl.WARE) &&
                                                    ctwd.TRANS.Equals(tl.TRANS)).T5;
                                        if (w1l.WEEKS < (w1l.WEEKH + T5))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                            expr1.AddTerm(1, SWD[ix]);
                                        }
                                    });
                                });
                            });
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixW = inputData.WareList.IndexOf(wl);
                                        int T3 = (int)inputData.CtpwList.Find(ctpd =>
                                            ctpd.FAC.Equals(fl.FAC) &&
                                            ctpd.WARE.Equals(wl.WARE) &&
                                            ctpd.TRANS.Equals(tl.TRANS)).T3;
                                        if (w1l.WEEKS < (w1l.WEEKH + T3))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                            expr1.AddTerm(1, SPW[ix]);
                                        }
                                    });
                                });
                            });
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        int T4 = (int)inputData.CtpdList.Find(ctpd =>
                                                ctpd.FAC.Equals(fl.FAC) &&
                                                ctpd.DC.Equals(dl.DIST) &&
                                                ctpd.TRANS.Equals(tl.TRANS)).T4;
                                        if (w1l.WEEKS < (w1l.WEEKH + T4))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                            double opend = subModelParameters.dcOpenList[ixD].open;
                                            double openf = subModelParameters.facOpenList[ixF].open;
                                            expr1.AddTerm(opend, SPD[ix]);
                                        }
                                    });
                                });
                            });
                            gModel.AddConstr(expr1, GRB.EQUAL, 0, "SD_Inv[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "]");
                            expr1.Clear();
                            counter++;
                        });
                    });
                });
                endAt = counter;
                Constraint SD_Inv = new Constraint()
                {
                    name = "SD_Inv",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SD_Inv);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C17: subject to SPD_Inv { (h, t) in WEEK1,k in PROD,q in QUAL}: 
                //         sum{ d in DC,f in FAC,r in TRANS: t > h + T4[f, d, r]}
                //                    SPD[h, t, k, q, f, d, r]
                //        + sum{ w in WARE,f in FAC,r in TRANS: t > h + T3[f, w, r]}
                //                   SPW[h, t, k, q, f, w, r] = 0;
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.QualList.ForEach(ql =>
                        {
                            int ixQ = inputData.QualList.IndexOf(ql);
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T4 = (int)inputData.CtpdList.Find(ctpd =>
                                                ctpd.FAC.Equals(fl.FAC) &&
                                                ctpd.DC.Equals(dl.DIST) &&
                                                ctpd.TRANS.Equals(tl.TRANS)).T4;
                                        if (w1l.WEEKS > (w1l.WEEKH + T4))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                            double opend = subModelParameters.dcOpenList[ixD].open;
                                            double openf = subModelParameters.facOpenList[ixF].open;
                                            expr1.AddTerm(opend, SPD[ix]);
                                        }
                                    });
                                });
                            });

                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.TransList.ForEach(tl =>
                                    {
                                        int ixT = inputData.TransList.IndexOf(tl);
                                        int T3 = (int)inputData.CtpwList.Find(ctpd =>
                                                ctpd.FAC.Equals(fl.FAC) &&
                                                ctpd.WARE.Equals(wl.WARE) &&
                                                ctpd.TRANS.Equals(tl.TRANS)).T3;
                                        if (w1l.WEEKS > (w1l.WEEKH + T3))
                                        {
                                            int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                            expr1.AddTerm(1, SPW[ix]);
                                        }
                                    });
                                });
                            });
                            gModel.AddConstr(expr1, GRB.EQUAL, 0, "SPD_Inv[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "]");
                            expr1.Clear();
                            counter++;
                        });
                    });
                });
                endAt = counter;
                Constraint SPD_Inv = new Constraint()
                {
                    name = "SPD_Inv",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SPD_Inv);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                // C18: Capacity at the DC
                //subject to Cap_DC {d in DC, t in WEEKS}: 
                //sum{h in WEEKH,k in PROD,q in QUAL: t>= h >= t-SL[k]}
                //Invd[h,t,k,q,d]/Pallet[k]<= Dcap[d];
                startAt = counter;
                inputData.DistList.ForEach(dl =>
                {
                    int ixD = inputData.DistList.IndexOf(dl);
                    double Dcap = dl.Dcap;
                    inputData.WeekSList.ForEach(wsl =>
                    {
                        int ixWS = inputData.WeekSList.IndexOf(wsl);
                        inputData.WeekHList.ForEach(whl =>
                        {
                            int ixWH = inputData.WeekHList.IndexOf(whl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                double SL = pl.SL;
                                double Pallet = pl.Pallet;
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    if (wsl.WEEKS >= whl.WEEKH && whl.WEEKH >= (wsl.WEEKS - SL))
                                    {
                                        double totalP = 1 / Pallet;
                                        WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                        w1l.WEEKH.Equals(whl.WEEKH) &&
                                        w1l.WEEKS.Equals(wsl.WEEKS)
                                        );
                                        int ixW1 = inputData.Week1List.IndexOf(week1);
                                        int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                        expr1.AddTerm(totalP, Invd[ix]);
                                    }
                                });
                            });
                        });
                        double opend = subModelParameters.dcOpenList[ixD].open;
                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, Dcap * opend, "Cap_DC[" + dl.DIST + "," + wsl.WEEKS + "]");
                        expr1.Clear();
                        counter++;
                    });
                });
                endAt = counter;
                Constraint Cap_DC = new Constraint()
                {
                    name = "Cap_DC",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Cap_DC);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C19: Customer demand is met either through shipping from field 
                //# to customer or from warehouse to customer
                //subject to Demand {t in WEEKS,k in PROD,i in CUST}: 
                //sum{f in FAC,r in TRANS,q in QUAL: q<=Qmin[i]} SC[t,k,q,f,i,r]
                //  +sum {w in WARE,r in TRANS,h in WEEKH,q in QUAL
                //  :t>= h >= t-SL[k] and q<=Qmin[i]} SW[h,t,k,q,w,i,r]
                //  +sum {d in DC,r in TRANS,h in WEEKH,q in QUAL:t>= h >= t-SL[k] 
                //  and r='TM1' and q<=Qmin[i]} SD[h,t,k,q,d,i,r]<=Dem[t,k,i];               
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double Qmin = cl.Qmin;
                            double maxDem = inputData.DemList[ix].maxDem;
                            double weight = inputData.ProdList[ixP].Weight;
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.QualList.ForEach(ql =>
                                    {
                                        int ixQ = inputData.QualList.IndexOf(ql);
                                        if (ql.QUAL <= Qmin)
                                        {
                                            int ix6 = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                            expr1.AddTerm(1, SC[ix6]);
                                        }
                                    });
                                });
                            });
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.WeekHList.ForEach(whl =>
                                    {
                                        int ixWH = inputData.WeekHList.IndexOf(whl);
                                        inputData.QualList.ForEach(ql =>
                                        {
                                            int ixQ = inputData.QualList.IndexOf(ql);
                                            if (wsl.WEEKS >= whl.WEEKH && (whl.WEEKH >= wsl.WEEKS - SL) && ql.QUAL <= Qmin)
                                            {
                                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                                w1l.WEEKH.Equals(whl.WEEKH) &&
                                                w1l.WEEKS.Equals(wsl.WEEKS)
                                                );
                                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                                int ix6 = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                                expr1.AddTerm(1, SW[ix6]);
                                            }
                                        });
                                    });
                                });
                            });

                            //+sum {d in DC,r in TRANS,h in WEEKH,q in QUAL
                            //:t>= h >= t-SL[k] and r='TM1' and q<=Qmin[i]}
                            //SD[h,t,k,q,d,i,r]
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    string r = inputData.TransList.FirstOrDefault().TRANS;
                                    inputData.WeekHList.ForEach(whl =>
                                    {
                                        int ixWH = inputData.WeekHList.IndexOf(whl);
                                        inputData.QualList.ForEach(ql =>
                                        {
                                            int ixQ = inputData.QualList.IndexOf(ql);
                                            if (wsl.WEEKS >= whl.WEEKH &&
                                            (whl.WEEKH >= wsl.WEEKS - SL) &&
                                            tl.TRANS.Equals(r)
                                            && ql.QUAL <= Qmin)
                                            {
                                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                                w1l.WEEKH.Equals(whl.WEEKH) &&
                                                w1l.WEEKS.Equals(wsl.WEEKS));
                                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                                int ix6 = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                                double opend = subModelParameters.dcOpenList[ixD].open;
                                                expr1.AddTerm(opend, SD[ix6]);
                                            }
                                        });
                                    });
                                });
                            });
                            expr1.AddTerm(1, Z[ix]);
                            gModel.AddConstr(expr1, GRB.LESS_EQUAL, maxDem / weight, "Demand[" + wsl.WEEKS + "," + pl.PROD + "," + cl.CUST + "]");
                            expr1.Clear();
                            counter++;
                        });
                    });
                });
                endAt = counter;
                Constraint Demand = new Constraint()
                {
                    name = "Demand",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(Demand);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;


                /*
                // CHECK Cannot buy more than demand in order to avoid arbitrage
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double contractDem = inputData.DemList[ix].contractDem;

                            gModel.AddConstr(Z[ix], GRB.LESS_EQUAL, contractDem / pl.Weight, "MinPurchasing[" + wsl.WEEKS + "," + pl.PROD + "," + cl.CUST + "]");
                            counter++;

                        });
                    });
                });
                endAt = counter;
                Constraint MinPurchasing = new Constraint()
                {
                    name = "MinPurchasing",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(MinPurchasing);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;
                */


                // C20: subject to ContractDemand {t in WEEKS,k in PROD,i in CUST}: 
                //sum{f in FAC,r in TRANS,q in QUAL: q<=Qmin[i]} SC[t,k,q,f,i,r]
                //+sum {w in WARE,r in TRANS,h in WEEKH,q in QUAL:t>= h >= t-SL[k] and q<=Qmin[i]} SW[h,t,k,q,w,i,r]
                //+sum {d in DC,r in TRANS,h in WEEKH,q in QUAL:t>= h >= t-SL[k]
                //and r='TM1' and q<=Qmin[i]} SD[h,t,k,q,d,i,r]>=Dem2[t,k,i];
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
                        inputData.CustList.ForEach(cl =>
                        {
                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix3 = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double Qmin = cl.Qmin;
                            double contractDem = inputData.DemList[ix3].contractDem;
                            if (wsl.WEEKS <= inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_start")).Value || wsl.WEEKS > inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_end")).Value)  //////
                            {
                                contractDem = 0;
                            }
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.QualList.ForEach(ql =>
                                    {
                                        int ixQ = inputData.QualList.IndexOf(ql);
                                        if (ql.QUAL <= Qmin)
                                        {
                                            int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                            expr1.AddTerm(1, SC[ix]);
                                        }
                                    });
                                });
                            });
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.WeekHList.ForEach(whl =>
                                    {
                                        int ixWH = inputData.WeekHList.IndexOf(whl);
                                        inputData.QualList.ForEach(ql =>
                                        {
                                            int ixQ = inputData.QualList.IndexOf(ql);
                                            if (wsl.WEEKS >= whl.WEEKH && (whl.WEEKH >= wsl.WEEKS - SL) && ql.QUAL <= Qmin)
                                            {
                                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                                w1l.WEEKH.Equals(whl.WEEKH) &&
                                                w1l.WEEKS.Equals(wsl.WEEKS));
                                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                                expr1.AddTerm(1, SW[ix]);
                                            }
                                        });
                                    });
                                });
                            });
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    string r = inputData.TransList.FirstOrDefault().TRANS;
                                    inputData.WeekHList.ForEach(whl =>
                                    {
                                        int ixWH = inputData.WeekHList.IndexOf(whl);
                                        inputData.QualList.ForEach(ql =>
                                        {
                                            int ixQ = inputData.QualList.IndexOf(ql);
                                            if (wsl.WEEKS >= whl.WEEKH &&
                                            (whl.WEEKH >= wsl.WEEKS - SL) && ql.QUAL <= Qmin)
                                            {
                                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                                w1l.WEEKH.Equals(whl.WEEKH) &&
                                                w1l.WEEKS.Equals(wsl.WEEKS));
                                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                                double opend = subModelParameters.dcOpenList[ixD].open;
                                                expr1.AddTerm(opend, SD[ix]);
                                            }
                                        });
                                    });
                                });
                            });
                            expr1.AddTerm(1, Z[ix3]);
                            if (!useContract) { contractDem = 0; }
                            gModel.AddConstr(expr1, GRB.GREATER_EQUAL, contractDem / pl.Weight, "ContractDemand[" + wsl.WEEKS + "," + pl.PROD + "," + cl.CUST + "]");
                            expr1.Clear();
                            counter++;
                        });
                    });
                });
                endAt = counter;
                Constraint ContractDemand = new Constraint()
                {
                    name = "ContractDemand",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(ContractDemand);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C21: Shipment of products is subject to transportation selection
                //subject to SC_TC {t in WEEKS,k in PROD,q in QUAL,f in FAC,i in CUST,r in TRANS}:
                //SC[t,k,q,f,i,r]<=BM*TC[t,k,f,i,r];
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
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
                                        int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                        int ix2 = ixVar.getIx5(ixWS, ixP, ixF, ixC, ixT, nWeekS, nProd, nFac, nCust, nTrans);
                                        expr1.AddTerm(1, SC[ix]);
                                        expr2.AddTerm(BM, TC[ix2]);
                                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, expr2, "SC_TC[" + wsl.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + fl.FAC + "," + cl.CUST + "," + tl.TRANS + "]");
                                        expr1.Clear();
                                        expr2.Clear();
                                        counter++;
                                    });
                                });
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint SC_TC = new Constraint()
                {
                    name = "SC_TC",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SC_TC);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C22: Shipment of products is subject to transportation selection
                //subject to SW_TW { (h, t) in WEEK1,k in PROD,q in QUAL,w in WARE,i in CUST,r in TRANS}
                //: SW[h, t, k, q, w, i, r] <= BM * TW[t, k, w, i, r];
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
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
                                        WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(w1l.WEEKS));
                                        int ixWS = inputData.WeekSList.IndexOf(weekS);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                        int ix2 = ixVar.getIx5(ixWS, ixP, ixW, ixC, ixT, nWeekS, nProd, nWare, nCust, nTrans);
                                        expr1.AddTerm(1, SW[ix]);
                                        expr2.AddTerm(BM, TW[ix2]);
                                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, expr2, "SW_TW[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + wl.WARE + "," + cl.CUST + "," + tl.TRANS + "]");
                                        expr1.Clear();
                                        expr2.Clear();
                                        counter++;
                                    });
                                });
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint SW_TW = new Constraint()
                {
                    name = "SW_TW",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SW_TW);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C23: Shipment of products is subject to transportation selection
                //subject to SD_TD { (h, t) in WEEK1,k in PROD,q in QUAL,d in DC,i in CUST,r in TRANS}
                //: SD[h, t, k, q, d, i, r] <= BM * TD[t, k, d, i, r];
                startAt = counter;
                inputData.Week1List.ForEach(w1l =>
                {
                    int ixW1 = inputData.Week1List.IndexOf(w1l);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
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
                                        WEEKSModel weekS = inputData.WeekSList.Find(wsl => wsl.WEEKS.Equals(w1l.WEEKS));
                                        int ixWS = inputData.WeekSList.IndexOf(weekS);
                                        int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                        int ix2 = ixVar.getIx5(ixWS, ixP, ixD, ixC, ixT, nWeekS, nProd, nDC, nCust, nTrans);
                                        expr1.AddTerm(1, SD[ix]);
                                        double opend = subModelParameters.dcOpenList[ixD].open;
                                        expr2.AddTerm(BM * opend, TD[ix2]);
                                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, expr2, "SD_TD[" + w1l.WEEKH + "," + w1l.WEEKS + "," + pl.PROD + "," + ql.QUAL + "," + dl.DIST + "," + cl.CUST + "," + tl.TRANS + "]");
                                        expr1.Clear();
                                        expr2.Clear();
                                        counter++;
                                    });
                                });
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint SD_TD = new Constraint()
                {
                    name = "SD_TD",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(SD_TD);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C24: Cycle time restriction
                //subject to TC_Time { t in WEEKS,k in PROD,f in FAC,i in CUST,r in TRANS}
                //: Time[f, i, r] * TC[t, k, f, i, r] <= LT[i];
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.FacList.ForEach(fl =>
                        {
                            int ixF = inputData.FacList.IndexOf(fl);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                double LT = cl.LT;
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    double Time = inputData.CtList.Find(ctl =>
                                    ctl.FAC.Equals(fl.FAC) &&
                                    ctl.CUST.Equals(cl.CUST) &&
                                    ctl.TRANS.Equals(tl.TRANS)).Time;
                                    int ix = ixVar.getIx5(ixWS, ixP, ixF, ixC, ixT, nWeekS, nProd, nFac, nCust, nTrans);
                                    expr1.AddTerm(Time, TC[ix]);
                                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, LT, "TC_Time[" + wsl.WEEKS + "," + pl.PROD + "," + fl.FAC + "," + cl.CUST + "," + tl.TRANS + "]");
                                    expr1.Clear();
                                    counter++;
                                });
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint TC_Time = new Constraint()
                {
                    name = "TC_Time",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(TC_Time);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C25: Cycle time restriction
                //subject to TW_Time { t in WEEKS,k in PROD,w in WARE,i in CUST,r in TRANS}
                //: TimeW[w, i, r] * TW[t, k, w, i, r] <= LT[i];
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.WareList.ForEach(wl =>
                        {
                            int ixW = inputData.WareList.IndexOf(wl);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                double LT = cl.LT;
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    double TimeW = inputData.CtwList.Find(ctl =>
                                    ctl.WARE.Equals(wl.WARE) &&
                                    ctl.CUST.Equals(cl.CUST) &&
                                    ctl.TRANS.Equals(tl.TRANS)).TimeW;
                                    int ix = ixVar.getIx5(ixWS, ixP, ixW, ixC, ixT, nWeekS, nProd, nWare, nCust, nTrans);
                                    expr1.AddTerm(TimeW, TW[ix]);
                                    gModel.AddConstr(expr1, GRB.LESS_EQUAL, LT, "TW_Time[" + wsl.WEEKS + "," + pl.PROD + "," + wl.WARE + "," + cl.CUST + "," + tl.TRANS + "]");
                                    expr1.Clear();
                                    counter++;
                                });
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint TW_Time = new Constraint()
                {
                    name = "TW_Time",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(TW_Time);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;

                //# C26: Cycle time restriction
                //subject to TD_Time { t in WEEKS,k in PROD,d in DC,i in CUST,r in TRANS
                //: r = 'TM1'}
                //: TimeD[d, i, r] * TD[t, k, d, i, r] <= LT[i];
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        inputData.DistList.ForEach(dl =>
                        {
                            int ixD = inputData.DistList.IndexOf(dl);
                            inputData.CustList.ForEach(cl =>
                            {
                                int ixC = inputData.CustList.IndexOf(cl);
                                double LT = cl.LT;
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    string r = inputData.TransList.FirstOrDefault().TRANS;
                                    double TimeD = inputData.CtdList.Find(ctl =>
                                    ctl.DC.Equals(dl.DIST) &&
                                    ctl.CUST.Equals(cl.CUST) &&
                                    ctl.TRANS.Equals(tl.TRANS)).TimeD;
                                    int ix = ixVar.getIx5(ixWS, ixP, ixD, ixC, ixT, nWeekS, nProd, nDC, nCust, nTrans);
                                    if (tl.TRANS.Equals(r))
                                    {
                                        expr1.AddTerm(TimeD, TD[ix]);
                                        gModel.AddConstr(expr1, GRB.LESS_EQUAL, LT, "TD_Time[" + wsl.WEEKS + "," + pl.PROD + "," + dl.DIST + "," + cl.CUST + "," + tl.TRANS + "]");
                                        expr1.Clear();
                                        counter++;
                                    }
                                });
                            });
                        });
                    });
                });
                endAt = counter;
                Constraint TD_Time = new Constraint()
                {
                    name = "TD_Time",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(TD_Time);
                utime = DateTime.Now;
                Console.Write("\t\tOK.\t" + Math.Round((utime - time0).TotalSeconds, 2) + "\n");

                // C27: prevent going thorugh SD or SW
                expr1.Clear();
                for (int i = 0; i < SW.Length; i++)
                {
                    expr1.AddTerm(1, SW[i]);
                }
                gModel.AddConstr(expr1, GRB.EQUAL, 0, "c27_SW_equal_0");
                expr1.Clear();
                counter++;
                for (int i = 0; i < SD.Length; i++)
                {
                    expr1.AddTerm(1, SD[i]);
                }
                gModel.AddConstr(expr1, GRB.EQUAL, 0, "c27_SD_equal_0");
                expr1.Clear();
                counter++;

                // C28: For determining the value of the auxillary variable EXCESS (determines how many packaging units we are shipping beyond the contract demand)
                startAt = counter;
                inputData.WeekSList.ForEach(wsl =>
                {
                    int ixWS = inputData.WeekSList.IndexOf(wsl);
                    inputData.ProdList.ForEach(pl =>
                    {
                        int ixP = inputData.ProdList.IndexOf(pl);
                        double SL = pl.SL;
                        inputData.CustList.ForEach(cl =>
                        {

                            int ixC = inputData.CustList.IndexOf(cl);
                            int ix3 = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            double Qmin = cl.Qmin;
                            double contractDem = inputData.DemList[ix3].contractDem;
                            if (wsl.WEEKS <= inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_start")).Value || wsl.WEEKS > inputData.INPUTPARAMETERSList.Find(xyz => xyz.Parameter.Equals("harvest_end")).Value)  //////
                            {
                                contractDem = 0;
                            }
                            inputData.FacList.ForEach(fl =>
                            {
                                int ixF = inputData.FacList.IndexOf(fl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.QualList.ForEach(ql =>
                                    {
                                        int ixQ = inputData.QualList.IndexOf(ql);
                                        if (ql.QUAL <= Qmin)
                                        {
                                            int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                            expr1.AddTerm(1, SC[ix]);
                                        }
                                    });
                                });
                            });
                            inputData.WareList.ForEach(wl =>
                            {
                                int ixW = inputData.WareList.IndexOf(wl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    inputData.WeekHList.ForEach(whl =>
                                    {
                                        int ixWH = inputData.WeekHList.IndexOf(whl);
                                        inputData.QualList.ForEach(ql =>
                                        {
                                            int ixQ = inputData.QualList.IndexOf(ql);
                                            if (wsl.WEEKS >= whl.WEEKH && (whl.WEEKH >= wsl.WEEKS - SL) && ql.QUAL <= Qmin)
                                            {
                                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                                w1l.WEEKH.Equals(whl.WEEKH) &&
                                                w1l.WEEKS.Equals(wsl.WEEKS));
                                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                                expr1.AddTerm(1, SW[ix]);
                                            }
                                        });
                                    });
                                });
                            });
                            inputData.DistList.ForEach(dl =>
                            {
                                int ixD = inputData.DistList.IndexOf(dl);
                                inputData.TransList.ForEach(tl =>
                                {
                                    int ixT = inputData.TransList.IndexOf(tl);
                                    string r = inputData.TransList.FirstOrDefault().TRANS;
                                    inputData.WeekHList.ForEach(whl =>
                                    {
                                        int ixWH = inputData.WeekHList.IndexOf(whl);
                                        inputData.QualList.ForEach(ql =>
                                        {
                                            int ixQ = inputData.QualList.IndexOf(ql);
                                            if (wsl.WEEKS >= whl.WEEKH &&
                                            (whl.WEEKH >= wsl.WEEKS - SL) && ql.QUAL <= Qmin)
                                            {
                                                WEEK1Model week1 = inputData.Week1List.Find(w1l =>
                                                w1l.WEEKH.Equals(whl.WEEKH) &&
                                                w1l.WEEKS.Equals(wsl.WEEKS));
                                                int ixW1 = inputData.Week1List.IndexOf(week1);
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                                double opend = subModelParameters.dcOpenList[ixD].open;
                                                expr1.AddTerm(opend, SD[ix]);
                                            }
                                        });
                                    });
                                });
                            });

                            int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                            expr1.AddTerm(1, Z[ix]);

                            if (!useContract) { contractDem = 0; }
                            expr1.AddConstant(-contractDem / pl.Weight);
                            gModel.AddConstr(expr1, GRB.EQUAL, EXCESS[ix], "Excess_Demand[" + wsl.WEEKS + "," + pl.PROD + "," + cl.CUST + "]");
                            expr1.Clear();
                            counter++;
                        });
                    });
                });
                endAt = counter;
                Constraint EXCESS_DEMAND = new Constraint()
                {
                    name = "EXCESS_DEMAND",
                    startAt = startAt,
                    endAt = endAt,
                    length = endAt - startAt
                };
                constraints.Add(EXCESS_DEMAND);
                utime = DateTime.Now;
                startT = DateTime.Now;
                ct++;


                // Store the model as .lp file
                //gModel.Write(outputFolder + "SubProblem//Models//Subproblem_LP_" + subModelParameters.nCut + "_" + scen + ".lp");

                try
                {
                    startT = DateTime.Now;
                    Console.Write("Solving:");
                    gModel.Parameters.InfUnbdInfo = 1; // set to one to retain informatio related to infeasibility, 0 otherwise 
                    gModel.Optimize();
                    utime = DateTime.Now;
                    Console.Write("\t\tOK.\t" + Math.Round((utime - startT).TotalSeconds, 2) + "\n");

                    bool hasError = false;
                    if (gModel.Status == 2 || gModel.Status == 3 || gModel.Status == 4) // Solved, infeasible/unbounded
                    {
                        //PACK VAR
                        List<GeneralOutputs> packOutputs = new List<GeneralOutputs>();
                        inputData.WeekHList.ForEach(whl =>
                        {
                            int ixWH = inputData.WeekHList.IndexOf(whl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    inputData.FacList.ForEach(fl =>
                                    {
                                        int ixF = inputData.FacList.IndexOf(fl);
                                        int ix = ixVar.getIx4(ixWH, ixP, ixQ, ixF, nWeekH, nProd, nQual, nFac);
                                        GeneralOutputs po = new GeneralOutputs()
                                        {
                                            WEEKH = whl.WEEKH,
                                            PROD = pl.PROD,
                                            QUAL = ql.QUAL,
                                            FAC = fl.FAC,
                                            Value = PACK[ix].X
                                        };
                                        packOutputs.Add(po);
                                    });
                                });
                            });
                        });

                        //SP VAR
                        List<GeneralOutputs> spOutputs = new List<GeneralOutputs>();
                        inputData.WeekPList.ForEach(wpl =>
                        {
                            int ixWP = inputData.WeekPList.IndexOf(wpl);
                            inputData.WeekHList.ForEach(whl =>
                            {
                                int ixWH = inputData.WeekHList.IndexOf(whl);
                                inputData.CropList.ForEach(cl =>
                                {
                                    int ixC = inputData.CropList.IndexOf(cl);
                                    inputData.LocList.ForEach(ll =>
                                    {
                                        int ixL = inputData.LocList.IndexOf(ll);
                                        inputData.FacList.ForEach(fl =>
                                        {
                                            int ixF = inputData.FacList.IndexOf(fl);
                                            int ix = ixVar.getIx5(ixWP, ixWH, ixC, ixL, ixF, nWeekP, nWeekH, nCrop, nLoc, nFac);
                                            GeneralOutputs spo = new GeneralOutputs()
                                            {
                                                WEEKP = wpl.WEEKP,
                                                WEEKH = whl.WEEKH,
                                                CROP = cl.CROP,
                                                LOC = ll.LOC,
                                                FAC = fl.FAC,
                                                Value = SP[ix].X
                                            };
                                            spOutputs.Add(spo);
                                        });
                                    });
                                });
                            });
                        });

                        //Sel VAR
                        List<GeneralOutputs> selOutputs = new List<GeneralOutputs>();
                        for (int p = 0; p < nWeekP; p++)
                        {
                            for (int h = 0; h < nWeekH; h++)
                            {
                                for (int j = 0; j < nCrop; j++)
                                {
                                    for (int f = 0; f < nFac; f++)
                                    {
                                        int ix = ixVar.getIx4(p, h, j, f, nWeekP, nWeekH, nCrop, nFac);
                                        GeneralOutputs so = new GeneralOutputs()
                                        {
                                            WEEKP = inputData.WeekPList[p].WEEKP,
                                            WEEKH = inputData.WeekHList[h].WEEKH,
                                            CROP = inputData.CropList[j].CROP,
                                            FAC = inputData.FacList[f].FAC,
                                            Value = Sel[ix].X
                                        };
                                        selOutputs.Add(so);
                                    }
                                }
                            }
                        }

                        //SC VAR
                        List<GeneralOutputs> scOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                double TraF = pl.TraF;
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
                                                int ix = ixVar.getIx6(ixWS, ixP, ixQ, ixF, ixC, ixT, nWeekS, nProd, nQual, nFac, nCust, nTrans);
                                                GeneralOutputs sco = new GeneralOutputs()
                                                {
                                                    WEEKS = wsl.WEEKS,
                                                    PROD = pl.PROD,
                                                    QUAL = ql.QUAL,
                                                    FAC = fl.FAC,
                                                    CUST = cl.CUST,
                                                    TRANS = tl.TRANS,
                                                    Value = SC[ix].X
                                                };
                                                scOutputs.Add(sco);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        //SD VAR
                        List<GeneralOutputs> sdOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
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
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixD, ixC, ixT, nWeek1, nProd, nQual, nDC, nCust, nTrans);
                                                GeneralOutputs sdo = new GeneralOutputs()
                                                {
                                                    WEEKH = w1l.WEEKH,
                                                    WEEKS = w1l.WEEKS,
                                                    PROD = pl.PROD,
                                                    QUAL = ql.QUAL,
                                                    DC = dl.DIST,
                                                    CUST = cl.CUST,
                                                    TRANS = tl.TRANS,
                                                    Value = SD[ix].X
                                                };
                                                sdOutputs.Add(sdo);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        //SW VAR
                        List<GeneralOutputs> swOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
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
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixC, ixT, nWeek1, nProd, nQual, nWare, nCust, nTrans);
                                                GeneralOutputs swo = new GeneralOutputs()
                                                {
                                                    WEEKH = w1l.WEEKH,
                                                    WEEKS = w1l.WEEKS,
                                                    PROD = pl.PROD,
                                                    QUAL = ql.QUAL,
                                                    WARE = wl.WARE,
                                                    CUST = cl.CUST,
                                                    TRANS = tl.TRANS,
                                                    Value = SW[ix].X
                                                };
                                                swOutputs.Add(swo);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        //SPD VAR
                        List<GeneralOutputs> spdOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    inputData.FacList.ForEach(fl =>
                                    {
                                        int ixF = inputData.FacList.IndexOf(fl);
                                        inputData.DistList.ForEach(dl =>
                                        {
                                            int ixD = inputData.DistList.IndexOf(dl);
                                            inputData.TransList.ForEach(tl =>
                                            {
                                                int ixT = inputData.TransList.IndexOf(tl);
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixD, ixT, nWeek1, nProd, nQual, nFac, nDC, nTrans);
                                                GeneralOutputs spdo = new GeneralOutputs()
                                                {
                                                    WEEKH = w1l.WEEKH,
                                                    WEEKS = w1l.WEEKS,
                                                    PROD = pl.PROD,
                                                    QUAL = ql.QUAL,
                                                    FAC = fl.FAC,
                                                    DC = dl.DIST,
                                                    TRANS = tl.TRANS,
                                                    Value = SPD[ix].X
                                                };
                                                spdOutputs.Add(spdo);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        //SPW VAR
                        List<GeneralOutputs> spwOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
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
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixF, ixW, ixT, nWeek1, nProd, nQual, nFac, nWare, nTrans);
                                                GeneralOutputs spwo = new GeneralOutputs()
                                                {
                                                    WEEKH = w1l.WEEKH,
                                                    WEEKS = w1l.WEEKS,
                                                    PROD = pl.PROD,
                                                    QUAL = ql.QUAL,
                                                    FAC = fl.FAC,
                                                    WARE = wl.WARE,
                                                    TRANS = tl.TRANS,
                                                    Value = SPW[ix].X
                                                };
                                                spwOutputs.Add(spwo);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        //SWD VAR
                        List<GeneralOutputs> swdOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixW = inputData.WareList.IndexOf(wl);
                                        inputData.DistList.ForEach(dl =>
                                        {
                                            int ixD = inputData.DistList.IndexOf(dl);
                                            inputData.TransList.ForEach(tl =>
                                            {
                                                int ixT = inputData.TransList.IndexOf(tl);
                                                int ix = ixVar.getIx6(ixW1, ixP, ixQ, ixW, ixD, ixT, nWeek1, nProd, nQual, nWare, nDC, nTrans);
                                                GeneralOutputs swdo = new GeneralOutputs()
                                                {
                                                    WEEKH = w1l.WEEKH,
                                                    WEEKS = w1l.WEEKS,
                                                    PROD = pl.PROD,
                                                    QUAL = ql.QUAL,
                                                    DC = dl.DIST,
                                                    WARE = wl.WARE,
                                                    TRANS = tl.TRANS,
                                                    Value = SWD[ix].X
                                                };
                                                swdOutputs.Add(swdo);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        //INVW VAR
                        List<GeneralOutputs> invwOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            double ws = w1l.WEEKS;
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixW = inputData.WareList.IndexOf(wl);
                                        int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixW, nWeek1, nProd, nQual, nWare);
                                        GeneralOutputs invwo = new GeneralOutputs()
                                        {
                                            WEEKH = w1l.WEEKH,
                                            WEEKS = w1l.WEEKS,
                                            PROD = pl.PROD,
                                            QUAL = ql.QUAL,
                                            WARE = wl.WARE,
                                            Value = Invw[ix].X
                                        };
                                        invwOutputs.Add(invwo);
                                    });
                                });
                            });
                        });

                        //INVD VAR
                        List<GeneralOutputs> invdOutputs = new List<GeneralOutputs>();
                        inputData.Week1List.ForEach(w1l =>
                        {
                            int ixW1 = inputData.Week1List.IndexOf(w1l);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.QualList.ForEach(ql =>
                                {
                                    int ixQ = inputData.QualList.IndexOf(ql);
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        int ix = ixVar.getIx4(ixW1, ixP, ixQ, ixD, nWeek1, nProd, nQual, nDC);
                                        GeneralOutputs invdo = new GeneralOutputs()
                                        {
                                            WEEKH = w1l.WEEKH,
                                            WEEKS = w1l.WEEKS,
                                            PROD = pl.PROD,
                                            QUAL = ql.QUAL,
                                            DC = dl.DIST,
                                            Value = Invd[ix].X
                                        };
                                        invdOutputs.Add(invdo);
                                    });
                                });
                            });
                        });

                        //TC VAR
                        List<GeneralOutputs> tcOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx5(ixWS, ixP, ixF, ixC, ixT, nWeekS, nProd, nFac, nCust, nTrans);
                                            GeneralOutputs tco = new GeneralOutputs()
                                            {
                                                WEEKS = wsl.WEEKS,
                                                PROD = pl.PROD,
                                                FAC = fl.FAC,
                                                CUST = cl.CUST,
                                                TRANS = tl.TRANS,
                                                Value = TC[ix].X
                                            };
                                            tcOutputs.Add(tco);
                                        });
                                    });
                                });
                            });
                        });

                        //TD VAR
                        List<GeneralOutputs> tdOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.DistList.ForEach(dl =>
                                {
                                    int ixD = inputData.DistList.IndexOf(dl);
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx5(ixWS, ixP, ixD, ixC, ixT, nWeekS, nProd, nDC, nCust, nTrans);
                                            GeneralOutputs tdo = new GeneralOutputs()
                                            {
                                                WEEKS = wsl.WEEKS,
                                                PROD = pl.PROD,
                                                DC = dl.DIST,
                                                CUST = cl.CUST,
                                                TRANS = tl.TRANS,
                                                Value = TD[ix].X
                                            };
                                            tdOutputs.Add(tdo);
                                        });
                                    });
                                });
                            });
                        });

                        //TW VAR
                        List<GeneralOutputs> twOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.CustList.ForEach(cl =>
                                    {
                                        int ixC = inputData.CustList.IndexOf(cl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx5(ixWS, ixP, ixW, ixC, ixT, nWeekS, nProd, nWare, nCust, nTrans);
                                            GeneralOutputs two = new GeneralOutputs()
                                            {
                                                WEEKS = wsl.WEEKS,
                                                PROD = pl.PROD,
                                                WARE = wl.WARE,
                                                CUST = cl.CUST,
                                                TRANS = tl.TRANS,
                                                Value = TW[ix].X
                                            };
                                            twOutputs.Add(two);
                                        });
                                    });
                                });
                            });
                        });

                        //TPD VAR
                        List<GeneralOutputs> tpdOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx5(ixWS, ixP, ixF, ixD, ixT, nWeekS, nProd, nFac, nDC, nTrans);
                                            GeneralOutputs tpdo = new GeneralOutputs()
                                            {
                                                WEEKS = wsl.WEEKS,
                                                PROD = pl.PROD,
                                                FAC = fl.FAC,
                                                DC = dl.DIST,
                                                TRANS = tl.TRANS,
                                                Value = TPD[ix].X
                                            };
                                            tpdOutputs.Add(tpdo);
                                        });
                                    });
                                });
                            });
                        });

                        //TPW VAR
                        List<GeneralOutputs> tpwOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.FacList.ForEach(fl =>
                                {
                                    int ixF = inputData.FacList.IndexOf(fl);
                                    inputData.WareList.ForEach(wl =>
                                    {
                                        int ixW = inputData.WareList.IndexOf(wl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx5(ixWS, ixP, ixF, ixW, ixT, nWeekS, nProd, nFac, nWare, nTrans);
                                            GeneralOutputs tpwo = new GeneralOutputs()
                                            {
                                                WEEKS = wsl.WEEKS,
                                                PROD = pl.PROD,
                                                FAC = fl.FAC,
                                                WARE = wl.WARE,
                                                TRANS = tl.TRANS,
                                                Value = TPW[ix].X
                                            };
                                            tpwOutputs.Add(tpwo);
                                        });
                                    });
                                });
                            });
                        });

                        //scenario harvest

                        List<GeneralOutputs> harsOutputs = new List<GeneralOutputs>();
                        inputData.WeekPList.ForEach(pl =>
                        {
                            int ixP = inputData.WeekPList.IndexOf(pl);
                            inputData.WeekHList.ForEach(hl =>
                            {
                                int ixH = inputData.WeekHList.IndexOf(hl);
                                inputData.CropList.ForEach(cl =>
                                {
                                    int ixC = inputData.CropList.IndexOf(cl);
                                    inputData.LocList.ForEach(ll =>
                                    {
                                        int ixL = inputData.LocList.IndexOf(ll);
                                        int ix = ixVar.getIx4(ixP, ixH, ixC, ixL, nWeekP, nWeekH, nCrop, nLoc);
                                        GeneralOutputs harvest_s = new GeneralOutputs()
                                        {
                                            WEEKP = pl.WEEKP,
                                            WEEKH = hl.WEEKH,
                                            CROP = cl.CROP,
                                            LOC = ll.LOC,
                                            SCEN = scen,
                                            Value = subModelParameters.HarvestList[ix]
                                        };
                                        harsOutputs.Add(harvest_s);
                                    });
                                });

                            });
                        });

                        //TWD VAR
                        List<GeneralOutputs> twdOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.WareList.ForEach(wl =>
                                {
                                    int ixW = inputData.WareList.IndexOf(wl);
                                    inputData.DistList.ForEach(dl =>
                                    {
                                        int ixD = inputData.DistList.IndexOf(dl);
                                        inputData.TransList.ForEach(tl =>
                                        {
                                            int ixT = inputData.TransList.IndexOf(tl);
                                            int ix = ixVar.getIx5(ixWS, ixP, ixW, ixD, ixT, nWeekS, nProd, nWare, nDC, nTrans);
                                            GeneralOutputs twdo = new GeneralOutputs()
                                            {
                                                WEEKS = wsl.WEEKS,
                                                PROD = pl.PROD,
                                                DC = dl.DIST,
                                                WARE = wl.WARE,
                                                TRANS = tl.TRANS,
                                                SCEN = scen,
                                                Value = TWD[ix].X
                                            };
                                            twdOutputs.Add(twdo);
                                        });
                                    });
                                });
                            });
                        });

                        //K VAR
                        List<GeneralOutputs> kOutputs = new List<GeneralOutputs>();
                        inputData.WeekHList.ForEach(whl =>
                        {
                            int ixWH = inputData.WeekHList.IndexOf(whl);
                            inputData.CropList.ForEach(cl =>
                            {
                                int ixC = inputData.CropList.IndexOf(cl);
                                int ix = ixVar.getIx2(ixWH, ixC, nWeekH, nCrop);
                                GeneralOutputs ko = new GeneralOutputs()
                                {
                                    WEEKH = whl.WEEKH,
                                    CROP = cl.CROP,
                                    Value = K[ix].X
                                };
                                kOutputs.Add(ko);
                            });
                        });

                        //Z VAR
                        List<GeneralOutputs> zOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.CustList.ForEach(cl =>
                                {
                                    int ixC = inputData.CustList.IndexOf(cl);
                                    int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                    GeneralOutputs zo = new GeneralOutputs()
                                    {
                                        WEEKS = wsl.WEEKS,
                                        PROD = pl.PROD,
                                        CUST = cl.CUST,
                                        Value = Z[ix].X
                                    };
                                    zOutputs.Add(zo);
                                });
                            });
                        });
                        //Excess VAR
                        List<GeneralOutputs> ExcessOutputs = new List<GeneralOutputs>();
                        inputData.WeekSList.ForEach(wsl =>
                        {
                            int ixWS = inputData.WeekSList.IndexOf(wsl);
                            inputData.ProdList.ForEach(pl =>
                            {
                                int ixP = inputData.ProdList.IndexOf(pl);
                                inputData.CustList.ForEach(cl =>
                                {
                                    int ixC = inputData.CustList.IndexOf(cl);
                                    int ix = ixVar.getIx3(ixWS, ixP, ixC, nWeekS, nProd, nCust);
                                    GeneralOutputs zo = new GeneralOutputs()
                                    {
                                        WEEKS = wsl.WEEKS,
                                        PROD = pl.PROD,
                                        CUST = cl.CUST,
                                        Value = EXCESS[ix].X
                                    };
                                    ExcessOutputs.Add(zo);
                                });
                            });
                        });

                        submodelOutputs = new SubModelOutputs()
                        {
                            Pack = packOutputs,
                            SP = spOutputs,
                            SEL = selOutputs,
                            SC = scOutputs,
                            SD = sdOutputs,
                            SW = swOutputs,
                            SPD = spdOutputs,
                            SPW = spwOutputs,
                            SWD = swdOutputs,
                            Invw = invwOutputs,
                            Invd = invdOutputs,
                            TC = tcOutputs,
                            TD = tdOutputs,
                            TW = twOutputs,
                            TPD = tpdOutputs,
                            TPW = tpwOutputs,
                            TWD = twdOutputs,
                            HARVEST = harsOutputs,
                            K = kOutputs,
                            Z = zOutputs,
                            EXCESS = ExcessOutputs,
                            Constraints = constraints,
                            Sub_rev = gModel.Status == 2 ? gModel.ObjVal : 0,
                            hasError = hasError,
                            status = gModel.Status
                        };
                    }
                    else
                    {
                        hasError = true;
                        submodelOutputs = new SubModelOutputs()
                        {
                            hasError = hasError,
                            status = gModel.Status
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR SOLVING THE SUB MODEL");
                    Console.WriteLine("Error code: " + ex.InnerException + ". " + ex.Message);
                    submodelOutputs = new SubModelOutputs()
                    {
                        hasError = true,
                        status = gModel.Status
                    };
                }
            }
            catch (GRBException ex)
            {
                Console.WriteLine("Error code: " + ex.ErrorCode + ". " + ex.Message);
                submodelOutputs = new SubModelOutputs()
                {
                    hasError = true,
                    status = gModel.Status
                };
            }
            return submodelOutputs;
        }

    }
}
