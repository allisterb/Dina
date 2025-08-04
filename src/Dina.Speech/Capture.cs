namespace Dina;

using System.ComponentModel;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using static Result;
public class Audio : Runtime
{
    const int sampleRate = 16_000;
    const int bufferSize = 1024;

    public static void Capture(CancellationToken ct, Action<Result<List<byte[]>>> processSamples)
    {
        BackgroundWorker worker = new BackgroundWorker();   
        worker.DoWork += (sender, e) =>
        {
            ALCaptureDevice captureDevice = ALC.CaptureOpenDevice(null, sampleRate, ALFormat.Mono16, bufferSize);
            if (captureDevice == default)
            {
                e.Result = Failure<List<byte[]>>("Failed to open default capture device.");
                return;
            }
            
            var samples = (List<byte[]>)e.Argument!;    
            ALC.CaptureStart(captureDevice);
            while (!ct.IsCancellationRequested)
            {
                byte[] buffer = new byte[bufferSize];
                var current = 0;
                while (current < buffer.Length && !ct.IsCancellationRequested)
                {
                    var samplesAvailable = ALC.GetInteger(captureDevice, AlcGetInteger.CaptureSamples);
                    if (samplesAvailable < 256) continue;
                    var samplesToRead = Math.Min(samplesAvailable, buffer.Length - current);
                    ALC.CaptureSamples(captureDevice, ref buffer[current], samplesToRead);
                    current += samplesToRead;
                }
                samples.Add(buffer);
                
            }
            ALC.CaptureStop(captureDevice);
            e.Result = Success(samples);
        };
        worker.RunWorkerCompleted += (sender, e) =>
        {
            if (e.Error != null)
            {
                processSamples(FailureError<List<byte[]>>("Capture failed.", e.Error));
                return;
            }
            else
                processSamples((Result<List<byte[]>>)e.Result!);
        };
        worker.RunWorkerAsync(new List<byte[]>());
        return; 
    }

   public static void WriteWav(byte[] data, string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(data.Length + 36);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // Subchunk1Size
                bw.Write((short)1); // AudioFormat
                bw.Write((short)1); // NumChannels
                bw.Write(sampleRate); // SampleRate
                bw.Write(sampleRate * 2); // ByteRate
                bw.Write((short)2); // BlockAlign
                bw.Write((short)16); // BitsPerSample
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(data.Length);
                bw.Write(data);
            }
        }
    }

    
}
