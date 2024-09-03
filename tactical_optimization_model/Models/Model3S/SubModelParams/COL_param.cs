using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6.Models.SubModelParams
{
    class COL_param
    {
        public WEEKHModel weekH { get; set; }
        public PRODModel prod { get; set; }
        public QUALModel qual { get; set; }
    }
}
