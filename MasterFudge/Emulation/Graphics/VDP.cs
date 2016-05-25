using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterFudge.Emulation.Graphics
{
    public class VDP
    {
        byte[] registers;
        byte[] vram;

        public VDP()
        {
            registers = new byte[0x10];
            vram = new byte[0x4000];
        }
        
        //
    }
}
