using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

// ComVisible を false に設定すると、その型はこのアセンブリ内で COM コンポーネントから 
// 参照不可能になります。COM からこのアセンブリ内の型にアクセスする場合は、
// その型の ComVisible 属性を true に設定してください。
[assembly: ComVisible(false)]

// このプロジェクトが COM に公開される場合、次の GUID が typelib の ID になります
[assembly: Guid("4232cb4a-dfe3-46ca-9503-c5f1798baed3")]

// OpenCvSharp objects' debugger visualizer
[assembly: DebuggerVisualizer(
    typeof(OpenCvSharp.DebuggerVisualizers.MatDebuggerVisualizer),
    typeof(OpenCvSharp.DebuggerVisualizers.MatObjectSource),
    Target = typeof(OpenCvSharp.Mat),
    Description = "Mat Visualizer"
)]