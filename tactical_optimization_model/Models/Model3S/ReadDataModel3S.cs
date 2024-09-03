using ModelStochastic6.Models;
using ModelStochastic6.Models.Inputs;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using WebApiDemoNetCore.Models.Model3S;

namespace ModelStochastic6
{
    class ReadDataModel3S
    {
        private static MySqlConnection connection;
        private static MySqlCommand cmd = null;
        private static DataTable dt;
        private static MySqlDataAdapter sda;

        public InputData readData(string inputFolder, Model3SRequest model3SRequest)
        {
            string sheetName = "Sheet1";
            string strFile = "";
            DataTable dtXLS = new DataTable();
            DataTable dtDB = new DataTable();
            string dataTableToJsonString = "";

            //CROPS & LOCATION SELECTED
            List<string> cropsList = model3SRequest.cropsSelected.OrderBy(cs => cs.ccrop).Select(cs => cs.ccrop).Distinct().ToList();
            List<string> locationsList = model3SRequest.locationsSelected.OrderBy(ls => ls.abbr).Select(ls => ls.abbr.Replace(" ", "_")).Distinct().ToList();

            //READING input_parameters.xlsx
            strFile = "Extra_Parameters.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<INPUTModel> INPUTPARAMTERSList = new List<INPUTModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            INPUTPARAMTERSList = JsonConvert.DeserializeObject<List<INPUTModel>>(dataTableToJsonString);

            //READING Plant_0.xlsx
            strFile = "Plant_0.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<Plant0Model> Plant0List = new List<Plant0Model>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            Plant0List = JsonConvert.DeserializeObject<List<Plant0Model>>(dataTableToJsonString);
            Plant0List = (from pl in Plant0List
                          where locationsList.Contains(pl.LOC)
                           && cropsList.Contains(pl.CROP)
                          select pl).ToList();

            //READING CUST.xlsx
            strFile = "CUST.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CUSTModel> CustList = new List<CUSTModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CustList = JsonConvert.DeserializeObject<List<CUSTModel>>(dataTableToJsonString);

            //READING CROP.xlsx
            strFile = "CROP.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CROPModel> CropList = new List<CROPModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CropList = JsonConvert.DeserializeObject<List<CROPModel>>(dataTableToJsonString);
            List<CROPModel> cropListSelectedByUser = new List<CROPModel>();
            model3SRequest.cropsSelected.ForEach(cs =>  // Here is adding from the user Selection to this new object
            {
                cropListSelectedByUser.Add(new CROPModel()
                {
                    CROP = cs.ccrop,
                    Dharv = cs.dharv,
                    Psalv = cs.pslav,
                });
            });
            CropList = cropListSelectedByUser;

            // CROPBUDGET (SQL)
            List<CROPBUDGETModel> cropBudgetListSelectedByUser = new List<CROPBUDGETModel>();
            dtDB = queryCropBudgetDB(cropsList, locationsList);
            dataTableToJsonString = DataTableToJsonString(dtDB);
            cropBudgetListSelectedByUser = JsonConvert.DeserializeObject<List<CROPBUDGETModel>>(dataTableToJsonString);

            //READING PROD.xlsx
            strFile = "PROD.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<PRODModel> ProdList = new List<PRODModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            ProdList = JsonConvert.DeserializeObject<List<PRODModel>>(dataTableToJsonString);
            List<string> prodsList = (from pl in ProdList
                                      where cropsList.Contains(pl.Pcrop)
                                      select pl.PROD).ToList();
            ProdList = (from pl in ProdList
                        where cropsList.Contains(pl.Pcrop)
                        select pl).ToList();

            //READING WEEKP.xlsx
            strFile = "WEEKP.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<WEEKPModel> WeekPList = new List<WEEKPModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            WeekPList = JsonConvert.DeserializeObject<List<WEEKPModel>>(dataTableToJsonString);

            //READING WEEKH.xlsx
            strFile = "WEEKH.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<WEEKHModel> WeekHList = new List<WEEKHModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            WeekHList = JsonConvert.DeserializeObject<List<WEEKHModel>>(dataTableToJsonString);

            //READING WEEKS.xlsx
            strFile = "WEEKS.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<WEEKSModel> WeekSList = new List<WEEKSModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            WeekSList = JsonConvert.DeserializeObject<List<WEEKSModel>>(dataTableToJsonString);

            //READING WEEK.xlsx
            strFile = "WEEK.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<WEEKModel> WeekList = new List<WEEKModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            WeekList = JsonConvert.DeserializeObject<List<WEEKModel>>(dataTableToJsonString);

            //READING WEEK1.xlsx
            strFile = "WEEK1.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<WEEK1Model> Week1List = new List<WEEK1Model>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            Week1List = JsonConvert.DeserializeObject<List<WEEK1Model>>(dataTableToJsonString);

            //READING LOC.xlsx
            strFile = "LOC.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<LOCModel> LocList = new List<LOCModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            LocList = JsonConvert.DeserializeObject<List<LOCModel>>(dataTableToJsonString);
            List<LOCModel> locListSelectedByUser = new List<LOCModel>();
            model3SRequest.locationsSelected.ForEach(ls =>
            {
                locListSelectedByUser.Add(new LOCModel()
                {
                    LOC = ls.abbr,
                    LA = ls.la
                });
            });
            LocList = locListSelectedByUser;

            //READING FAC.xlsx
            strFile = "FAC.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<FACModel> FacList = new List<FACModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            FacList = JsonConvert.DeserializeObject<List<FACModel>>(dataTableToJsonString);

            //READING WARE.xlsx
            strFile = "WARE.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<WAREModel> WareList = new List<WAREModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            WareList = JsonConvert.DeserializeObject<List<WAREModel>>(dataTableToJsonString);

            //READING DIST.xlsx
            strFile = "DIST.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<DISTModel> DistList = new List<DISTModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            DistList = JsonConvert.DeserializeObject<List<DISTModel>>(dataTableToJsonString);

            //READING TRANS.xlsx
            strFile = "TRANS.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<TRANSModel> TransList = new List<TRANSModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            TransList = JsonConvert.DeserializeObject<List<TRANSModel>>(dataTableToJsonString);

            //READING M.xlsx
            strFile = "M.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<MModel> MList = new List<MModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            MList = JsonConvert.DeserializeObject<List<MModel>>(dataTableToJsonString);

            // Yields ALL  (SQL)
            List<YIELDModel> YieldList = new List<YIELDModel>();
            dtDB = queryYieldDB(cropsList, locationsList);
            YieldList = DataTableToYieldModelList(dtDB, YieldList);

            // Expected Yield  (SQL)
            List<YIELDExpModel> YieldList_exp = new List<YIELDExpModel>();
            dtDB = queryExpYieldDB(cropsList, locationsList);
            YieldList_exp = DataTableToExpYieldModelList(dtDB, YieldList_exp);

            //READING POD.xlsx
            strFile = "POD.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<PODModel> PodList = new List<PODModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            PodList = JsonConvert.DeserializeObject<List<PODModel>>(dataTableToJsonString);
            PodList = (from pl in PodList
                       where cropsList.Contains(pl.CROP)
                       && prodsList.Contains(pl.PROD)
                       select pl).ToList();

            //READING DEM.xlsx
            strFile = "DEM.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<DEMModel> DemList = new List<DEMModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            DemList = JsonConvert.DeserializeObject<List<DEMModel>>(dataTableToJsonString);
            DemList = (from dl in DemList
                       where prodsList.Contains(dl.PROD)
                       select dl).ToList();



            // PRICE ALL (SQL)
            /*
            List<PRICEModel> PriceList = new List<PRICEModel>();
            dtDB = queryPriceDB(cropsList, new List<string>());
            dataTableToJsonString = DataTableToJsonString(dtDB);
            PriceList = JsonConvert.DeserializeObject<List<PRICEModel>>(dataTableToJsonString);


            // Expected Price (SQL)
            List<PRICEExpModel> PriceList_exp = new List<PRICEExpModel>();
            dtDB = queryExpPriceDB(cropsList, new List<string>());
            dataTableToJsonString = DataTableToJsonString(dtDB);
            PriceList_exp = JsonConvert.DeserializeObject<List<PRICEExpModel>>(dataTableToJsonString);
            dtDB.Dispose();
            */
            //READING PRICE.xlsx
            strFile = "PRICE.xlsx";
            //Console.WriteLine("READING DATA: " + strFile);
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<PRICEModel> PriceList = new List<PRICEModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            //Console.WriteLine(JsonConvert.SerializeObject(dataTableToJsonString));
            PriceList = JsonConvert.DeserializeObject<List<PRICEModel>>(dataTableToJsonString);

            //PPDATA
            PriceList = (from pl in PriceList
                         where prodsList.Contains(pl.PROD)
                         select pl).ToList();

            //READING PRICE.xlsx
            strFile = "PRICE_exp.xlsx";
            //Console.WriteLine("READING DATA: " + strFile);
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<PRICEExpModel> PriceList_exp = new List<PRICEExpModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            //Console.WriteLine(JsonConvert.SerializeObject(dataTableToJsonString));
            PriceList_exp = JsonConvert.DeserializeObject<List<PRICEExpModel>>(dataTableToJsonString);

            //PPDATA
            PriceList_exp = (from pl in PriceList_exp
                         where prodsList.Contains(pl.PROD)
                         select pl).ToList();





            /*// Expected Price (SQL)
            List<PRICEExpModel> PriceList_exp = new List<PRICEExpModel>();
            dtDB = queryExpPriceDB(cropsList, new List<string>());
            dataTableToJsonString = DataTableToJsonString(dtDB);
            PriceList_exp = JsonConvert.DeserializeObject<List<PRICEExpModel>>(dataTableToJsonString);
            dtDB.Dispose();
            */


            //READING SCEN.xlsx
            strFile = "SCEN.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<SCENModel> ScenList = new List<SCENModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            ScenList = JsonConvert.DeserializeObject<List<SCENModel>>(dataTableToJsonString);

            //READING PAVG.xlsx
            strFile = "PAVG.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<PAVGModel> PavgList = new List<PAVGModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            PavgList = JsonConvert.DeserializeObject<List<PAVGModel>>(dataTableToJsonString);

            PavgList = (from pl in PavgList
                        where prodsList.Contains(pl.PROD)
                        select pl).ToList();

            //READING CHD.xlsx
            strFile = "CHD.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CHDModel> ChdList = new List<CHDModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            ChdList = JsonConvert.DeserializeObject<List<CHDModel>>(dataTableToJsonString);
            ChdList = (from cl in ChdList
                       where prodsList.Contains(cl.PROD)
                       select cl).ToList();

            //READING CHW.xlsx
            strFile = "CHW.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CHWModel> ChwList = new List<CHWModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            ChwList = JsonConvert.DeserializeObject<List<CHWModel>>(dataTableToJsonString);
            ChwList = (from cl in ChwList
                       where prodsList.Contains(cl.PROD)
                       select cl).ToList();

            //READING CT.xlsx
            strFile = "CT.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTModel> CtList = new List<CTModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtList = JsonConvert.DeserializeObject<List<CTModel>>(dataTableToJsonString);

            //READING CTW.xlsx
            strFile = "CTW.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTWModel> CtwList = new List<CTWModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtwList = JsonConvert.DeserializeObject<List<CTWModel>>(dataTableToJsonString);

            //READING CTD.xlsx
            strFile = "CTD.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTDModel> CtdList = new List<CTDModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtdList = JsonConvert.DeserializeObject<List<CTDModel>>(dataTableToJsonString);

            //READING CTPW.xlsx
            strFile = "CTPW.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTPWModel> CtpwList = new List<CTPWModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtpwList = JsonConvert.DeserializeObject<List<CTPWModel>>(dataTableToJsonString);

            //READING CTPD.xlsx
            strFile = "CTPD.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTPDModel> CtpdList = new List<CTPDModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtpdList = JsonConvert.DeserializeObject<List<CTPDModel>>(dataTableToJsonString);

            //READING CTWD.xlsx
            strFile = "CTWD.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTWDModel> CtwdList = new List<CTWDModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtwdList = JsonConvert.DeserializeObject<List<CTWDModel>>(dataTableToJsonString);


            //READING CTLF.xlsx
            strFile = "CTLF.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<CTLFModel> CtlfList = new List<CTLFModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            CtlfList = JsonConvert.DeserializeObject<List<CTLFModel>>(dataTableToJsonString);
            CtlfList = (from cl in CtlfList
                        where
                        locationsList.Contains(cl.LOC)
                        select cl).ToList();

            //READING LABOR.xlsx
            strFile = "LABOR.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<LABORModel> LaborList = new List<LABORModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            LaborList = JsonConvert.DeserializeObject<List<LABORModel>>(dataTableToJsonString);
            LaborList = (from ll in LaborList
                         where cropsList.Contains(ll.CROP)
                         select ll).ToList();

            //READING COL.xlsx
            strFile = "COL.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<COLModel> ColList = new List<COLModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            ColList = JsonConvert.DeserializeObject<List<COLModel>>(dataTableToJsonString);
            ColList = (from cl in ColList
                       where prodsList.Contains(cl.PROD)
                       select cl).ToList();

            //READING QUAL.xlsx
            strFile = "QUAL.xlsx";
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<QUALModel> QualList = new List<QUALModel>();
            dataTableToJsonString = DataTableToJsonString(dtXLS);
            QualList = JsonConvert.DeserializeObject<List<QUALModel>>(dataTableToJsonString);
            dtXLS.Dispose();
            dataTableToJsonString = null;
            dtDB.Dispose();

            return new InputData()
            {
                Plant0List = Plant0List,
                CustList = CustList,
                CropList = CropList,
                CropBudgetList = cropBudgetListSelectedByUser,
                ProdList = ProdList,
                WeekPList = WeekPList,
                WeekHList = WeekHList,
                WeekSList = WeekSList,
                WeekList = WeekList,
                Week1List = Week1List,
                LocList = LocList,
                FacList = FacList,
                WareList = WareList,
                DistList = DistList,
                TransList = TransList,
                MList = MList,
                YieldList = YieldList,
                YieldList_exp = YieldList_exp,
                PodList = PodList,
                DemList = DemList,
                PriceList = PriceList,
                PriceList_exp = PriceList_exp,
                ScenList = ScenList,
                PavgList = PavgList,
                ChdList = ChdList,
                ChwList = ChwList,
                CtList = CtList,
                CtlfList = CtlfList,
                CtwdList = CtwdList,
                CtdList = CtdList,
                CtpdList = CtpdList,
                CtpwList = CtpwList,
                CtwList = CtwList,
                LaborList = LaborList,
                ColList = ColList,
                QualList = QualList,
                INPUTPARAMETERSList = INPUTPARAMTERSList
            };
        }

