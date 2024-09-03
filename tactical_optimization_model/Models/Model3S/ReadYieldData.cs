using ModelStochastic6.Models;
using ModelStochastic6.Models.Inputs;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using WebApiDemoNetCore.Models.Model3S;

namespace ModelStochastic6
{
    class ReadYieldData2
    /*
     *                     CLASS NOT USED! 
     * 
     */
    {
        public InputYieldData readYieldData(string inputFolder, Model3SRequest model3SRequest)
        {



            //NEW ############################################################################
            string sheetName = "Sheet1";
            string strFile = "";
            DataTable dtXLS = new DataTable();
            string dataTableToJsonString = "";

            //CROPS & LOCATION SELECTED
            List<string> cropsList = model3SRequest.cropsSelected.OrderBy(cs => cs.ccrop).Select(cs => cs.ccrop).Distinct().ToList();
            List<string> locationsList = model3SRequest.locationsSelected.OrderBy(ls => ls.abbr).Select(ls => ls.abbr.Replace(" ", "_")).Distinct().ToList();

            List<string> ccropsList = model3SRequest.cropsSelected.OrderBy(cs => cs.ccrop).Select(cs => cs.ccrop).Distinct().ToList();

            //READING YIELD.xlsx
            strFile = "YIELD_ext.xlsx";

            //Console.WriteLine("READING DATA: " + strFile);
            dtXLS = ReadExcelFileToDataTable(inputFolder, strFile, sheetName);
            List<YIELDModel> YieldList = new List<YIELDModel>();


            YieldList = DataTableToYieldModelList(dtXLS, YieldList);

            //PPDATA
            /*
            YieldList = (from yl in YieldList
                         where
                         cropsList.Contains(yl.CROP) && locationsList.Contains(yl.LOC)
                         select yl).ToList();
            */

            return new InputYieldData()
            {
                YieldList = YieldList,
            };
            //#######################################################################################


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

        //Write new method to covert data table to what we want rather than to json string then data table...
        static public List<YIELDModel> DataTableToYieldModelList(DataTable dataTable, List<YIELDModel> yieldList)
        {
            //Iterate through the rows of the data table
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                //store the first elements (indexes and salv) into List<double>
                //Store remaining columns (yield values across scenatios) in an array that is stored as the last element in the list
                List<String> indices = new List<String>();
                for (int j = 0; j < 5; j++)
                {
                    indices.Add(Convert.ToString(dataTable.Rows[i][j]));
                }
                //new list for colleting yields for this row across all scenarios
                double yields = 0.0;
                for (int z = 5; z < dataTable.Columns.Count; z++)
                {
                    if (dataTable.Rows[i][z] != DBNull.Value)
                        yields = Convert.ToDouble(dataTable.Rows[i][z]);
                    else
                        break;
                }

                // Make a yieldModel object to gather information
                YIELDModel yieldObject = new YIELDModel();

                yieldObject = new YIELDModel
                {
                    //WEEKP = Convert.ToDouble(indices[0]),
                    //WEEKH = Convert.ToDouble(indices[1]),
                    //CROP = indices[2],
                    //LOC = indices[3],
                    //Salv = Convert.ToDouble(indices[4]),
                    Yield = yields
                };

                //Add row to the list of yield objects
                yieldList.Add(yieldObject);
            }

            return yieldList;
        }



    }
}
