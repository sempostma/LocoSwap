using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LocoSwap
{
    public class HeightPoint : ModelBase
    {
        public bool Manual { get; internal set; }
        public double Position { get; internal set; }
        public double Height { get; internal set; }
    }
}

