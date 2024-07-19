using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IO;
using Microsoft.VisualStudio.TextManager.Interop;
using Path = Microsoft.IO.Path;

namespace CppReferenceDocsExtension.Editor
{
    public class DocumentLocation
    {
        public string Filename { get; set; }
        public int Line { set; get; }
        public int Column { set; get; }

        /// is it a symbol in user project sources,
        /// or from an external dependency?
        public bool IsExternal { get; set; }
    };

    [Flags]
    public enum VSProjectType
    {
        None,
        VisualStudio,
        CMake,
        UnrealEngine,
    }

    [Serializable]
    public struct NativeSymbol
    {
        [Flags]
        public enum Element
        {
            Unknown = 1 << 0,
            Namespace = 1 << 1,
            Interface = 1 << 2,
            Property = 1 << 3,
            Union = 1 << 4,
            Class = 1 << 5,
            Struct = 1 << 6,
            Function = 1 << 7,
            Parameter = 1 << 8,
            Variable = 1 << 9,
            Macro = 1 << 10,
            Delegate = 1 << 11,
            Attribute = 1 << 12,
            FunctionInvoke = 1 << 13,
            LocalDeclaration = 1 << 14,

            PropertySetter,
            PropertyGetter,

            Enum = 1 << 15,

            Other = 1 << 13,
        };

        [Flags]
        public enum Qualifier
        {
            None,
            Const,
            ConstExpr,
            ConstEval,
            ConstInit,
            Static,
            Inline,
            Mutable,
            Volatile
        };

        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Typename { get; set; }
        public Qualifier Qualifiers { get; set; }
        public Element ElementType { get; set; }
        public List<NativeSymbol> Parameters { get; set; }
        public List<NativeSymbol> Members { get; set; }
        public List<NativeSymbol> Parent { get; set; }
        public List<NativeSymbol> Children { get; set; }
        public List<NativeSymbol> DerivedTypes { get; set; }
    }

    public static class EditorUtils
    {
        public static ExtensionPackage Package { get; set; }
        private static IServiceProvider ServiceProvider { get; set; }

        public static void Initialize(ExtensionPackage package) {
            Package = package;
            ServiceProvider = package;
        }

        public static async Task FindCodeElementAtCurrentLocationAsync() {
            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            DocumentLocation location = await EditorUtils.GetCurrentCaretLocationAsync().ConfigureAwait(true);
            await EditorUtils.FindCodeElementAtLocationAsync(location).ConfigureAwait(true);
        }

        private static async Task<DocumentLocation> GetCurrentCaretLocationAsync() {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            Document activeDocument = await EditorUtils.GetActiveDocumentAsync().ConfigureAwait(true);
            if (activeDocument == null)
                return null;
            if (EditorUtils.ServiceProvider.GetService(typeof(SVsTextManager)) is not IVsTextManager2 textManager)
                return null;

            textManager.GetActiveView2(1, null, (int)_VIEWFRAMETYPE.vftCodeWindow, out IVsTextView view);
            if (view == null)
                return null;

            view.GetCaretPos(out int line, out int col);

            return new() {
                Filename = activeDocument.FullName,
                Line = line,
                Column = col
            };
        }

        public static async Task<string> FindCodeElementAtLocationAsync(DocumentLocation location) {
            // Use VS intellisense data to find the struct scope and
            // store the first line before querying the pdb information
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            Document activeDocument = await EditorUtils.GetActiveDocumentAsync();
            ProjectItem projItem = activeDocument?.ProjectItem;
            FileCodeModel model = activeDocument == null ? null : projItem.FileCodeModel;
            CodeElements globalElements = model?.CodeElements;
            return EditorUtils.FindCodeElementAtLocation(globalElements, location.Line, location.Column);
        }

        public static async Task<List<NativeSymbol>> GetActiveDocumentCodeElementsAsync() {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            Document activeDocument = await EditorUtils.GetActiveDocumentAsync();
            ProjectItem projItem = activeDocument?.ProjectItem;
            FileCodeModel model = activeDocument == null ? null : projItem.FileCodeModel;
            CodeElements globalElements = model?.CodeElements;
            StringBuilder sb = new();

            List<NativeSymbol> elems = EditorUtils.FindCodeElements(globalElements, 0);
            return await Task.FromResult(elems);
        }

