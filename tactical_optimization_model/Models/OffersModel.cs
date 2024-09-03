using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class OffersModel
    {
        public string offerId { get; set; }
        public string accId { get; set; }
        public string incoterm { get; set; }
        public string incotermId { get; set; }
        public string package { get; set; }
        public string packageId { get; set; }
        public string location { get; set; }
        public string locationId { get; set; }
        public string quality { get; set; }
        public string qualityId { get; set; }
        public string specialDenomination { get; set; }
        public string specialDenominationId { get; set; }
        public string usId { get; set; }
        public string userFirstName { get; set; }
        public string userLastName { get; set; }
        public DateTime dateRegistered { get; set; }
        public bool? active { get; set; }
        public bool open { get; set; }
        public DateTime dateToSell { get; set; }
        public string images { get; set; }
    }


    public class DetailOfferModel
    {
        public string rltOfrCropId { get; set; }
        public string offerId { get; set; }
        public string cropId { get; set; }
        public string crop { get; set; }
        public double quantity { get; set; }
        public int miniContainersNumber { get; set; }
        public double price { get; set; }
        public bool? active { get; set; }
    }
}
