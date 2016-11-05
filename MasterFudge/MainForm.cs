using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Linq;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using NAudio.Wave;

using MasterFudge.Emulation;
using MasterFudge.Emulation.Cartridges;
using MasterFudge.Emulation.Graphics;
using MasterFudge.Emulation.IO;
using MasterFudge.Controls;

namespace MasterFudge
{
    public partial class MainForm : Form
    {
        static readonly string saveDirectory = "Saves";
        static readonly string screenshotDirectory = "Screenshots";

        static readonly string vertexShader =
            "#version 330\n" +
            "\n" +
            "layout(location = 0) in vec2 in_position;\n" +
            "layout(location = 1) in vec2 in_texCoord;\n" +
            "\n" +
            "out vec3 frag_position;\n" +
            "out vec2 frag_texCoord;\n" +
            "\n" +
            "void main(void)\n" +
            "{\n" +
            "    frag_position = vec3(in_position, 0.0);\n" +
            "    frag_texCoord = in_texCoord;\n" +
            "    gl_Position = vec4(in_position, 0.0, 1.0);\n" +
            "}\n";

        static readonly string fragmentShader =
            "#version 330\n" +
            "\n" +
            "precision highp float;\n" +
            "\n" +
            "uniform sampler2D material_texture;\n" +
            "uniform bool enable_noise;\n" +
            "uniform int time;\n" +
            "\n" +
            "in vec3 frag_position;\n" +
            "in vec2 frag_texCoord;\n" +
            "\n" +
            "out vec4 out_finalColor;\n" +
            "\n" +
            "float random(vec2 co)\n" +
            "{\n" +
            "    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);\n" +
            "}\n" +
            "\n" +
            "void main(void)\n" +
            "{\n" +
            "    if (enable_noise)\n" +
            "    {\n" +
            "        float value = random(vec2(frag_position.x * cos(time), frag_position.y * sin(time)));\n" +
            "        out_finalColor = vec4(value, value, value, 1.0);\n" +
            "    }\n" +
            "    else\n" +
            "        out_finalColor = texture2D(material_texture, frag_texCoord);\n" +
            "}\n";

        static readonly Utils.OpenGL.Vertex2D[] vertices = new Utils.OpenGL.Vertex2D[]
        {
            new Utils.OpenGL.Vertex2D() { Position = new Vector2(-1.0f, -1.0f), TexCoord = new Vector2(0.0f, 1.0f) },
            new Utils.OpenGL.Vertex2D() { Position = new Vector2( 1.0f, -1.0f), TexCoord = new Vector2(1.0f, 1.0f) },
            new Utils.OpenGL.Vertex2D() { Position = new Vector2( 1.0f,  1.0f), TexCoord = new Vector2(1.0f, 0.0f) },
            new Utils.OpenGL.Vertex2D() { Position = new Vector2(-1.0f,  1.0f), TexCoord = new Vector2(0.0f, 0.0f) },
        };

        // TODO: key mapping should be finished for now; eventually make user-configurable?

