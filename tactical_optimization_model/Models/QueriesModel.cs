using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class GetCropsByUserModel
    {
        public UserRequestModel user { get; set; }
        public bool? active { get; set; }
    }

    public class GetGrowersByUserModel
    {
        public UserRequestModel user { get; set; }
        public bool? active { get; set; }
    }

    public class GetUserModel
    {
        public UserRequestModel user { get; set; }
        public string usId { get; set; }
        public bool? active { get; set; }
    }

    public class GetOfferAttribute
    {
        public UserRequestModel user { get; set; }
        public bool? active { get; set; }
    }

    public class GetLocationsByUserModel
    {
        public UserRequestModel user { get; set; }
        public bool? active { get; set; }
    }

    public class UpdateCropByIdRLTModel
    {
        public UserRequestModel user { get; set; }
        public CropsConfigurationModel crop { get; set; }
    }

    public class SwitchStatusCropByIdRLTModel
    {
        public UserRequestModel user { get; set; }
        public CropsConfigurationModel crop { get; set; }
    }

    public class UpdateLocationByIdRLTModel
    {
        public UserRequestModel user { get; set; }
        public LocationsConfigurationModel location { get; set; }
    }

    public class SwitchStatusLocationByIdRLTModel
    {
        public UserRequestModel user { get; set; }
        public LocationsConfigurationModel location { get; set; }
    }

    public class ContractsResultModel
    {
        public UserRequestModel user { get; set; }
        public string contractId { get; set; }
        public string name { get; set; }
        public string incotermId { get; set; }
        public string locationId { get; set; }
        public string images { get; set; }
        public DateTime dateExpiration { get; set; }
    }

    public class ContractsResultModelCSV
    {
        public UserRequestModel user { get; set; }
        public DateTime dateExpiration { get; set; }
        public string name { get; set; }
        public string incotermId { get; set; }
        public string locationId { get; set; }
        public string images { get; set; }
    }

    public class ContractDetailModel
    {
        public ContractsModel contract { get; set; }
        public UserRequestModel user { get; set; }
    }

    public class GetContractsByUserModel
    {
        public UserRequestModel user { get; set; }
        public bool? active { get; set; }

    }

    public class AddOfferByUserModel
    {
        public UserRequestModel user { get; set; }
        public OffersModel offer { get; set; }
        public List<DetailOfferModel> detailOffer { get; set; }

    }

    public class GetOffersByUserModel
    {
        public UserRequestModel user { get; set; }
        public bool? open { get; set; }
        public bool? active { get; set; }

    }

    public class OffersDetailModel
    {
        public OffersModel offer { get; set; }
        public List<DetailOfferModel> detailOffer { get; set; }
    }


    public class DeleteOfferByOfferIdModel
    {
        public UserRequestModel user { get; set; }
        public string offerId { get; set; }
    }

    public class SwitchStatusOfferByOfferIdModel
    {
        public UserRequestModel user { get; set; }
        public string offerId { get; set; }
        public bool active { get; set; }
    }

    public class GetDataModel
    {
        public UserRequestModel user { get; set; }
        public bool? active { get; set; }
    }

    public class UpdateGrowerAdditionalInfodModel
    {
        public UserRequestModel user { get; set; }
        public string us_id { get; set; }
        public string phone { get; set; }
        public string website { get; set; }
        public string description { get; set; }
        public string images { get; set; }
    }

    public class UpdateUserAttributesModel
    {
        public UserRequestModel user { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string company { get; set; }
    }

    public class UpdateUserImageModel
    {
        public UserRequestModel user { get; set; }
        public string image_url { get; set; }
        public string image_name { get; set; }
    }

    public class GetYields
    {
        public UserRequestModel? user { get; set; }
        public YieldInputModel yieldInputModel { get; set; }
    }
}
