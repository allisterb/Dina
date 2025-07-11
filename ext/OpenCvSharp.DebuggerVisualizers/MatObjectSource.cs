using System.IO;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.DebuggerVisualizers;
using OpenCvSharp;

namespace OpenCvSharp.DebuggerVisualizers
{
    /// <summary>
    /// シリアライズ処理
    /// </summary>
    public class MatObjectSource : VisualizerObjectSource
    {

        public override void GetData(object target, Stream outgoingData)
        {
            var fs = new FileStorage("json", FileStorage.Modes.Write | FileStorage.Modes.Memory);
            fs.Write("f", (Mat)target);
            VisualizerObjectSource.Serialize(outgoingData, fs.ReleaseAndGetString());
        }
            
        
    }
}
