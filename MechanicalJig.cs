using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLaserCuttingInput
{
    class MechanicalJig
    {
        public int rowCount { get; set; }
        public int columnCount { get; set; }

        public MechanicalJig()
        {
            rowCount = 5;
            columnCount = 2;
        }
    }
}
