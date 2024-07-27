//using Microsoft.VisualStudio.Extensibility;
//using Microsoft.VisualStudio.Extensibility.DebuggerVisualizers;
//using Microsoft.VisualStudio.RpcContracts.RemoteUI;

//namespace CppReferenceDocsExtensibility
//{
//    public class DebugVisualizer { }

//    [VisualStudioContribution]
//    internal class CodeElementVisualizerProvider : DebuggerVisualizerProvider
//    {
//        public CodeElementVisualizerProvider(
//            OOPExtensibilityExtension extension, VisualStudioExtensibility extensibility)
//            : base(extension, extensibility)
//        { }

//        public override DebuggerVisualizerProviderConfiguration DebuggerVisualizerProviderConfiguration() {
//            string display = "";

//            return new(display, typeof(string));

//        }

//        public override DebuggerVisualizerProviderConfiguration DebuggerVisualizerProviderConfiguration => new DebuggerVisualizerProviderConfiguration(
//            new VisualizerTargetType("DataSet Visualizer", typeof(System.Data.DataSet)),
//            new VisualizerTargetType("DataTable Visualizer", typeof(System.Data.DataTable)),
//            new VisualizerTargetType("DataView Visualizer", typeof(System.Data.DataView)),
//            new VisualizerTargetType("DataViewManager Visualizer", typeof(System.Data.DataViewManager)));

//        public override async Task<IRemoteUserControl> CreateVisualizerAsync(VisualizerTarget visualizerTarget, CancellationToken cancellationToken) {
//            ...
//        }
//    }
//}


