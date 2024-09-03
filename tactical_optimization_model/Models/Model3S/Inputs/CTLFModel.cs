using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct CTLFModel
    {
        public string LOC { get; set; }
        public string FAC { get; set; }
        public string TRANS { get; set; }
        public double TimeLF { get; set; }
        public double T10 { get; set; }
        public double CTLF { get; set; }
    }
}
