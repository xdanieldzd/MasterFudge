using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

using Nini.Config;

using MasterFudge.Emulation.Units;

namespace MasterFudge
{
    public static class Configuration
    {
        public static readonly string UserDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);

        static readonly string configFilename = "Settings.xml";
        static readonly string configFilePath = Path.Combine(UserDataPath, configFilename);

        static readonly string sectionOptions = "Options";
        static readonly string sectionSystem = "System";
        static readonly string sectionPaths = "Paths";
        static readonly string sectionInputGeneral = "Input";
        static readonly string sectionInputPlayer1 = "InputPlayer1";

        static readonly string[] sections = new string[] { sectionOptions, sectionSystem, sectionPaths, sectionInputGeneral, sectionInputPlayer1 };

        static IConfigSource source;

        public static bool LimitFPS
        {
            get { return (source.Configs[sectionOptions].GetBoolean("LimitFPS", true)); }
            set { source.Configs[sectionOptions].Set("LimitFPS", value); }
        }

        public static bool SoundEnabled
        {
            get { return (source.Configs[sectionOptions].GetBoolean("SoundEnabled", true)); }
            set { source.Configs[sectionOptions].Set("SoundEnabled", value); }
        }

        public static bool BootstrapEnabled
        {
            get { return (source.Configs[sectionOptions].GetBoolean("BootstrapEnabled", false)); }
            set { source.Configs[sectionOptions].Set("BootstrapEnabled", value); }
        }

        public static bool NoiseEffectEnabled
        {
            get { return (source.Configs[sectionOptions].GetBoolean("NoiseEffectEnabled", false)); }
            set { source.Configs[sectionOptions].Set("NoiseEffectEnabled", value); }
        }

        public static BaseUnitRegion BaseUnitRegion
        {
            get { return ((BaseUnitRegion)Enum.Parse(typeof(BaseUnitRegion), (source.Configs[sectionSystem].GetString("BaseUnitRegion", "ExportNTSC")))); }
            set { source.Configs[sectionSystem].Set("BaseUnitRegion", value); }
        }

        public static string LastCartridgePath
        {
            get { return (source.Configs[sectionPaths].GetString("LastCartridgePath", string.Empty)); }
            set { source.Configs[sectionPaths].Set("LastCartridgePath", value); }
        }

        public static string MasterSystemBootstrapPath
        {
            get { return (source.Configs[sectionPaths].GetString("MasterSystemBootstrapPath", string.Empty)); }
            set { source.Configs[sectionPaths].Set("MasterSystemBootstrapPath", value); }
        }

        public static string GameGearBootstrapPath
        {
            get { return (source.Configs[sectionPaths].GetString("GameGearBootstrapPath", string.Empty)); }
            set { source.Configs[sectionPaths].Set("GameGearBootstrapPath", value); }
        }

        public static string[] RecentFiles
        {
            get { return source.Configs[sectionPaths].GetString("RecentFiles", string.Empty).Split('|'); }
            set { source.Configs[sectionPaths].Set("RecentFiles", string.Join("|", value)); }
        }

        public static Keys KeyReset
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputGeneral].GetString("Reset", "Back")))); }
            set { source.Configs[sectionInputGeneral].Set("Reset", value); }
        }

        public static Keys KeyP1Up
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("Up", "Up")))); }
            set { source.Configs[sectionInputPlayer1].Set("Up", value); }
        }

        public static Keys KeyP1Down
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("Down", "Down")))); }
            set { source.Configs[sectionInputPlayer1].Set("Down", value); }
        }

        public static Keys KeyP1Left
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("Left", "Left")))); }
            set { source.Configs[sectionInputPlayer1].Set("Left", value); }
        }

        public static Keys KeyP1Right
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("Right", "Right")))); }
            set { source.Configs[sectionInputPlayer1].Set("Right", value); }
        }

        public static Keys KeyP1Button1
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("Button1", "A")))); }
            set { source.Configs[sectionInputPlayer1].Set("Button1", value); }
        }

        public static Keys KeyP1Button2
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("Button2", "S")))); }
            set { source.Configs[sectionInputPlayer1].Set("Button2", value); }
        }

        public static Keys KeyP1StartPause
        {
            get { return ((Keys)Enum.Parse(typeof(Keys), (source.Configs[sectionInputPlayer1].GetString("StartPause", "Enter")))); }
            set { source.Configs[sectionInputPlayer1].Set("StartPause", value); }
        }

        static Configuration()
        {
            if (!File.Exists(configFilePath)) File.WriteAllText(configFilePath, "<Nini>\n</Nini>\n");

            source = new XmlConfigSource(configFilePath);
            source.AutoSave = true;

            foreach (string section in sections)
                if (source.Configs[section] == null)
                    source.AddConfig(section);
        }
    }
}
