using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LocoSwap
{
    public class NetworkInfo : ModelBase
    {
        public string NetworkDevString { get; set; }

        public XElement RibbonContainer { get; set; }

        public List<TrackRibbon> TrackRibbons { get; internal set; }
    }
}

