using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class UserModel
    {

    }

    public partial class UserRequestModel
    {
        public string accTkn { get; set; }
        public string usId { get; set; }
    }

    public partial class User
    {
        public string accId { get; set; }

        public string accTkn { get; set; }

        public string usId { get; set; }

        [Required]
        public string usFirstName { get; set; }

        [Required]
        public string usLastName { get; set; }

        [Required]
        public string usCompanyAffiliation { get; set; }

        [Required]
        public string usEmail { get; set; }

        [Required]
        public string usTypeUserId { get; set; }

        public string usTypeUser { get; set; }

        public DateTime? usDateRegistered { get; set; }
    }

    public partial class Grower
    {
        public string accId { get; set; }
        public string usId { get; set; }
        public string usFirstName { get; set; }
        public string usLastName { get; set; }
        public DateTime? usDateRegistered { get; set; }
        public string usCompanyAffiliation { get; set; }
        public double landAvaliable { get; set; }
        public List<string> crops { get; set; }
        public List<string> locations { get; set; }
        public List<GrowerCropsConfigurationModel> rltCrops { get; set; }
        public List<GrowerLocationsConfigurationModel> rltLocations { get; set; }
        public string status { get; set; }

    }
}
