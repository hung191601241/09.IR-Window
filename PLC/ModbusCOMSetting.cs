using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Development
{
    public class ModbusCOMSetting : COMSetting
    {
        public ushort AddressSlave { get; set; }

        public ModbusCOMSetting()
        {
            AddressSlave = 1;
        }
    }
}
