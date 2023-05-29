using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LocoSwap
{
    public class TrackRibbon : ModelBase
    {
        public string RibbonId { get; internal set; }
        public double Length { get; internal set; }
        public List<HeightPoint> HeightPoints { get; internal set; }
        public bool SuperElevated { get; internal set; }
        public bool LockCounterWhenModified { get; internal set; }
    }
}

