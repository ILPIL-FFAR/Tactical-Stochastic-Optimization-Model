using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiDemoNetCore.Models
{
    public class MessageResponse
    {
        public bool hasError { get; set; }
        public int statusCode { get; set; }
        public string statusMessage { get; set; }
        public string data { get; set; }
    }
}
