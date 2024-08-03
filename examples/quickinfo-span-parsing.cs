using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickInfoTest
{
    public class QuickToolTip : IAsyncQuickInfoSource
    {
        private QuickToolTipProvider toolTipProvider;
        private ITextBuffer textBuffer;

        public QuickToolTip(QuickToolTipProvider provider, ITextBuffer textBuffer)
        {
            this.toolTipProvider = provider;
            this.textBuffer = textBuffer;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            Task<QuickInfoItem> t = new Task<QuickInfoItem>(() =>
            {
                SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(textBuffer.CurrentSnapshot);
                if (!subjectTriggerPoint.HasValue)
                {
                    //no subject, return null
                    return new QuickInfoItem(null, "");
                }

                ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
                SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

                //look for occurrences of our QuickInfo words in the span
                ITextStructureNavigator navigator = toolTipProvider.NavigatorService.GetTextStructureNavigator(textBuffer);
                TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);
                string searchText = extent.Span.GetText();

                foreach (string key in new[] { "demo", "add", "select" })
                {
                    int foundIdx = searchText.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);
                    if (foundIdx > -1)
                    {
                        ITrackingSpan applicable = currentSnapshot.CreateTrackingSpan(extent.Span.Start + foundIdx, key.Length, SpanTrackingMode.EdgeInclusive);

                        return new QuickInfoItem(applicable, "this is a tooltip");
                    }
                }
                //no keyword found, return nothing
                return new QuickInfoItem(null, "");
            });
            t.Start();
            return t;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support
    }

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Foo Quick Info Provider")]
    [Order(After = "Default")]
    [ContentType("code")]
    public class QuickToolTipProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickToolTip(this, textBuffer);
        }
    }

    [Export]
    [Name("Tooltip Quick Info Controller")]
    [ContentType("text")]
    public class QuickToolTipControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IAsyncQuickInfoBroker Broker { get; set; }
        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new QuickToolTipController(textView, subjectBuffers, this);
        }
    }

    public class QuickToolTipController : IIntellisenseController
    {
        private ITextView _textView;
        private IList<ITextBuffer> _buffers;
        private QuickToolTipControllerProvider _provider;
        private IAsyncQuickInfoSession _session;
        public QuickToolTipController(ITextView view, IList<ITextBuffer> buffers, QuickToolTipControllerProvider provider)
        {
            _textView = view;
            _buffers = buffers;
            _provider = provider;

            _textView.MouseHover += _textView_MouseHover;
        }

        private void _textView_MouseHover(object sender, MouseHoverEventArgs e)
        {
            //find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = _textView.BufferGraph.MapDownToFirstMatch(
                new SnapshotPoint(_textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                snapshot => _buffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor
            );
            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(
                    point.Value.Position,
                    PointTrackingMode.Positive
                );

                if (!_provider.Broker.IsQuickInfoActive(_textView))
                {
                    Task<IAsyncQuickInfoSession> t =_provider.Broker.TriggerQuickInfoAsync(_textView, triggerPoint, QuickInfoSessionOptions.TrackMouse);
                    t.Start();
                    t.Wait();
                    _session = t.Result;
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if (textView == _textView)
            {
                _textView.MouseHover -= _textView_MouseHover;
                _textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }
    }

}
