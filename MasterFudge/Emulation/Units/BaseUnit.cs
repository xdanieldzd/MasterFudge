using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

using MasterFudge.Emulation.Media;

namespace MasterFudge.Emulation.Units
{
    public enum BaseUnitType
    {
        Invalid,
        MasterSystem,
        GameGear,
        SC3000
    }

    public enum BaseUnitRegion
    {
        Invalid,
        JapanNTSC,
        ExportNTSC,
        ExportPAL
    }

    public abstract class BaseUnit
    {
        public const BaseUnitRegion DefaultBaseUnitRegion = BaseUnitRegion.ExportNTSC;

        /* System state stuff */
        public bool IsStopped { get; private set; }
        public bool IsPaused { get; private set; }

        /* Region-related stuff */
        BaseUnitRegion region;

        bool isNtscSystem { get { return (region == BaseUnitRegion.JapanNTSC || region == BaseUnitRegion.ExportNTSC); } }
        bool isExportSystem { get { return (region == BaseUnitRegion.ExportNTSC || region == BaseUnitRegion.ExportPAL); } }

        public bool IsNtscSystem { get { return isNtscSystem; } }
        public bool IsPalSystem { get { return !isNtscSystem; } }
        public bool IsExportSystem { get { return isExportSystem; } }
        public bool IsJapaneseSystem { get { return !isExportSystem; } }

        /* FPS limiter/counter */
        Stopwatch stopWatch;
        long startTime;
        int frameCounter;
        public double FramesPerSecond { get; private set; }
        public bool LimitFPS { get; set; }

        /* Media-related stuff */
        public string MediaFilename { get; private set; }
        public string MediaSaveFilename { get; private set; }

        protected MediaType CurrentMediaType { get; private set; }
        protected BaseMedia CurrentMedia { get; private set; }

        public bool IsMediaInserted { get { return CurrentMedia != null; } }

        string savePath;

        /* Emulation I/O stuff */
        public event RenderScreenHandler RenderScreen;

        /* Constructor */
        public BaseUnit()
        {
            IsStopped = true;
            IsPaused = false;

            SetRegion(DefaultBaseUnitRegion);

            stopWatch = new Stopwatch();
            stopWatch.Start();
            startTime = 0;
            frameCounter = 0;
            FramesPerSecond = 0.0;
            LimitFPS = true;

            MediaFilename = MediaSaveFilename = string.Empty;

            CurrentMediaType = MediaType.None;
            CurrentMedia = null;

            savePath = string.Empty;
        }

        public void SetMediaPaths(string savePath)
        {
            this.savePath = savePath;
        }

        public virtual void SetRegion(BaseUnitRegion unitRegion)
        {
            region = unitRegion;
        }

        public BaseUnitRegion GetRegion()
        {
            return region;
        }

        public abstract double GetFrameRate();

        public void InsertMedia(MediaType mediaType, BaseMedia media)
        {
            CurrentMedia = media;
            CurrentMediaType = mediaType;

            MediaFilename = media.Filename;
            MediaSaveFilename = Path.Combine(savePath, Path.GetFileName(Path.ChangeExtension(MediaFilename, "sav")));
        }

        private void LoadMediaOnBoardRam()
        {
            if (!File.Exists(MediaSaveFilename)) return;

            using (FileStream file = new FileStream(MediaSaveFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] data = new byte[file.Length];
                file.Read(data, 0, data.Length);
                CurrentMedia?.SetRamData(data);
            }
        }

        private void SaveMediaOnBoardRam()
        {
            if (CurrentMedia == null) return;

            if (CurrentMedia.HasOnBoardRam())
            {
                byte[] cartRam = CurrentMedia.GetRamData();
                using (FileStream file = new FileStream(MediaSaveFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    file.Write(cartRam, 0, cartRam.Length);
                }
            }
        }

        public void PowerOn()
        {
            IsStopped = false;
            IsPaused = false;

            Reset();
            LoadMediaOnBoardRam();
        }

        public void PowerOff()
        {
            SaveMediaOnBoardRam();

            IsStopped = true;
            IsPaused = false;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Unpause()
        {
            IsPaused = false;
        }

        public void Execute()
        {
#if !DEBUG
            try
#endif
            {
                while (!IsStopped)
                {
                    startTime = stopWatch.ElapsedMilliseconds;
                    long interval = (long)TimeSpan.FromSeconds(1.0 / GetFrameRate()).TotalMilliseconds;

                    if (!IsPaused)
                        ExecuteFrame();

                    while (LimitFPS && stopWatch.ElapsedMilliseconds - startTime < interval)
                        Thread.Sleep(1);

                    frameCounter++;
                    double timeDifference = (stopWatch.ElapsedMilliseconds - startTime);
                    if (timeDifference >= 1.0)
                    {
                        FramesPerSecond = (frameCounter / (timeDifference / 1000));
                        frameCounter = 0;
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                string message = string.Format("Exception occured: {0}\n\nEmulation thread has been stopped.", ex.Message);
                MessageBox.Show(message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);

                IsStopped = true;
            }
#endif
        }

        protected void OnRenderScreen(RenderEventArgs e)
        {
            RenderScreen?.Invoke(this, e);
        }

        public abstract void Reset();
        public abstract void ExecuteFrame();

        public abstract byte ReadMemory(ushort address);
        public abstract void WriteMemory(ushort address, byte value);
        public abstract byte ReadPort(byte port);
        public abstract void WritePort(byte port, byte value);
    }
}
