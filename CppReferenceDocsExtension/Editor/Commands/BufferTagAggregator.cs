//using System;
//using System.Diagnostics;
//using System.Reflection;
//using System.Threading;
//using System.Threading.Tasks;
//using EnvDTE;
//using EnvDTE80;
//using Microsoft.VisualStudio.Text.Tagging;
//using Serilog;

//namespace CppReferenceDocsExtension.Editor.Commands
//{
//    //[VisualStudioContribution]
//    //public class Command1 : Command
//    //{
//    //    private TraceSource traceSource;
//    //    private AsyncServiceProviderInjection<DTE, DTE2> dte;
//    //    private MefInjection<IBufferTagAggregatorFactoryService> tagAggFactoryService;
//    //    private readonly ILogger log = Log.Logger;

//    //    public Command1(VisualStudioExtensibility extensibility, TraceSource traceSource,
//    //                    AsyncServiceProviderInjection<DTE, DTE2> dte,
//    //                    MefInjection<IBufferTagAggregatorFactoryService> tagAggFactoryService) : base(extensibility) {
//    //        this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
//    //        this.dte = dte;
//    //        this.traceSource = traceSource;
//    //        this.tagAggFactoryService = tagAggFactoryService;
//    //    }

//    //    public override CommandConfiguration CommandConfiguration =>
//    //        new(@"Sample Tagger Remote Command") {
//    //            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
//    //            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText)
//    //        };

//    //    public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken) {
//    //        this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
//    //        throw new NotImplementedException();
//    //    }
//    //}
//}