        static readonly Dictionary<Keys, KeyboardKeys> scKeyboardMapping = new Dictionary<Keys, KeyboardKeys>()
        {
            { Keys.D1, KeyboardKeys.D1 }, { Keys.D2, KeyboardKeys.D2 }, { Keys.D3, KeyboardKeys.D3 }, { Keys.D4, KeyboardKeys.D4 },
            { Keys.D5, KeyboardKeys.D5 }, { Keys.D6, KeyboardKeys.D6 }, { Keys.D7, KeyboardKeys.D7 }, { Keys.D8, KeyboardKeys.D8 },
            { Keys.D9, KeyboardKeys.D9 }, { Keys.D0, KeyboardKeys.D0 },

            { Keys.A, KeyboardKeys.A }, { Keys.B, KeyboardKeys.B }, { Keys.C, KeyboardKeys.C }, { Keys.D, KeyboardKeys.D },
            { Keys.E, KeyboardKeys.E }, { Keys.F, KeyboardKeys.F }, { Keys.G, KeyboardKeys.G }, { Keys.H, KeyboardKeys.H },
            { Keys.I, KeyboardKeys.I }, { Keys.J, KeyboardKeys.J }, { Keys.K, KeyboardKeys.K }, { Keys.L, KeyboardKeys.L },
            { Keys.M, KeyboardKeys.M }, { Keys.N, KeyboardKeys.N }, { Keys.O, KeyboardKeys.O }, { Keys.P, KeyboardKeys.P },
            { Keys.Q, KeyboardKeys.Q }, { Keys.R, KeyboardKeys.R }, { Keys.S, KeyboardKeys.S }, { Keys.T, KeyboardKeys.T },
            { Keys.U, KeyboardKeys.U }, { Keys.V, KeyboardKeys.V }, { Keys.W, KeyboardKeys.W }, { Keys.X, KeyboardKeys.X },
            { Keys.Y, KeyboardKeys.Y }, { Keys.Z, KeyboardKeys.Z },

            { Keys.Left, KeyboardKeys.Left }, { Keys.Right, KeyboardKeys.Right }, { Keys.Up, KeyboardKeys.Up }, { Keys.Down, KeyboardKeys.Down },

            { Keys.F1, KeyboardKeys.Func }, { Keys.ControlKey, KeyboardKeys.Ctrl }, { Keys.ShiftKey, KeyboardKeys.Shift }, { Keys.Tab, KeyboardKeys.Graph }, { Keys.F2, KeyboardKeys.EngDiers },
            { Keys.Space, KeyboardKeys.Space }, { Keys.Enter, KeyboardKeys.CR }, { Keys.Home, KeyboardKeys.HomeClr }, { Keys.Back, KeyboardKeys.InsDel },

            { Keys.OemMinus, KeyboardKeys.Minus }, { Keys.Oem6, KeyboardKeys.Caret }, { Keys.Oem4, KeyboardKeys.Yen }, { Keys.F12, KeyboardKeys.Break },
            { Keys.Oemcomma, KeyboardKeys.Comma }, { Keys.OemPeriod, KeyboardKeys.Period }, { Keys.Oem102, KeyboardKeys.Slash }, { Keys.Oem5, KeyboardKeys.Pi },
            { Keys.OemSemicolon, KeyboardKeys.At }, { Keys.Oemplus, KeyboardKeys.BracketOpen }, { Keys.Oem2, KeyboardKeys.BracketClose }, { Keys.Oem3, KeyboardKeys.Semicolon }, { Keys.Oem7, KeyboardKeys.Colon },
        };

        BaseUnit emulator;
        TaskWrapper taskWrapper;
        WaveOut waveOut;

        Version programVersion;

        int vaoId, vboId, textureId, pboId, vShaderId, fShaderId, shaderProgId;
        byte[] pixelData;

        bool logEnabled;
        TextWriter logWriter;

        const float defaultVolume = 0.5f;
        const int maxRecentFiles = 12;

        public bool soundEnabled
        {
            get { return (waveOut?.Volume == defaultVolume); }
            set { waveOut.Volume = (value ? defaultVolume : 0.0f); }
        }

        public MainForm()
        {
            InitializeComponent();

            /* Create emulator instance & task wrapper */
            emulator = new BaseUnit();
            emulator.OnRenderScreen += Emulator_OnRenderScreen;
            emulator.SetRegion(Configuration.BaseUnitRegion);
            emulator.LimitFPS = Configuration.LimitFPS;
            taskWrapper = new TaskWrapper();
            taskWrapper.Start(emulator);

            /* Create output instances */
            waveOut = new WaveOut();
            waveOut.Init(emulator.GetPSGWaveProvider());
            waveOut.Play();
            soundEnabled = Configuration.SoundEnabled;

            /* Misc variables */
            programVersion = new Version(Application.ProductVersion);

            /* Misc UI stuff */
            nTSCToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsNtscSystem");
            pALToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsPalSystem");
            exportToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsExportSystem");
            japaneseToolStripMenuItem.DataBindings.Add("Checked", emulator, "IsJapaneseSystem");
            limitFPSToolStripMenuItem.DataBindings.Add("Checked", emulator, "LimitFPS");
            enableSoundToolStripMenuItem.DataBindings.Add("Checked", this, "soundEnabled");

            /* Default settings stuff */
            if (Configuration.RecentFiles == null)
            {
                Configuration.RecentFiles = new string[maxRecentFiles];
                for (int i = 0; i < Configuration.RecentFiles.Length; i++) Configuration.RecentFiles[i] = string.Empty;
            }

            if (Configuration.LastCartridgePath != string.Empty)
                ofdOpenCartridge.InitialDirectory = Configuration.LastCartridgePath;

            CleanUpRecentList();
            UpdateRecentFilesMenu();

            SetFormTitle();
            tsslStatus.Text = "Ready";

            /* Logging stuff */
            logEnabled = false;
            logWriter = null;

            Program.Log.OnLogUpdate += new Logger.LogUpdateHandler((s, ev) =>
            {
                if (logWriter != null && !Disposing)
                {
                    if (InvokeRequired)
                        Invoke(new Action(() => { logWriter.WriteLine(ev.Message); }));
                    else
                        logWriter.WriteLine(ev.Message);
                }
            });
            Program.Log.OnLogCleared += new EventHandler((s, ev) => { logWriter?.Flush(); });

            /* Autostart ROM when debugging thingy */
            //DebugLoadRomShim();
        }

