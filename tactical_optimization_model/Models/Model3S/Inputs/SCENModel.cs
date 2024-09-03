using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models
{
    struct SCENModel
    {
        public double SCEN { get; set; }
        public double Prob { get; set; }

        public void setProb(double pr)
        {
            this.Prob = 1.0 * pr;
        }
    }
}
