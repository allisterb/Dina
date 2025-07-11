using System;

using Microsoft.VisualStudio.DebuggerVisualizers;

namespace OpenCvSharp.DebuggerVisualizers
{
    /// <summary>
    /// ビジュアライザでの視覚化処理
    /// </summary>
    public class MatDebuggerVisualizer : DialogDebuggerVisualizer
    {
        public MatDebuggerVisualizer(): base(FormatterPolicy.NewtonsoftJson) { }    
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            //var op = (IVisualizerObjectProvider3)objectProvider;    
            // MatProxyが送られてくるはず
            var op = objectProvider as IVisualizerObjectProvider3;  
            var f = op.GetObject<string>();
            var fs = new FileStorage(f, FileStorage.Modes.Read | FileStorage.Modes.Memory);
            Mat mat = fs["f"].ReadMat();    
            if (mat is null)
            {
                throw new ArgumentException();
            }

            // Formに表示
            using (var form = new ImageViewer(new MatProxy(mat)))
            {
                windowService.ShowDialog(form);
            }
        }
    }
}
