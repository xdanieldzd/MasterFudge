using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using MasterFudge.Emulation;

namespace MasterFudge
{
    public partial class OptionsForm : Form
    {
        public OptionsFormData OptionsData { get; private set; }

        public OptionsForm(OptionsFormData data)
        {
            InitializeComponent();

            OptionsData = data;

            lbKeyConfigKeys.DataSource = Enum.GetValues(typeof(Keys));
            lbKeyConfigButton.DataSource = Enum.GetValues(typeof(Buttons));

            chkBootstrapEnable.DataBindings.Add("Checked", OptionsData, "UseBootstrap", false, DataSourceUpdateMode.OnPropertyChanged);
            gbBootstrap.DataBindings.Add("Enabled", OptionsData, "UseBootstrap", false, DataSourceUpdateMode.OnPropertyChanged);
            txtBootstrapSMS.DataBindings.Add("Text", OptionsData, "MasterSystemBootstrapPath", false, DataSourceUpdateMode.OnPropertyChanged);
            txtBootstrapGG.DataBindings.Add("Text", OptionsData, "GameGearBootstrapPath", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void btnBootstrapSMSBrowse_Click(object sender, EventArgs e)
        {
            ofdBootstrapSMS.InitialDirectory = Path.GetDirectoryName(OptionsData.MasterSystemBootstrapPath);
            ofdBootstrapSMS.FileName = Path.GetFileName(OptionsData.MasterSystemBootstrapPath);

            if (ofdBootstrapSMS.ShowDialog() == DialogResult.OK)
            {
                OptionsData.MasterSystemBootstrapPath = ofdBootstrapSMS.FileName;
            }
        }

        private void btnBootstrapGGBrowse_Click(object sender, EventArgs e)
        {
            ofdBootstrapGG.InitialDirectory = Path.GetDirectoryName(OptionsData.GameGearBootstrapPath);
            ofdBootstrapGG.FileName = Path.GetFileName(OptionsData.GameGearBootstrapPath);

            if (ofdBootstrapGG.ShowDialog() == DialogResult.OK)
            {
                OptionsData.GameGearBootstrapPath = ofdBootstrapGG.FileName;
            }
        }

        private void lbKeyConfigButton_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbKeyConfigKeys.SelectedItem = OptionsData?.Player1Buttons[(Buttons)(sender as ListBox).SelectedItem];
        }

        private void lbKeyConfigKeys_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OptionsData != null && lbKeyConfigButton.SelectedItem != null)
                OptionsData.Player1Buttons[(Buttons)lbKeyConfigButton.SelectedItem] = (Keys)(sender as ListBox).SelectedItem;
        }
    }

    public class OptionsFormData
    {
        public bool UseBootstrap { get; set; }
        public string MasterSystemBootstrapPath { get; set; }
        public string GameGearBootstrapPath { get; set; }
        public Dictionary<Buttons, Keys> Player1Buttons { get; set; }
        public bool TVSystemNTSC { get; set; }
        public bool TVSystemPAL { get { return !TVSystemNTSC; } set { TVSystemNTSC = !value; } }
        public bool RegionJapan { get; set; }
        public bool RegionExport { get { return !RegionJapan; } set { RegionJapan = !value; } }
    }
}
