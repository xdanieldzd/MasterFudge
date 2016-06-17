using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation.Units;

namespace MasterFudge.Emulation.Media
{
    public class MediaIdentity
    {
        public KnownMapper Mapper { get; set; }
        public BaseUnitRegion UnitRegion { get; set; }
        public BaseUnitType UnitType { get; set; }

        public MediaIdentity()
        {
            Mapper = KnownMapper.DefaultSega;
            UnitRegion = BaseUnitRegion.Invalid;
            UnitType = BaseUnitType.Invalid;
        }
    }
}
