using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Core;
using CppReferenceDocsExtension.Core.Package;

namespace CppReferenceDocsExtension.Editor.QuickInfo
{
    [ContentType(Constants.Content.CPlusPlus)]
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name(nameof(DocsIntellisenseControllerProvider))]
    public class DocsIntellisenseControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IAsyncQuickInfoBroker Broker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(
            ITextView textView, IList<ITextBuffer> subjectBuffers) {
            return new DocsIntellisenseController(textView, subjectBuffers, this);
        }
    }

    public class DocsIntellisenseController : IIntellisenseController
    {
        private ITextView textView;
        private readonly IList<ITextBuffer> buffers;
        private readonly DocsIntellisenseControllerProvider provider;
        private Task<IAsyncQuickInfoSession> session;

        public DocsIntellisenseController(
            ITextView view, IList<ITextBuffer> buffers, DocsIntellisenseControllerProvider provider) {
            this.session = null;
            this.textView = view;
            this.buffers = buffers;
            this.provider = provider;
            this.textView.MouseHover += this.OnMouseHover;
        }

        public void Detach(ITextView view) {
            if (view == this.textView) {
                this.textView.MouseHover -= this.OnMouseHover;
                this.textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) {
            // TODO: handle anything in here?
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) {
            // TODO: handle anything in here?
        }

        private void OnMouseHover(object sender, MouseHoverEventArgs e) {
            // find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = this.textView.BufferGraph.MapDownToFirstMatch(
                new(this.textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                snapshot => this.buffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor
            );

            if (point != null) {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(
                    point.Value.Position,
                    PointTrackingMode.Positive
                );

                if (!this.provider.Broker.IsQuickInfoActive(this.textView)) {
                    this.session = this.provider.Broker.TriggerQuickInfoAsync(
                        this.textView,
                        triggerPoint,
                        QuickInfoSessionOptions.TrackMouse
                    );
                }
            }
        }
    }
}
