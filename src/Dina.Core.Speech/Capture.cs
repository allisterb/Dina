namespace Dina;

using System.ComponentModel;

using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

public class AudioDevice
{
    public static void Capture()
    {
        BackgroundWorker worker = new BackgroundWorker();   
        worker.DoWork += (sender, e) =>
        {
            ALCaptureDevice captureDevice = ALC.CaptureOpenDevice(null, 44100, ALFormat.Mono16, 1024);
        };
        worker
       

    }   
}
