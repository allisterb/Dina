using KokoroSharp;
using KokoroSharp.Core;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dina.Speech
{
    public class TTS : Runtime
    {
        public static void StartTTSWorker(int interval = 100, CancellationToken? cancellationToken = null)
        {
            var options = new SessionOptions()
            {
                ExecutionMode = ExecutionMode.ORT_PARALLEL,

            };
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var token = cancellationToken ?? CancellationToken.None;
                while (!token.IsCancellationRequested)
                {
                    if (!TTSIsPlayingBack && TTSQueue.TryDequeue(out var text))
                    {
                        try
                        {
                            TTSIsPlayingBack = true;
                            var h = tts.SpeakFast(text, voice);
                            h.OnSpeechCompleted = (_) => TTSIsPlayingBack = false;
                        }
                        catch (Exception ex)
                        {
                            Error("TTS playback failed: {0}", ex.Message);
                        }
                    }
                    else
                    {
                        Thread.Sleep(interval); // Avoid busy-waiting
                    }
                }
            };
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();
        }

        public static CancellationTokenSource cts = new CancellationTokenSource();

        public static CancellationToken ct = cts.Token;

        public static void EnqueueTTS(string text) => TTSQueue.Enqueue(text);  

        public static ConcurrentQueue<string> TTSQueue = new ConcurrentQueue<string>();

        static KokoroTTS tts = KokoroTTS.LoadModel(KModel.int8);
        
        static KokoroVoice voice = KokoroVoiceManager.GetVoice("af_heart");

        static bool TTSIsPlayingBack = false;
    }
}
