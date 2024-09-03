using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.Outputs.SubModel
{
    public class Constraint
    {
        public string name { get; set; }
        public int startAt { get; set; }
        public int endAt { get; set; }
        public int length { get; set; }
    }
}