        static public DataTable ReadExcelFileToDataTable(string inputFolder, string strFile, string sheetName)
        {
            DataTable dtXLS = new DataTable(sheetName);
            try
            {
                string strConnectionString = "";
                if (strFile.Trim().EndsWith(".xlsx"))
                {
                    strConnectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1\";", inputFolder + strFile);
                }
                else if (strFile.Trim().EndsWith(".xls"))
                {
                    strConnectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=1\";", inputFolder + strFile);
                }

                OleDbConnection SQLConn = new OleDbConnection(strConnectionString);
                SQLConn.Open();
                OleDbDataAdapter SQLAdapter = new OleDbDataAdapter();
                string sql = "SELECT * FROM [" + sheetName + "$]";
                OleDbCommand selectCMD = new OleDbCommand(sql, SQLConn);
                SQLAdapter.SelectCommand = selectCMD;
                SQLAdapter.Fill(dtXLS);

                SQLConn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return dtXLS;
        }

        static public string DataTableToJsonString(DataTable dataTable)
        {
            string dataTableToJsonString = "";
            try
            {
                List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                Dictionary<string, object> row;
                foreach (DataRow dr in dataTable.Rows)
                {
                    row = new Dictionary<string, object>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        row.Add(col.ColumnName, dr[col]);
                    }
                    rows.Add(row);
                }
                dataTableToJsonString = JsonConvert.SerializeObject(rows);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return dataTableToJsonString;
        }

