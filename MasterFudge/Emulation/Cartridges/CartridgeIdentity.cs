using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Cartridges
{
    public class CartridgeIdentity
    {
        public KnownMapper Mapper { get; set; }
        public BaseUnitRegion UnitRegion { get; set; }
        public BaseUnitType UnitType { get; set; }

        public CartridgeIdentity()
        {
            Mapper = KnownMapper.DefaultSega;
            UnitRegion = BaseUnitRegion.Default;
            UnitType = BaseUnitType.Default;
        }
    }
}
