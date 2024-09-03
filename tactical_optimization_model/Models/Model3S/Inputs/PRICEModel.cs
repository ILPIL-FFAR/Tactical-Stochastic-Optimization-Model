using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct PRICEModel
    {
        public double WEEKS { get; set; }
        public string PROD { get; set; }
        public string CUST { get; set; }
        public double SCEN { get; set; }
        public double Price { get; set; }
    }
}
