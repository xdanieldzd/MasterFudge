using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MasterFudge.Emulation;

namespace MasterFudge
{
    public class InputConfig
    {
        public Keys Up { get; set; }
        public Keys Down { get; set; }
        public Keys Left { get; set; }
        public Keys Right { get; set; }
        public Keys Button1 { get; set; }
        public Keys Button2 { get; set; }
        public Keys StartPause { get; set; }
        public Keys Reset { get; set; }

        public InputConfig()
        {
            Up = Keys.Up;
            Down = Keys.Down;
            Left = Keys.Left;
            Right = Keys.Right;
            Button1 = Keys.A;
            Button2 = Keys.S;
            StartPause = Keys.Enter;
            Reset = Keys.Back;
        }
    }
}
