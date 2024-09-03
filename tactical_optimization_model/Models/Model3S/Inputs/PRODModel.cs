using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct PRODModel
    {
        public string PROD { get; set; }
        public string Pcrop { get; set; }
        public double Weight { get; set; }
        public double Pallet { get; set; }
        public double LabF { get; set; }
        public double TraF { get; set; }
        public double SL { get; set; }
        public double Ccase { get; set; }
    }
}
