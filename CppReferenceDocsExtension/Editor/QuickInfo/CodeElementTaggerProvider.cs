//using System.ComponentModel.Composition;
//using Microsoft.VisualStudio.Shell.Interop;
//#using CppReferenceDocsExtension.Editor.Classifier;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Classification;
//using Microsoft.VisualStudio.Text.Tagging;
//using Microsoft.VisualStudio.Utilities;

//namespace CppReferenceDocsExtension.Editor.QuickInfo
//{
//    [ContentType("code")]
//    [Export(typeof(ITaggerProvider))]
//    [TagType(typeof(ClassificationTag))]
//    internal sealed class CodeElementTaggerProvider : ITaggerProvider
//    {
//        [Export]
//        [BaseDefinition("code")]
//        internal static ContentTypeDefinition ContentType = null;

//        [Export]
//        [ContentType("code")]
//        internal static FileExtensionToContentTypeDefinition FileType = null;

//        [Import]
//        internal IClassificationTypeRegistryService classificationTypeRegistry = null;

//        [Import]
//        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

//        public ITagger<T> CreateTagger<T>(ITextBuffer buffer)
//            where T : ITag {
//            ITagAggregator<ClassificationTag> codeElemTagAggregator =
//                this.aggregatorFactory.CreateTagAggregator<ClassificationTag>(buffer);

//            return new CodeElementClassifier(
//                buffer,
//                codeElemTagAggregator,
//                this.classificationTypeRegistry
//            ) as ITagger<T>;
//        }
//    }
//}


