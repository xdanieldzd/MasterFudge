using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using SharpDX;
using SharpDX.DirectSound;
using SharpDX.Multimedia;

using MasterFudge.Emulation;

namespace MasterFudge
{
    public class SoundWrapper
    {
        DirectSound directSound;
        WaveFormat waveFormat;

        SecondarySoundBuffer soundBuffer;
        NotificationPosition[] notifications;

        Thread fillBufferThread;
        volatile bool threadShouldStop;

        public int BufferSize { get { return soundBuffer.Capabilities.BufferBytes; } }

        public SoundWrapper(Form form)
        {
            directSound = new DirectSound();
            directSound.SetCooperativeLevel(form.Handle, CooperativeLevel.Priority);

            waveFormat = new WaveFormat(44100, 2);

            var soundBufferDesc = new SoundBufferDescription();
            soundBufferDesc.BufferBytes = (2048 * 2);
            soundBufferDesc.Format = waveFormat;
            soundBufferDesc.Flags = BufferFlags.GetCurrentPosition2 | BufferFlags.ControlPositionNotify | BufferFlags.GlobalFocus | BufferFlags.ControlVolume | BufferFlags.StickyFocus;
            soundBufferDesc.AlgorithmFor3D = Guid.Empty;

            soundBuffer = new SecondarySoundBuffer(directSound, soundBufferDesc);

            notifications = new NotificationPosition[2];
            notifications[0] = new NotificationPosition();
            notifications[0].Offset = ((soundBufferDesc.BufferBytes / 2) + 1);
            notifications[0].WaitHandle = new AutoResetEvent(false);
            notifications[1] = new NotificationPosition();
            notifications[1].Offset = (soundBufferDesc.BufferBytes - 1);
            notifications[1].WaitHandle = new AutoResetEvent(false);
            soundBuffer.SetNotificationPositions(notifications);
        }

        public void StartPlayback(Stream stream)
        {
            byte[] bytes1 = new byte[soundBuffer.Capabilities.BufferBytes / 2];

            StopThread();

            fillBufferThread = new Thread(() =>
            {
                int bytesRead = -1;

                soundBuffer.Play(0, PlayFlags.Looping);
                while (!threadShouldStop)
                {
                    if (bytesRead == 0) break;
                    bytesRead = stream.Read(bytes1, 0, bytes1.Length);
                    soundBuffer.Write(bytes1, 0, LockFlags.None);
                    notifications[0].WaitHandle.WaitOne();

                    if (bytesRead == 0) break;
                    bytesRead = stream.Read(bytes1, 0, bytes1.Length);
                    soundBuffer.Write(bytes1, soundBuffer.Capabilities.BufferBytes / 2, LockFlags.None);
                    notifications[1].WaitHandle.WaitOne();

                    stream.Seek(0, SeekOrigin.Begin);
                }

                stream.Close();
                stream.Dispose();
            });
            fillBufferThread.Start();
        }

        public void StopThread()
        {
            if (fillBufferThread != null)
            {
                while (fillBufferThread.IsAlive)
                    threadShouldStop = true;
            }
        }
    }
}
