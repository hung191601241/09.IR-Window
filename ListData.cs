using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLaserCuttingInput
{
     class ListData
    {
        public List<String> PkgQrListCH1 { get; set; }
        public List<String> ResultVisionListCH1 { get; set; }
        public List<String> PkgQrListCH2 { get; set; }
        public List<String> ResultVisionListCH2 { get; set; }
        public List<bool> PkgVisionListCH1 { get; set; }
        public List<bool> PkgVisionListCH2 { get; set; }

        public List<int> ColoVisionCH1 { get; set; }
        public List<int> ColoVisionCH2 { get; set; }
    }
}