        private void SetFormTitle()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} v{1}.{2}", Application.ProductName, programVersion.Major, programVersion.Minor);
            if (programVersion.Build != 0) builder.AppendFormat(".{0}", programVersion.Build);

            if (!emulator.IsStopped)
            {
                if (emulator.CartridgeLoaded)
                    builder.AppendFormat(" - [{0}]", Path.GetFileName(emulator.CartridgeFilename));
                else
                    builder.Append(" - [No Cartridge]");

                if (emulator.IsPaused)
                    builder.Append(" - (Paused)");
            }

            Text = builder.ToString();
        }

        private void UpdateRecentFilesMenu()
        {
            /* Recent files menu */
            var oldRecentItems = recentFilesToolStripMenuItem.DropDownItems.Cast<ToolStripItem>().Where(x => x is ToolStripMenuItem && x.Tag is int).ToList();
            foreach (ToolStripItem item in oldRecentItems)
                recentFilesToolStripMenuItem.DropDownItems.Remove(item);

            for (int i = 0; i < Configuration.RecentFiles.Length; i++)
            {
                string recentFile = Configuration.RecentFiles[i];

                if (recentFile == string.Empty)
                {
                    ToolStripMenuItem menuItem = new ToolStripMenuItem("-");
                    menuItem.ShortcutKeys = Keys.Control | (Keys.F1 + i);
                    menuItem.Enabled = false;
                    menuItem.Tag = -1;
                    recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
                }
                else
                {
                    ToolStripMenuItem menuItem = new ToolStripMenuItem(Path.GetFileName(recentFile));
                    menuItem.ShortcutKeys = Keys.Control | (Keys.F1 + i);
                    menuItem.Tag = i;
                    menuItem.Click += ((s, ev) =>
                    {
                        int fileNumber = (int)(s as ToolStripMenuItem).Tag;
                        string filePath = Configuration.RecentFiles[fileNumber];
                        if (!File.Exists(filePath))
                        {
                            MessageBox.Show("Selected file does not exist anymore; it will be removed from the list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Configuration.RecentFiles[fileNumber] = string.Empty;
                            CleanUpRecentList();
                            UpdateRecentFilesMenu();
                        }
                        else
                            LoadCartridge(filePath);
                    });
                    recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
                }
            }
        }

        private void ResizeWindowByOutput()
        {
            ClientSize = new Size(renderControl.Width * 2, (renderControl.Height * 2) + menuStrip.Height);
        }

        private string GetSaveFilePath(string cartFile)
        {
            return Path.Combine(Configuration.UserDataPath, saveDirectory, Path.GetFileName(Path.ChangeExtension(cartFile, "sav")));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            emulator.OnRenderScreen -= Emulator_OnRenderScreen;

            PowerOffEmulation();

            taskWrapper.Stop();

            logWriter?.Close();

            // TODO: make menu options for dumps, I guess
            if (emulator != null)
            {
                try
                {
                    File.WriteAllBytes(@"E:\temp\sms\wram.bin", BaseUnit.Debugging.DumpMemory(emulator, BaseUnit.Debugging.DumpRegion.WorkRam));
                    File.WriteAllBytes(@"E:\temp\sms\vram.sms", BaseUnit.Debugging.DumpMemory(emulator, BaseUnit.Debugging.DumpRegion.VideoRam));
                    File.WriteAllBytes(@"E:\temp\sms\cram.bin", BaseUnit.Debugging.DumpMemory(emulator, BaseUnit.Debugging.DumpRegion.ColorRam));
                }
                catch (IOException) { /* just ignore this one, happens if I have any of these open in ex. a hexeditor */ }
            }

            if (GL.IsVertexArray(vaoId))
                GL.DeleteVertexArray(vaoId);

            if (GL.IsBuffer(vboId))
                GL.DeleteBuffer(vboId);

            if (GL.IsTexture(textureId))
                GL.DeleteTexture(textureId);

            if (GL.IsBuffer(pboId))
                GL.DeleteBuffer(pboId);

            if (GL.IsProgram(shaderProgId))
            {
                if (GL.IsShader(vShaderId))
                {
                    GL.DetachShader(shaderProgId, vShaderId);
                    GL.DeleteShader(vShaderId);
                }

                if (GL.IsShader(fShaderId))
                {
                    GL.DetachShader(shaderProgId, fShaderId);
                    GL.DeleteShader(fShaderId);
                }

                GL.DeleteProgram(shaderProgId);
            }
        }

        private void SetRegion(bool isNtsc, bool isExport)
        {
            BaseUnitRegion regionToSet;
            if (isExport)
            {
                if (isNtsc)
                    regionToSet = BaseUnitRegion.ExportNTSC;
                else
                    regionToSet = BaseUnitRegion.ExportPAL;
            }
            else
                regionToSet = BaseUnitRegion.JapanNTSC;

            emulator.SetRegion(regionToSet);
            Configuration.BaseUnitRegion = regionToSet;
        }

        private void PowerOnEmulation()
        {
            Program.Log.WriteEvent("--- STARTING EMULATION ---");

            emulator.PowerOn();
        }

        private void PowerOffEmulation()
        {
            emulator.PowerOff();
            if (emulator.CartridgeLoaded)
                emulator.SaveCartridgeRam(GetSaveFilePath(emulator.CartridgeFilename));
            Program.Log.ClearEvents();

            tsslFps.Text = string.Empty;
        }

        private void LoadCartridge(string filename)
        {
            PowerOffEmulation();

            emulator.LoadCartridge(filename);
            emulator.LoadCartridgeRam(GetSaveFilePath(filename));
            LogCartridgeInformation(emulator, filename);

            SetFormTitle();
            tsslStatus.Text = string.Format("Cartridge '{0}' loaded", Path.GetFileName(filename));
            cartridgeInformationToolStripMenuItem.Enabled = true;
            AddFileToRecentList(filename);
            UpdateRecentFilesMenu();

            Configuration.LastCartridgePath = Path.GetDirectoryName(filename);

            PowerOnEmulation();
        }

        private void CleanUpRecentList()
        {
            List<string> files = Configuration.RecentFiles.Where(x => x != string.Empty).ToList();
            while (files.Count < maxRecentFiles) files.Add(string.Empty);
            Configuration.RecentFiles = files.Take(maxRecentFiles).ToArray();
        }

        private void AddFileToRecentList(string filename)
        {
            List<string> files = Configuration.RecentFiles.Where(x => x != string.Empty).ToList();
            files.Reverse();

            /* Remove if already exists, so that adding it will make it the most recent entry */
            if (files.Contains(filename)) files.Remove(filename);

            files.Add(filename);
            files.Reverse();

            /* Pad with dummy values */
            while (files.Count < maxRecentFiles) files.Add(string.Empty);

            Configuration.RecentFiles = files.Take(maxRecentFiles).ToArray();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugLoadRomShim()
        {
            if (Environment.MachineName != "NANAMI-X") return;

            //Configuration.MasterSystemBootstrapPath = @"D:\ROMs\SMS\[BIOS] Sega Master System (USA, Europe) (v1.3).sms";
            //Configuration.GameGearBootstrapPath = @"D:\ROMs\GG\majbios.gg";

            string romFile = @"D:\ROMs\SMS\Hang-On_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Sonic_the_Hedgehog_(UE)_[!].sms";
            romFile = @"D:\ROMs\SMS\Y's_-_The_Vanished_Omen_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\VDPTEST.sms";
            //romFile = @"D:\ROMs\SMS\[BIOS] Sega Master System (USA, Europe) (v1.3).sms";
            //romFile = @"D:\ROMs\SMS\Teddy_Boy_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\R-Type_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Alex_Kidd_in_Miracle_World_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\Psycho_Fox_(UE)_[!].sms";
            //romFile = @"D:\ROMs\SMS\SMS Sound Test 1.1.sms";
            //romFile = @"D:\ROMs\SMS\F16_Fighting_Falcon_(UE)_[!].sms";

            //romFile = @"D:\ROMs\GG\Sonic_the_Hedgehog_(JUE).gg";
            //romFile = @"D:\ROMs\GG\Gunstar_Heroes_(J).gg";

            //romFile = @"D:\ROMs\SMS\Girl's_Garden_(SC-3000).sg";
            //romFile = @"D:\ROMs\SMS\Sega_BASIC_Level_2_(SC-3000).sc";
            //romFile = @"D:\ROMs\SMS\Sega_BASIC_Level_3_V1_(SC-3000).sc";

            //romFile = @"D:\ROMs\SMS\Cosmic Spacehead (Europe) (En,Fr,De,Es).sms";
            //romFile = @"D:\ROMs\SMS\Fantastic Dizzy (Europe) (En,Fr,De,Es,It).sms";
            //romFile = @"D:\ROMs\SMS\Micro Machines (Europe).sms";

            LoadCartridge(romFile);

            Debugging.DisassemblyForm disasm = new Debugging.DisassemblyForm(emulator);
            disasm.Show();
        }

        private void LogCartridgeInformation(BaseUnit ms, string romFile)
        {
            Program.Log.WriteEvent("--- ROM INFORMATION ---");

            foreach (string line in GetCartridgeInformation())
                Program.Log.WriteEvent(line);
        }

        private string[] GetCartridgeInformation()
        {
            RomHeader header = emulator.GetCartridgeHeader();

            List<string> lines = new List<string>();
            lines.Add(string.Format("Filename: {0}", Path.GetFileName(emulator.CartridgeFilename)));
            lines.Add(string.Format("TMR SEGA string: '{0}'", header.TMRSEGAString));
            lines.Add(string.Format("Reserved: [0x{0:X2}, 0x{1:X2}]", header.Reserved[0], header.Reserved[1]));
            lines.Add(string.Format("Checksum: 0x{0:X4} (calculated 0x{1:X4}, {2})", header.Checksum, header.ChecksumCalculated, (header.Checksum == header.ChecksumCalculated ? "matches header" : "mismatch")));
            lines.Add(string.Format("Product code: {0}", header.ProductCode));
            lines.Add(string.Format("Version: {0}", header.Version));
            lines.Add(string.Format("Region: {0}", header.GetRegionName()));
            lines.Add(string.Format("ROM size: {0} (file is {1} KB, {2})", header.GetRomSizeName(), (header.RomSizeCalculated / 1024), (header.IsRomSizeCorrect ? "matches header" : "mismatch")));
            return lines.ToArray();
        }

        private void Emulator_OnRenderScreen(object sender, RenderEventArgs e)
        {
            try
            {
                if (IsHandleCreated && !Disposing)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action<RenderEventArgs>((ev) =>
                        {
                            RenderScreen(ev.FrameData);
                            tsslFps.Text = string.Format("{0:##} FPS", emulator.FramesPerSecond);
                        }), e);
                    }
                    else
                    {
                        RenderScreen(e.FrameData);
                    }
                }
            }
            catch (ObjectDisposedException) { /* meh, maybe fix later */ }
        }

        private void RenderScreen(byte[] frameData)
        {
            Buffer.BlockCopy(frameData, 0, pixelData, 0, frameData.Length);
        }

        private Buttons CheckJoypadInput(Keys key)
        {
            if (key == Configuration.KeyP1Up) return Buttons.Up;
            else if (key == Configuration.KeyP1Down) return Buttons.Down;
            else if (key == Configuration.KeyP1Left) return Buttons.Left;
            else if (key == Configuration.KeyP1Right) return Buttons.Right;
            else if (key == Configuration.KeyP1Button1) return Buttons.Button1;
            else if (key == Configuration.KeyP1Button2) return Buttons.Button2;
            else if (key == Configuration.KeyP1StartPause) return Buttons.StartPause;
            else if (key == Configuration.KeyReset) return Buttons.Reset;
            else return 0;
        }

        private KeyboardKeys CheckKeyboardInput(Keys key)
        {
            if (scKeyboardMapping.ContainsKey(key)) return scKeyboardMapping[key];
            else return KeyboardKeys.None;
        }

        private void CompileShader(ShaderType shaderType, string shaderString, out int handle)
        {
            handle = GL.CreateShader(shaderType);
            GL.ShaderSource(handle, shaderString);
            GL.CompileShader(handle);

            int statusCode;
            string infoLog;
            GL.GetShaderInfoLog(handle, out infoLog);
            GL.GetShader(handle, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new Exception(infoLog);
        }

        private void renderControl_Load(object sender, EventArgs e)
        {
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);

            /* VAO/VBO */
            vaoId = GL.GenVertexArray();
            GL.BindVertexArray(vaoId);
            vboId = Utils.OpenGL.GenerateVertexBuffer(vertices);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            /* Texture */
            pixelData = new byte[(VDP.OutputFramebufferWidth * VDP.OutputFramebufferHeight) * VDP.OutputFramebufferNumChannels];

            textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, VDP.OutputFramebufferWidth, VDP.OutputFramebufferHeight, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pixelData);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            /* PBO */
            pboId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboId);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, pixelData.Length, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

            /* Shader */
            CompileShader(ShaderType.VertexShader, vertexShader, out vShaderId);
            CompileShader(ShaderType.FragmentShader, fragmentShader, out fShaderId);

            shaderProgId = GL.CreateProgram();
            GL.AttachShader(shaderProgId, vShaderId);
            GL.AttachShader(shaderProgId, fShaderId);
            GL.LinkProgram(shaderProgId);
            GL.UseProgram(0);
        }

        private void renderControl_Render(object sender, EventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            if (true)
            {
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboId);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, VDP.OutputFramebufferWidth, VDP.OutputFramebufferHeight, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboId);
                GL.BufferData(BufferTarget.PixelUnpackBuffer, pixelData.Length, IntPtr.Zero, BufferUsageHint.StreamDraw);
                IntPtr ptr = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
                if (ptr != IntPtr.Zero)
                {
                    Marshal.Copy(pixelData, 0, ptr, pixelData.Length);
                    GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
                }
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, VDP.OutputFramebufferWidth, VDP.OutputFramebufferHeight, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pixelData);
            }

            GL.UseProgram(shaderProgId);
            GL.Uniform1(GL.GetUniformLocation(shaderProgId, "material_texture"), 0);
            GL.Uniform1(GL.GetUniformLocation(shaderProgId, "enable_noise"), emulator.CartridgeLoaded ? 0 : 1);
            GL.Uniform1(GL.GetUniformLocation(shaderProgId, "time"), DateTime.UtcNow.Millisecond);

            GL.BindVertexArray(vaoId);
            GL.DrawArrays(PrimitiveType.Quads, 0, vertices.Length);
            GL.BindVertexArray(0);

            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void renderControl_Resize(object sender, EventArgs e)
        {
            RenderControl renderControl = (sender as RenderControl);

            GL.Viewport(0, 0, renderControl.Width, renderControl.Height);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (emulator?.GetUnitType() == BaseUnitType.SC3000)
                emulator?.SetKeyboardData(CheckKeyboardInput(e.KeyCode), true);
            else
                emulator?.SetButtonData(CheckJoypadInput(e.KeyCode), 0, true);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (emulator?.GetUnitType() == BaseUnitType.SC3000)
                emulator?.SetKeyboardData(CheckKeyboardInput(e.KeyCode), false);
            else
                emulator?.SetButtonData(CheckJoypadInput(e.KeyCode), 0, false);
        }

        #region Menu Event Handlers

        private void openCartridgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ofdOpenCartridge.InitialDirectory = Path.GetDirectoryName(emulator.CartridgeFilename);
            ofdOpenCartridge.FileName = Path.GetFileName(emulator.CartridgeFilename);

            if (ofdOpenCartridge.ShowDialog() == DialogResult.OK)
            {
                LoadCartridge(ofdOpenCartridge.FileName);
            }
        }

        private void cartridgeInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string line in GetCartridgeInformation()) builder.AppendLine(line);

            MessageBox.Show(builder.ToString(), "Cartridge Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void screenshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string directory = Path.Combine(Configuration.UserDataPath, screenshotDirectory);
            string fileName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} ({1}).png", Path.GetFileNameWithoutExtension(emulator.CartridgeFilename), DateTime.Now);
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "-"));

            string filePath = Path.Combine(Configuration.UserDataPath, screenshotDirectory, fileName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // TODO: fixme
            //screenBitmap?.Save(filePath);

            tsslStatus.Text = string.Format("Saved screenshot '{0}'", fileName);
        }

        private void clearListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Configuration.RecentFiles.Length; i++)
                Configuration.RecentFiles[i] = string.Empty;
            UpdateRecentFilesMenu();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            taskWrapper.Stop();
            Application.Exit();
        }

        private void powerOnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PowerOnEmulation();

            SetFormTitle();
        }

        private void powerOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PowerOffEmulation();
            SetFormTitle();
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.TogglePause();
            (sender as ToolStripMenuItem).Checked = emulator.IsPaused;
            SetFormTitle();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.Reset();
        }

        private void limitFPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configuration.LimitFPS = emulator.LimitFPS = (sender as ToolStripMenuItem).Checked;
        }

        private void enableSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configuration.SoundEnabled = soundEnabled = (sender as ToolStripMenuItem).Checked;
        }

        private void nTSCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion((sender as ToolStripMenuItem).Checked, emulator.IsExportSystem);
        }

        private void pALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion(!(sender as ToolStripMenuItem).Checked, emulator.IsExportSystem);
        }

        private void japaneseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion(emulator.IsNtscSystem, !(sender as ToolStripMenuItem).Checked);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRegion(emulator.IsNtscSystem, (sender as ToolStripMenuItem).Checked);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsFormData optionsData = new OptionsFormData()
            {
                UseBootstrap = Configuration.BootstrapEnabled,
                MasterSystemBootstrapPath = Configuration.MasterSystemBootstrapPath,
                GameGearBootstrapPath = Configuration.GameGearBootstrapPath,
                Player1Buttons = new Dictionary<Buttons, Keys>()
                {
                    { Buttons.Up, Configuration.KeyP1Up },
                    { Buttons.Down, Configuration.KeyP1Down },
                    { Buttons.Left, Configuration.KeyP1Left },
                    { Buttons.Right, Configuration.KeyP1Right },
                    { Buttons.Button1, Configuration.KeyP1Button1 },
                    { Buttons.Button2, Configuration.KeyP1Button2 },
                    { Buttons.StartPause, Configuration.KeyP1StartPause },
                    { Buttons.Reset, Configuration.KeyReset }
                },
                UseNoiseEffect = Configuration.NoiseEffectEnabled,
            };

            using (OptionsForm optionsForm = new OptionsForm(optionsData))
            {
                if (optionsForm.ShowDialog() == DialogResult.OK)
                {
                    Configuration.BootstrapEnabled = optionsForm.OptionsData.UseBootstrap;
                    Configuration.MasterSystemBootstrapPath = optionsForm.OptionsData.MasterSystemBootstrapPath;
                    Configuration.GameGearBootstrapPath = optionsForm.OptionsData.GameGearBootstrapPath;
                    Configuration.KeyP1Up = optionsForm.OptionsData.Player1Buttons[Buttons.Up];
                    Configuration.KeyP1Down = optionsForm.OptionsData.Player1Buttons[Buttons.Down];
                    Configuration.KeyP1Left = optionsForm.OptionsData.Player1Buttons[Buttons.Left];
                    Configuration.KeyP1Right = optionsForm.OptionsData.Player1Buttons[Buttons.Right];
                    Configuration.KeyP1Button1 = optionsForm.OptionsData.Player1Buttons[Buttons.Button1];
                    Configuration.KeyP1Button2 = optionsForm.OptionsData.Player1Buttons[Buttons.Button2];
                    Configuration.KeyP1StartPause = optionsForm.OptionsData.Player1Buttons[Buttons.StartPause];
                    Configuration.KeyReset = optionsForm.OptionsData.Player1Buttons[Buttons.Reset];
                    Configuration.NoiseEffectEnabled = optionsForm.OptionsData.UseNoiseEffect;
                }
            }
        }

        private void enableLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logEnabled = (sender as ToolStripMenuItem).Checked;

            if (logEnabled && logWriter == null)
            {
                // TODO: path selection? or just dump into executable or ROM folder?
                logWriter = new StreamWriter(@"E:\temp\sms\log.txt", false);
            }
        }

        private void logOpcodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulator.DebugLogOpcodes = (sender as ToolStripMenuItem).Checked;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();

            Version programVersion = new Version(Application.ProductVersion);
            builder.AppendFormat("{0} v{1}.{2}", Application.ProductName, programVersion.Major, programVersion.Minor);
            if (programVersion.Build != 0) builder.AppendFormat(".{0}", programVersion.Build);
            builder.Append(" - ");
            builder.AppendLine((Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute).Description);
            builder.AppendLine();
            builder.AppendLine((Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute).Copyright);

            MessageBox.Show(builder.ToString(), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
