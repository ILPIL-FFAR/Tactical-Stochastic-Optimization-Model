using ModelStochastic6;
using System.Collections.Generic;
using WebApiDemoNetCore.Models;
using WebApiDemoNetCore.Models.Model3S;

namespace Model3_CaseStudy_2years
{
    /* 
     * START HERE                     THIS READS PRICES FROM .CSV FILE RATHER THAN FROM DATABASE
     */
    class Program
    {
        /*This code works and integrates easily with the web interface, we are following the structure provided from Luis (from Octavio's team).
        * This class "Program.cs" simulates the interface with the web UI. The crop and locations parameters are defined here, to select or unselect
        * crops or locations comment them on each section.
        * 
        * '15 scen finish 41 iter in 223 minutes
        */
        static void Main(string[] args)
        {
            // SET initial WEEK
            int init_week = 1;

            UserInputs userInputs = new UserInputs();
            // Inputs Selected by the user
            userInputs.deterministic = true;   // Use deterministic model (true) or stochastic (false)
            userInputs.num_scenarios =5;      // Number of scenarios, if using stochastic model
            userInputs.useContract = false;     // Use a contract in the model (true) or no contract (false) .. no contract means no minimum demand to supply

            // Not selected by the user for now (could be implemented later)
            userInputs.activate_target = false;
            userInputs.maxIterations = 1000;      // Use 12 for testing
            userInputs.use_random_scenarios =false; //Use a new set of random scenarios at each iteration
            userInputs.gap_change_limit = 1.0 / 100.0;   // Set stopping criteria for algorithm to changes in current GAP, to 0.1 %
            userInputs.use_random_fixed_scenarios =false; // sample randomly from available scenarios and use them until termination



            //Selected Crops & Locations
            Model3SRequest model3SRequest = new Model3SRequest()
            {
                cropsSelected = getCropsSelected(),
                locationsSelected = getLocationsSelected()
            };

            //Run the algorithm
            TacticalModelProgramModel3S TMPM3S = new TacticalModelProgramModel3S(init_week);
            var resultModel = TMPM3S.RunModel(model3SRequest, userInputs);
        }