        static public DataTable queryPriceDB(List<string> cropList, List<string> locationList)
        {
            DataTable dt = new DataTable();
            try
            {
                string myConnectionString = "server=db-asu-datapipeline.cbhji75irubx.us-east-2.rds.amazonaws.com;uid=OptimizationUser;pwd=ProjectFFAR2022;database=USDA";
                string sql = "Select * from USDA.pricesForOptimization ";
                string whereclause = " ";
                string croplist = " ";
                string locationlist = " ";
                if (cropList.Count > 0)
                {
                    croplist += " (";
                    for (int i = 0; i < cropList.Count; i++)
                    {
                        croplist += " prod = '" + cropList[i] + "_P" + "' or ";
                    }
                    croplist = croplist.Substring(0, croplist.Length - 3);
                    croplist += " ) ";
                }
                if (locationList.Count > 0)
                {
                    locationlist += " (";
                    for (int i = 0; i < locationList.Count; i++)
                    {
                        locationlist += "cust = '" + locationList[i] + "' or ";
                    }
                    locationlist = locationlist.Substring(0, locationlist.Length - 3);
                    locationlist += " ) ";
                }
                if (cropList.Count != 0 && locationList.Count != 0)
                {
                    whereclause += " where " + croplist + " and " + locationlist;
                }
                else
                {
                    if (cropList.Count != 0)
                    {
                        whereclause += " where " + croplist;
                    }
                    else if (locationList.Count != 0)
                    {
                        whereclause += " where " + locationlist;
                    }
                }
                sql += whereclause;
                connection = new MySqlConnection(myConnectionString);
                connection.Open();
                sda = new MySqlDataAdapter();
                cmd = new MySqlCommand(sql, connection);

                sda.SelectCommand = cmd;
                sda.Fill(dt);
                connection.Close();
                sda.Dispose();
                dt.Columns.Remove("id");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dt;
        }

        static public DataTable queryCropBudgetDB(List<string> cropList, List<string> locationList)
        {
            DataTable dt = new DataTable();
            try
            {
                string myConnectionString = "server=db-asu-datapipeline.cbhji75irubx.us-east-2.rds.amazonaws.com;uid=OptimizationUser;pwd=ProjectFFAR2022;database=USDA"; ;
                string sql = "Select * from USDA.cropBudgetForOptimization ";
                string whereclause = " ";
                string croplist = " ";
                string locationlist = " ";
                if (cropList.Count > 0)
                {
                    croplist += " (";
                    for (int i = 0; i < cropList.Count; i++)
                    {
                        croplist += " crop = '" + cropList[i] + "' or ";
                    }
                    croplist = croplist.Substring(0, croplist.Length - 3);
                    croplist += " ) ";
                }
                if (locationList.Count > 0)
                {
                    locationlist += " (";
                    for (int i = 0; i < locationList.Count; i++)
                    {
                        locationlist += "loc = '" + locationList[i] + "' or ";
                    }
                    locationlist = locationlist.Substring(0, locationlist.Length - 3);
                    locationlist += " ) ";
                }
                if (cropList.Count != 0 && locationList.Count != 0)
                {
                    whereclause += " where " + croplist + " and " + locationlist;
                }
                else
                {
                    if (cropList.Count != 0)
                    {
                        whereclause += " where " + croplist;
                    }
                    else if (locationList.Count != 0)
                    {
                        whereclause += " where " + locationlist;
                    }
                }
                sql += whereclause;
                connection = new MySqlConnection(myConnectionString);
                connection.Open();
                sda = new MySqlDataAdapter();
                cmd = new MySqlCommand(sql, connection);
                cmd.CommandTimeout = 1000;
                sda.SelectCommand = cmd;
                sda.Fill(dt);
                connection.Close();
                sda.Dispose();
                dt.Columns.Remove("id");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dt;
        }

        static public DataTable queryExpPriceDB(List<string> cropList, List<string> locationList)
        {
            DataTable dt = new DataTable();
            try
            {
                string myConnectionString = "server=db-asu-datapipeline.cbhji75irubx.us-east-2.rds.amazonaws.com;uid=OptimizationUser;pwd=ProjectFFAR2022;database=USDA";
                string sql = "Select weeks, prod, cust, AVG(price) as price from USDA.pricesForOptimization ";
                string whereclause = " ";
                string croplist = " ";
                string locationlist = " ";
                if (cropList.Count > 0)
                {
                    croplist += " (";
                    for (int i = 0; i < cropList.Count; i++)
                    {
                        croplist += " prod = '" + cropList[i] + "_P" + "' or ";
                    }
                    croplist = croplist.Substring(0, croplist.Length - 3);
                    croplist += " ) ";
                }
                if (locationList.Count > 0)
                {
                    locationlist += " (";
                    for (int i = 0; i < locationList.Count; i++)
                    {
                        locationlist += "cust = '" + locationList[i] + "' or ";
                    }
                    locationlist = locationlist.Substring(0, locationlist.Length - 3);
                    locationlist += " ) ";
                }
                if (cropList.Count != 0 && locationList.Count != 0)
                {
                    whereclause += " where " + croplist + " and " + locationlist;
                }
                else
                {
                    if (cropList.Count != 0)
                    {
                        whereclause += " where " + croplist;
                    }
                    else if (locationList.Count != 0)
                    {
                        whereclause += " where " + locationlist;
                    }
                }
                sql += whereclause;
                sql += " group by  weeks, prod, cust ";
                connection = new MySqlConnection(myConnectionString);
                connection.Open();
                sda = new MySqlDataAdapter();
                cmd = new MySqlCommand(sql, connection);
                sda.SelectCommand = cmd;
                sda.Fill(dt);
                connection.Close();
                sda.Dispose();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dt;
        }

        static public DataTable queryYieldDB(List<string> cropList, List<string> locationList)
        {
            DataTable dt = new DataTable();
            try
            {
                string myConnectionString = "server=db-asu-datapipeline.cbhji75irubx.us-east-2.rds.amazonaws.com;uid=OptimizationUser;pwd=ProjectFFAR2022;database=USDA";
                string sql = "Select YIELD from USDA.yieldScenarios_Nov18_2022 ";
                string whereclause = " ";
                string croplist = " ";
                string locationlist = " ";
                if (cropList.Count > 0)
                {
                    croplist += " (";
                    for (int i = 0; i < cropList.Count; i++)
                    {
                        croplist += " CROP = '" + cropList[i] + "' or ";
                    }
                    croplist = croplist.Substring(0, croplist.Length - 3);
                    croplist += " ) ";
                }
                if (locationList.Count > 0)
                {
                    locationlist += " (";
                    for (int i = 0; i < locationList.Count; i++)
                    {
                        locationlist += "LOC = '" + locationList[i] + "' or ";
                    }
                    locationlist = locationlist.Substring(0, locationlist.Length - 3);
                    locationlist += " ) ";
                }
                if (cropList.Count != 0 && locationList.Count != 0)
                {
                    whereclause += " where " + croplist + " and " + locationlist;
                }
                else
                {
                    if (cropList.Count != 0)
                    {
                        whereclause += " where " + croplist;
                    }
                    else if (locationList.Count != 0)
                    {
                        whereclause += " where " + locationlist;
                    }
                }

                //Add to where clause in order to circumvent the issue of 104 WEEKH in the data base
                whereclause += " and WEEKH <=102";

                sql += whereclause;
                sql += " ORDER BY WEEKP, WEEKH, CROP, LOC, SCEN ";   //make sure the data is sorted correctly
                connection = new MySqlConnection(myConnectionString);
                connection.Open();
                sda = new MySqlDataAdapter();
                cmd = new MySqlCommand(sql, connection);
                cmd.CommandTimeout = 6000;
                sda.SelectCommand = cmd;
                sda.Fill(dt);
                connection.Close();
                sda.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dt;
        }

        static public DataTable queryExpYieldDB(List<string> cropList, List<string> locationList)
        {
            DataTable dt = new DataTable();
            try
            {
                string myConnectionString = "server=db-asu-datapipeline.cbhji75irubx.us-east-2.rds.amazonaws.com;uid=OptimizationUser;pwd=ProjectFFAR2022;database=USDA";
                string sql = "Select WEEKP, WEEKH, CROP,  LOC, Salv, AVG(YIELD) as YIELD from USDA.yieldScenarios_Nov18_2022 "; 
                string whereclause = " ";
                string croplist = " ";
                string locationlist = " ";
               
                if (cropList.Count > 0)
                {
                    croplist += " (";
                    for (int i = 0; i < cropList.Count; i++)
                    {
                        croplist += " CROP = '" + cropList[i] + "' or ";
                    }
                    croplist = croplist.Substring(0, croplist.Length - 3);
                    croplist += " ) ";
                }
                if (locationList.Count > 0)
                {
                    locationlist += " (";
                    for (int i = 0; i < locationList.Count; i++)
                    {
                        locationlist += "LOC = '" + locationList[i] + "' or ";
                    }
                    locationlist = locationlist.Substring(0, locationlist.Length - 3);
                    locationlist += " ) ";
                }
                if (cropList.Count != 0 && locationList.Count != 0)
                {
                    whereclause += " where " + croplist + " and " + locationlist;
                }
                else
                {
                    if (cropList.Count != 0)
                    {
                        whereclause += " where " + croplist;
                    }
                    else if (locationList.Count != 0)
                    {
                        whereclause += " where " + locationlist;
                    }
                }
                whereclause += " and WEEKH <=102";

                sql += whereclause;
                sql += " group by WEEKP, WEEKH, CROP,  LOC, Salv";
                sql += " order by WEEKP, WEEkH, CROP, LOC";
                connection = new MySqlConnection(myConnectionString);
                connection.Open();
                sda = new MySqlDataAdapter();
                cmd = new MySqlCommand(sql, connection);
                cmd.CommandTimeout = 3000;
                sda.SelectCommand = cmd;
                sda.Fill(dt);
                connection.Close();
                sda.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dt;
        }

        static public List<YIELDModel> DataTableToYieldModelList(DataTable dataTable, List<YIELDModel> yieldList)
        {
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                double yields = 0.0;
                if (dataTable.Rows[i][0] != DBNull.Value)
                    yields = Convert.ToDouble(dataTable.Rows[i][0]);
                else
                {
                    Console.WriteLine("Error parsing yields in DataTableToYieldModelList function.");
                    break;
                }
                YIELDModel yieldObject = new YIELDModel();
                yieldObject = new YIELDModel
                {
                    Yield = yields
                };
                yieldList.Add(yieldObject);
            }
            return yieldList;
        }

        static public List<YIELDExpModel> DataTableToExpYieldModelList(DataTable dataTable, List<YIELDExpModel> yieldList)
        {
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                double yields = 0.0;
                if (dataTable.Rows[i][5] != DBNull.Value)
                    yields = Convert.ToDouble(dataTable.Rows[i][5]);
                else
                {
                    Console.WriteLine("Error parsing yields in DataTableToExpYieldModelList function.");
                    break;
                }
                YIELDExpModel yieldObject = new YIELDExpModel();
                yieldObject = new YIELDExpModel
                {
                    Yield = yields
                };
                yieldList.Add(yieldObject);
            }
            return yieldList;
        }

      
    }
}