        private static List<NativeSymbol> FindCodeElements(IEnumerable elems, int depth) {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<NativeSymbol> symbols = [];
            if (elems == null)
                return symbols;

            ++depth;
            foreach (CodeElement element in elems) {
                switch (element.Kind) {
                    // A class element
                    case vsCMElement.vsCMElementClass: {
                        CodeClass e = (CodeClass)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Class,
                                Namespace = e.Namespace?.FullName,
                                Members = EditorUtils.FindCodeElements(e.Members, depth),
                                //Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // A structure element
                    case vsCMElement.vsCMElementStruct: {
                        CodeStruct e = (CodeStruct)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Struct,
                                Namespace = e.Namespace?.FullName,
                                Members = EditorUtils.FindCodeElements(e.Members, depth),
                                //Children = EditorUtils.FindCodeElements(e.Children, depth),
                                //DerivedTypes = EditorUtils.FindCodeElements(e.DerivedTypes, depth),
                            }
                        );
                        break;
                    }
                    // A union element
                    case vsCMElement.vsCMElementUnion: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = NativeSymbol.Element.Union,
                                Children = EditorUtils.FindCodeElements(element.Children, depth),
                            }
                        );
                        break;
                    }
                    // A namespace element
                    case vsCMElement.vsCMElementNamespace: {
                        CodeNamespace e = (CodeNamespace)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Namespace,
                                Members = EditorUtils.FindCodeElements(e.Members, depth),
                                //Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // A function element
                    case vsCMElement.vsCMElementFunction: {
                        CodeFunction e = (CodeFunction)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Function,
                                // Function element children are params
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                                // TODO: add overloads as a top level property
                                // Overloads = EditorUtils.FindCodeElements(e.Overloads, depth),
                            }
                        );
                        break;
                    }
                    // A variable element
                    case vsCMElement.vsCMElementVariable: {
                        CodeVariable e = (CodeVariable)element;
                        NativeSymbol.Qualifier qualifiers = NativeSymbol.Qualifier.None;
                        if (e.IsConstant)
                            qualifiers |= NativeSymbol.Qualifier.Const;

                        // TODO: add to NativeSymbol struct
                        vsCMInfoLocation test = e.InfoLocation;
                        switch (test) {
                            case vsCMInfoLocation.vsCMInfoLocationProject:  break;
                            case vsCMInfoLocation.vsCMInfoLocationExternal: break;
                            case vsCMInfoLocation.vsCMInfoLocationNone:     break;
                            case vsCMInfoLocation.vsCMInfoLocationVirtual:  break;
                        }

                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Variable,
                                Typename = e.Type.AsFullName,
                                Qualifiers = qualifiers,
                                // TODO: children?
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // A property element
                    case vsCMElement.vsCMElementProperty: {
                        CodeProperty e = (CodeProperty)element;

                        switch (e.Access) {
                            case vsCMAccess.vsCMAccessPublic:             break;
                            case vsCMAccess.vsCMAccessPrivate:            break;
                            case vsCMAccess.vsCMAccessProject:            break;
                            case vsCMAccess.vsCMAccessProtected:          break;
                            case vsCMAccess.vsCMAccessDefault:            break;
                            case vsCMAccess.vsCMAccessAssemblyOrFamily:   break;
                            case vsCMAccess.vsCMAccessWithEvents:         break;
                            case vsCMAccess.vsCMAccessProjectOrProtected: break;
                        }

                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Property,
                                Typename = e.Type.AsFullName,
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // A parameter element
                    case vsCMElement.vsCMElementParameter: {
                        CodeParameter e = (CodeParameter)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Parameter,
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // An attribute element
                    case vsCMElement.vsCMElementAttribute: {
                        CodeAttribute e = (CodeAttribute)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Attribute,
                                Typename = e.Value,
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // An interface element
                    case vsCMElement.vsCMElementInterface: {
                        CodeInterface e = (CodeInterface)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Interface,
                                Namespace = e.Namespace.FullName,
                                Members = EditorUtils.FindCodeElements(e.Members, depth),
                                //Children = EditorUtils.FindCodeElements(e.Children, depth),
                                DerivedTypes = EditorUtils.FindCodeElements(e.DerivedTypes, depth),
                            }
                        );
                        break;
                    }
                    // A delegate element
                    case vsCMElement.vsCMElementDelegate: {
                        CodeDelegate e = (CodeDelegate)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Delegate,
                                Namespace = e.Namespace.FullName,
                                Members = EditorUtils.FindCodeElements(e.Members, depth),
                            }
                        );
                        break;
                    }
                    // An enumerator element
                    case vsCMElement.vsCMElementEnum: {
                        CodeEnum e = (CodeEnum)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Enum,
                                Namespace = e.Namespace.FullName,
                                Members = EditorUtils.FindCodeElements(e.Members, depth),
                            }
                        );
                        break;
                    }
                    // A local declaration statement element
                    case vsCMElement.vsCMElementLocalDeclStmt: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = NativeSymbol.Element.LocalDeclaration,
                                Children = EditorUtils.FindCodeElements(element.Children, depth),
                            }
                        );
                        EditorUtils.FindCodeElements(element.Children, depth);
                        break;
                    }
                    // A function invoke statement element
                    case vsCMElement.vsCMElementFunctionInvokeStmt: {
                        // TODO: look into Access, Qualifiers, ProjectItem, InfoLocation
                        //       Comment, DocComment, Prototype, etc
                        CodeFunction e = (CodeFunction)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.FunctionInvoke,
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // A property set statement element
                    case vsCMElement.vsCMElementPropertySetStmt: {
                        CodeProperty e = (CodeProperty)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.PropertySetter,
                                Children = EditorUtils.FindCodeElements(e.Children, depth),
                            }
                        );
                        break;
                    }
                    // A macro element
                    case vsCMElement.vsCMElementMacro: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = NativeSymbol.Element.Macro,
                                Children = EditorUtils.FindCodeElements(element.Children, depth),
                            }
                        );
                        break;
                    }
                    // An assignment statement element
                    case vsCMElement.vsCMElementAssignmentStmt:
                    // An inherits statement element
                    case vsCMElement.vsCMElementInheritsStmt:
                    // An implements statement element
                    case vsCMElement.vsCMElementImplementsStmt:
                    // An option statement element
                    case vsCMElement.vsCMElementOptionStmt:
                    // A VB attributes statement element
                    case vsCMElement.vsCMElementVBAttributeStmt:
                    // A VB attribute group element
                    case vsCMElement.vsCMElementVBAttributeGroup:
                    // An events declaration element
                    case vsCMElement.vsCMElementEventsDeclaration:
                    // A user-defined type declaration element
                    case vsCMElement.vsCMElementUDTDecl:
                    // A declare declaration element
                    case vsCMElement.vsCMElementDeclareDecl:
                    // A define statement element
                    case vsCMElement.vsCMElementDefineStmt:
                    // A type definition element
                    case vsCMElement.vsCMElementTypeDef:
                    // An include statement element
                    case vsCMElement.vsCMElementIncludeStmt:
                    // A using statement element
                    case vsCMElement.vsCMElementUsingStmt:
                    // A map element
                    case vsCMElement.vsCMElementMap:
                    // An IDL import element
                    case vsCMElement.vsCMElementIDLImport:
                    // An IDL import library element
                    case vsCMElement.vsCMElementIDLImportLib:
                    // An IDL co-class element
                    case vsCMElement.vsCMElementIDLCoClass:
                    // An IDL library element
                    case vsCMElement.vsCMElementIDLLibrary:
                    // An import statement element
                    case vsCMElement.vsCMElementImportStmt:
                    // A map entry element
                    case vsCMElement.vsCMElementMapEntry:
                    // A VC base element
                    case vsCMElement.vsCMElementVCBase:
                    // An event element
                    case vsCMElement.vsCMElementEvent:
                    // A module element
                    case vsCMElement.vsCMElementModule:
                    // An element not in the list
                    case vsCMElement.vsCMElementOther: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = NativeSymbol.Element.Other,
                                Children = EditorUtils.FindCodeElements(element.Children, depth),
                            }
                        );
                        break;
                    }
                    default: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = NativeSymbol.Element.Unknown,
                                Children = EditorUtils.FindCodeElements(element.Children, depth),
                            }
                        );
                        break;
                    }
                }

                if (element.IsCodeType) {
                    // TODO: find better place for this
                    CodeType e = (CodeType)element;
                    List<NativeSymbol> members = EditorUtils.FindCodeElements(e.Members, depth);
                    if (symbols.Count > 0) {
                        List<NativeSymbol> symMembers = symbols.Last().Members;
                        Assumes.NotNullOrEmpty(symMembers);
                        NativeSymbol sym = symbols[symbols.Count - 1];
                        sym.Members = members;
                    }
                    else {
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = NativeSymbol.Element.Unknown,
                                Namespace = e.Namespace.FullName
                            }
                        );
                    }
                }
            }

            --depth;
            return symbols;
        }

        private static string FindCodeElementAtLocation(
            CodeElements elements, int line, int column) {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (elements == null)
                return null;

            StringBuilder sb = new();
            IEnumerable codeElements =
                from CodeElement elem in elements
                let elementStart = elem.StartPoint
                let elementEnd = elem.EndPoint
                where line >= elementStart.Line
                   && line <= elementEnd.Line
                //&& column >= elem.StartPoint.LineCharOffset - 1
                //&& column <= elem.EndPoint.LineCharOffset + 1
                select elem;

            EditorUtils.FindCodeElements(codeElements, 0);
            return sb.ToString();
        }

        public static async Task<Document> GetActiveDocumentAsync() {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            DTE2 applicationObject = EditorUtils.ServiceProvider.GetService(typeof(DTE))
                as EnvDTE80.DTE2;

            Assumes.Present(applicationObject);
            return applicationObject.ActiveDocument;
        }

        private static Project GetActiveProject() {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE2 applicationObject = ServiceProvider.GetService(typeof(DTE))
                as EnvDTE80.DTE2;

            Assumes.Present(applicationObject);
            return applicationObject.ActiveDocument?.ProjectItem?.ContainingProject;
        }

        private static Solution GetActiveSolution() {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE2 applicationObject = ServiceProvider.GetService(typeof(SDTE))
                as EnvDTE80.DTE2;

            Assumes.Present(applicationObject);
            return applicationObject.Solution;
        }

        private static string GetSolutionPath() {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution solution = GetActiveSolution();
            if (solution == null)
                return null;

            return Path.HasExtension(solution.FullName)
                ? $"{Path.GetDirectoryName(solution.FullName)}\\"
                : $"{solution.FullName}\\";
        }

        public static string GetExtensionInstallationDirectory() {
            try {
                Uri uri = new(typeof(ExtensionPackage).Assembly.CodeBase, UriKind.Absolute);
                return $"{Path.GetDirectoryName(uri.LocalPath)}\\";
            }
            catch {
                return null;
            }
        }

        public static VSProjectType GetEditorMode() {
            ThreadHelper.ThrowIfNotOnUIThread();
            Project project = EditorUtils.GetActiveProject();

            if (project == null)
                return VSProjectType.None;
            if (project.Object == null)
                return VSProjectType.CMake;

            Solution solution = EditorUtils.GetActiveSolution();
            if (solution != null) {
                string uproject = Path.ChangeExtension(solution.FullName, "uproject");
                if (File.Exists(uproject))
                    return VSProjectType.UnrealEngine;

                if (Path.GetFileNameWithoutExtension(solution.FullName) == "UE4"
                 && File.Exists(GetSolutionPath() + @"Engine/Source/UE4Editor.Target.cs"))
                    return VSProjectType.UnrealEngine;

                if (Path.GetFileNameWithoutExtension(solution.FullName) == "UE5"
                 && File.Exists(GetSolutionPath() + @"Engine/Source/UnrealEditor.Target.cs"))
                    return VSProjectType.UnrealEngine;
            }

            return VSProjectType.VisualStudio;
        }

        public static void SaveActiveDocument() {
            ThreadHelper.ThrowIfNotOnUIThread();

            //Get full file path
            DTE2 applicationObject = ServiceProvider.GetService(typeof(DTE)) as EnvDTE80.DTE2;
            Assumes.Present(applicationObject);

            Document doc = applicationObject.ActiveDocument;
            if (doc != null && !doc.ReadOnly && !doc.Saved) {
                doc.Save();
            }
        }
    }
}