        public static List<CropsConfigurationModel> getCropsSelected()
        {
            // Create each of the crops with its parameters:
            CropsConfigurationModel BNS_CROP = new CropsConfigurationModel()
            {
                crop = "BNS",
                cplant = 1053,
                pslav = 0.05,
                labp = 19.44,
                labh = 159,
                ccrop = "BNS",
                dharv = 4,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0002,
                laborP = 0.01034
            };

            CropsConfigurationModel BRSP_CROP = new CropsConfigurationModel()
            {
                crop = "BRSP",
                cplant = 2016,
                pslav = 0.05,
                labp = 82.22,
                labh = 140,
                ccrop = "BRSP",
                dharv = 4,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0014,
                laborP = 0.027266667
            };

            CropsConfigurationModel CAUL_CROP = new CropsConfigurationModel()
            {
                crop = "CAUL",
                cplant = 1386,
                pslav = 0.05,
                labp = 34.96,
                labh = 323,
                ccrop = "CAUL",
                dharv = 4,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0002,
                laborP = 0.0198
            };

            CropsConfigurationModel CEL_CROP = new CropsConfigurationModel()
            {
                crop = "CEL",
                cplant = 3360,
                pslav = 0.05,
                labp = 17.46,
                labh = 495,
                ccrop = "CEL",
                dharv = 4,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0002,
                laborP = 0.0198
            };

            CropsConfigurationModel CHY_CROP = new CropsConfigurationModel()
            {
                crop = "CHY",
                cplant = 2068,
                pslav = 0.05,
                labp = 85.1,
                labh = 484,
                ccrop = "CHY",
                dharv = 4,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0002,
                laborP = 0.0198
            };

            CropsConfigurationModel CUX_CROP = new CropsConfigurationModel()
            {
                crop = "CUX",
                cplant = 2443,
                pslav = 0.05,
                labp = 59.67,
                labh = 468,
                ccrop = "CUX",
                dharv = 3,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0023,
                laborP = 0.02
            };

            CropsConfigurationModel LET_CROP = new CropsConfigurationModel()
            {
                crop = "LET",
                cplant = 2874,
                pslav = 0.05,
                labp = 8.66,
                labh = 308,
                ccrop = "LET",
                dharv = 3,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0204,
                laborP = 0.06673
            };

            CropsConfigurationModel PEP_CROP = new CropsConfigurationModel()
            {
                crop = "PEP",
                cplant = 4904,
                pslav = 0.05,
                labp = 79.94,
                labh = 396,
                ccrop = "PEP",
                dharv = 3,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0003,
                laborP = 0.0333
            };

            CropsConfigurationModel SPN_CROP = new CropsConfigurationModel()
            {
                crop = "SPN",
                cplant = 878,
                pslav = 0.05,
                labp = 20.15,
                labh = 220,
                ccrop = "SPN",
                dharv = 3,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0003,
                laborP = 0.0333
            };

            CropsConfigurationModel TOM_CROP = new CropsConfigurationModel()
            {
                crop = "TOM",
                cplant = 2011,
                pslav = 0.05,
                labp = 6.54,
                labh = 51,
                ccrop = "TOM",
                dharv = 3,
                water = 0.0001,
                minl = 1,
                maxl = 100,
                laborH = 0.0001,
                laborP = 0.02027
            };

            List<CropsConfigurationModel> listCropsSelected = new List<CropsConfigurationModel>();

            /* 
             * To remove crops, comment the corresponding line below (ALSO they should be in alphabetical order:
             */

            //listCropsSelected.Add(BNS_CROP);
            //listCropsSelected.Add(BRSP_CROP);
            listCropsSelected.Add(CAUL_CROP);
            listCropsSelected.Add(CEL_CROP);
            //listCropsSelected.Add(CHY_CROP);
            listCropsSelected.Add(CUX_CROP);
            listCropsSelected.Add(LET_CROP);
            listCropsSelected.Add(PEP_CROP);
            //listCropsSelected.Add(SPN_CROP);
            listCropsSelected.Add(TOM_CROP);

            
            
            

            return listCropsSelected;
        }

        public static List<LocationsConfigurationModel> getLocationsSelected()
        {
            // Add each location ant its parameters
            LocationsConfigurationModel ALBUQUERQUE_LOC = new LocationsConfigurationModel()
            {
                location = "Albuquerque",
                abbr = "Albuquerque",
                la = 60
            };

            LocationsConfigurationModel ASPEN_LOC = new LocationsConfigurationModel()
            {
                location = "Aspen",
                abbr = "Aspen",
                la = 10
            };

            LocationsConfigurationModel LAS_CRUCES_LOC = new LocationsConfigurationModel()
            {
                location = "Las_Cruces",
                abbr = "Las_Cruces",
                la = 30
            };

            LocationsConfigurationModel PHOENIX_LOC = new LocationsConfigurationModel()
            {
                location = "Phoenix",
                abbr = "Phoenix",
                la = 40
            };

            LocationsConfigurationModel TUCSON_LOC = new LocationsConfigurationModel()
            {
                location = "Tucson",
                abbr = "Tucson",
                la = 30
            };

            LocationsConfigurationModel YUMA_LOC = new LocationsConfigurationModel()
            {
                location = "Yuma",
                abbr = "Yuma",
                la = 30
            };

            //ADD LOCATIONS OR SELECTED
            List<LocationsConfigurationModel> listLocationsSelected = new List<LocationsConfigurationModel>();

            /* 
             * To remove locations, comment the corresponding line below:
             */

            listLocationsSelected.Add(ALBUQUERQUE_LOC);
            listLocationsSelected.Add(ASPEN_LOC);
            listLocationsSelected.Add(LAS_CRUCES_LOC);
            listLocationsSelected.Add(PHOENIX_LOC);
            listLocationsSelected.Add(TUCSON_LOC);
            listLocationsSelected.Add(YUMA_LOC);



            return listLocationsSelected;
        }

    }
}
