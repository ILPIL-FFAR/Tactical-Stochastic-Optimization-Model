using ModelStochastic6.Models.Inputs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelStochastic6
{
    struct UserInputs
    {
        public bool deterministic;
        public bool activate_target;
        public bool useContract;
        public bool use_random_scenarios;
        public bool use_random_fixed_scenarios;
        public int maxIterations;
        public int num_scenarios;
        public double gap_change_limit;
    }
}
