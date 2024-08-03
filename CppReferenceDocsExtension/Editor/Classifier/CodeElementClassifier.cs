//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using CppReferenceDocsExtension.Editor.QuickInfo;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Classification;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Text.Tagging;
//using Microsoft.VisualStudio.Utilities;

//namespace CppReferenceDocsExtension.Editor.Classifier
//{
//    internal sealed class CodeElementClassifier : ITagger<ClassificationTag>
//    {
//        ITextBuffer buffer;
//        ITagAggregator<ClassificationTag> aggregator;
//        readonly IDictionary<ClassificationTag, IClassificationType> elemTypes;

//        internal CodeElementClassifier(ITextBuffer buffer, ITagAggregator<ClassificationTag> tagAggregator,
//                                       IClassificationTypeRegistryService typeService) {
//            this.buffer = buffer;
//            this.aggregator = tagAggregator;
//            this.elemTypes = new Dictionary<ClassificationTag, IClassificationType>();
//        }

//        public event EventHandler<SnapshotSpanEventArgs> TagsChanged {
//            add { }
//            remove { }
//        }

//        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
//            foreach (var tagSpan in this.aggregator.GetTags(spans)) {
//                NormalizedSnapshotSpanCollection tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);
//                yield return new TagSpan<ClassificationTag>(
//                    tagSpans[0],
//                    new ClassificationTag(this.elemTypes[tagSpan.Tag])
//                );
//            }
//        }
//    }
//}


