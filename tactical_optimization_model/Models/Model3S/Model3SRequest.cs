using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models.Model3S
{
    public class Model3SRequest
    {
        public List<CropsConfigurationModel> cropsSelected { get; set; }
        public List<LocationsConfigurationModel> locationsSelected { get; set; }
        public UserRequestModel user { get; set; }
    }
}
