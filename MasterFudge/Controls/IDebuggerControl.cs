using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MasterFudge.Emulation;

namespace MasterFudge.Controls
{
    interface IDebuggerControl
    {
        void UpdateControl(BaseUnit.CoreDebugSnapshot snapshot);
    }
}
