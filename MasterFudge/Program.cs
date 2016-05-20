using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterFudge
{
    static class Program
    {
        // TODO: temporary
        public static Logger Log;

        [STAThread]
        static void Main()
        {
            Log = new Logger();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
