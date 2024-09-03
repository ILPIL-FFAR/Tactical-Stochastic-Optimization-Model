using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class ContractsModel
    {
        public string rlt_cct_cctdefault_Id { get; set; }
        public string contract_id { get; set; }
        public int weeks { get; set; }
        public string prod { get; set; }
        public string cust { get; set; }
        public double dem2 { get; set; }
        public double price { get; set; }
    }

    public partial class ContractModel
    {
        public string contractId { get; set; }
        public string name { get; set; }
        public string AccId { get; set; }
        public string location { get; set; }
        public string locationId { get; set; }
        public string incoterm { get; set; }
        public string incotermId { get; set; }
        public bool? active { get; set; }
        public DateTime dateRegistered { get; set; }
        public string status { get; set; }
        public string statusId { get; set; }
        public string userId { get; set; }
        public string userFirstName { get; set; }
        public string userLastName { get; set; }
        public DateTime dateExpiration { get; set; }
        public double totalPrice { get; set; }
        public double totalQuantity { get; set; }
        public double detailQuantity { get; set; }
        public int miniContainerNumber { get; set; }
        public List<string> crops { get; set; }
        public string images { get; set; }
    }

}
