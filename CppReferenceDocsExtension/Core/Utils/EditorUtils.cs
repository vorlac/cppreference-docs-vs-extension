using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Core.Lang;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Document = EnvDTE.Document;
using Path = Microsoft.IO.Path;
using Project = EnvDTE.Project;
using Solution = EnvDTE.Solution;

namespace CppReferenceDocsExtension.Core.Utils
{
    public class DocumentLocation
    {
        public string Filename { get; set; }
        public int Line { set; get; }
        public int Column { set; get; }
    };

    public enum VSProjectType
    {
        None,
        VisualStudio,
        CMake,
        UnrealEngine,
    }

    public static class EditorUtils
    {
        public static ExtensionPackage Package { get; private set; }
        private static IServiceProvider ServiceProvider { get; set; }

        public static void Initialize(ExtensionPackage package) {
            EditorUtils.Package = package;
            EditorUtils.ServiceProvider = package;
        }

        public static async Task FindCodeElementAtCurrentLocationAsync() {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
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

        private static async Task<string> FindCodeElementAtLocationAsync(DocumentLocation location) {
            // Use VS intellisense data to find the struct scope and
            // store the first line before querying the pdb information
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            Document activeDocument = await EditorUtils.GetActiveDocumentAsync();
            ProjectItem projItem = activeDocument?.ProjectItem;
            FileCodeModel model = activeDocument == null ? null : projItem.FileCodeModel;
            CodeElements globalElements = model?.CodeElements;
            return await EditorUtils.FindCodeElementAtLocationAsync(globalElements, location.Line, location.Column);
        }

        public static async Task<List<NativeSymbol>> GetActiveDocumentCodeElementsAsync() {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            Document activeDocument = await EditorUtils.GetActiveDocumentAsync();
            ProjectItem projItem = activeDocument?.ProjectItem;
            FileCodeModel model = activeDocument == null ? null : projItem.FileCodeModel;
            CodeElements globalElements = model?.CodeElements;
            List<NativeSymbol> elems = await EditorUtils.FindCodeElementsAsync(globalElements);
            return await Task.FromResult(elems);
        }

        private static async Task<List<NativeSymbol>> FindCodeElementsAsync(IEnumerable elems, int depth = 0) {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();

            List<NativeSymbol> symbols = [];
            if (elems == null)
                return symbols;

            ++depth;
            foreach (CodeElement2 element in elems) {
                TextPoint startPos = element.GetStartPoint();
                TextPoint endPos = element.GetEndPoint();
                SymbolLocation symbolLocation = new() {
                    StartLine = startPos.Line,
                    StartOffset = startPos.LineCharOffset,
                    EndLine = endPos.Line,
                    EndOffset = endPos.LineCharOffset,
                };

                switch (element.Kind) {
                    // A class element
                    case vsCMElement.vsCMElementClass: {
                        CodeClass2 e = (CodeClass2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                Typename = e.Name,
                                ElementType = Element.Class,
                                Namespace = e.Namespace?.FullName,
                                Members = await EditorUtils.FindCodeElementsAsync(e.Members, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A function element
                    case vsCMElement.vsCMElementFunction: {
                        // std::function? lambda? functor? 
                        CodeFunction2 e = (CodeFunction2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                ElementType = Element.Function,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Parameters = await EditorUtils.FindCodeElementsAsync(e.Parameters, depth),
                                Location = symbolLocation,
                                // TODO: add overloads as a top level property
                                // Overloads = FindCodeElementsAsync(e.Overloads, depth),
                            }
                        );
                        break;
                    }
                    // A variable element
                    case vsCMElement.vsCMElementVariable: {
                        CodeVariable2 e = (CodeVariable2)element;
                        Qualifier qualifiers = Qualifier.None;
                        if (e.IsConstant)
                            qualifiers |= Qualifier.Const;

                        object a = e.InitExpression;
                        // TODO: add to NativeSymbol struct
                        vsCMInfoLocation test = e.InfoLocation;
                        switch (test) {
                            case vsCMInfoLocation.vsCMInfoLocationProject:  break;
                            case vsCMInfoLocation.vsCMInfoLocationExternal: break;
                            case vsCMInfoLocation.vsCMInfoLocationNone:     break;
                            case vsCMInfoLocation.vsCMInfoLocationVirtual:  break;
                            default:                                        throw new ArgumentOutOfRangeException();
                        }

                        string asdf;
                        using (MSBuildWorkspace workspace = MSBuildWorkspace.Create()) {
                            DTE2 applicationObject = EditorUtils.ServiceProvider.GetService(typeof(DTE)) as DTE2;
                            Assumes.NotNull(applicationObject);

                            string slnPath = applicationObject.Solution.FullName;
                            Microsoft.CodeAnalysis.Solution solution = await workspace.OpenSolutionAsync($@"{slnPath}");
                            foreach (Microsoft.CodeAnalysis.Project project in solution.Projects) {
                                Compilation compilation = await project.GetCompilationAsync();
                                foreach (Microsoft.CodeAnalysis.Document document in project.Documents) {
                                    SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
                                    SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                                    SyntaxNode root = await syntaxTree.GetRootAsync();
                                    IEnumerable<VariableDeclarationSyntax> variableDeclarations =
                                        root.DescendantNodes().OfType<VariableDeclarationSyntax>();

                                    foreach (VariableDeclarationSyntax declaration in variableDeclarations) {
                                        if (declaration.Type.IsVar) {
                                            VariableDeclaratorSyntax v = declaration.Variables.First();
                                            if (semanticModel.GetDeclaredSymbol(v) is ILocalSymbol symbol) {
                                                ITypeSymbol type = symbol.Type;
                                                asdf = $"Variable: {v.Identifier.Text}, Type: {type.ToDisplayString()}";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                ElementType = Element.Variable,
                                Typename = e.Type.AsFullName,
                                Qualifiers = qualifiers,
                                // TODO: children?
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A property element
                    case vsCMElement.vsCMElementProperty: {
                        CodeProperty2 e = (CodeProperty2)element;

                        switch (e.Access) {
                            case vsCMAccess.vsCMAccessPublic:             break;
                            case vsCMAccess.vsCMAccessPrivate:            break;
                            case vsCMAccess.vsCMAccessProject:            break;
                            case vsCMAccess.vsCMAccessProtected:          break;
                            case vsCMAccess.vsCMAccessDefault:            break;
                            case vsCMAccess.vsCMAccessAssemblyOrFamily:   break;
                            case vsCMAccess.vsCMAccessWithEvents:         break;
                            case vsCMAccess.vsCMAccessProjectOrProtected: break;
                            default:                                      throw new ArgumentOutOfRangeException();
                        }

                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                QualifiedName = e.FullName,
                                ElementType = Element.Property,
                                Typename = e.Type.AsFullName,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
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
                                QualifiedName = e.FullName,
                                ElementType = Element.Namespace,
                                Members = await EditorUtils.FindCodeElementsAsync(e.Members, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A parameter element
                    case vsCMElement.vsCMElementParameter: {
                        CodeParameter2 e = (CodeParameter2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                Typename = e.Type.AsFullName,
                                ElementType = Element.Parameter,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // An attribute element
                    case vsCMElement.vsCMElementAttribute: {
                        CodeAttribute2 e = (CodeAttribute2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Attribute,
                                Typename = e.Value,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // An interface element
                    case vsCMElement.vsCMElementInterface: {
                        CodeInterface2 e = (CodeInterface2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Interface,
                                Namespace = e.Namespace.FullName,
                                Members = await EditorUtils.FindCodeElementsAsync(e.Members, depth),
                                DerivedTypes = await EditorUtils.FindCodeElementsAsync(e.DerivedTypes, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A delegate element
                    case vsCMElement.vsCMElementDelegate: {
                        CodeDelegate2 e = (CodeDelegate2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Delegate,
                                Namespace = e.Namespace.FullName,
                                Members = await EditorUtils.FindCodeElementsAsync(e.Members, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // An enumerator element
                    case vsCMElement.vsCMElementEnum: {
                        CodeEnum e = (CodeEnum)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Enum,
                                Namespace = e.Namespace.FullName,
                                Members = await EditorUtils.FindCodeElementsAsync(e.Members, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A structure element
                    case vsCMElement.vsCMElementStruct: {
                        CodeStruct2 e = (CodeStruct2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Struct,
                                Namespace = e.Namespace?.FullName,
                                Members = await EditorUtils.FindCodeElementsAsync(e.Members, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A union element
                    case vsCMElement.vsCMElementUnion: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Union,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A local declaration statement element
                    case vsCMElement.vsCMElementLocalDeclStmt: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.LocalDeclaration,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A function invoke statement element
                    case vsCMElement.vsCMElementFunctionInvokeStmt: {
                        // TODO: look into Access, Qualifiers, ProjectItem, InfoLocation
                        //       Comment, DocComment, Prototype, etc
                        CodeFunction2 e = (CodeFunction2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.FunctionInvocation,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Parameters = await EditorUtils.FindCodeElementsAsync(e.Parameters, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A property set statement element
                    case vsCMElement.vsCMElementPropertySetStmt: {
                        CodeProperty2 e = (CodeProperty2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.PropertySetter,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // An assignment statement element
                    case vsCMElement.vsCMElementAssignmentStmt: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Assignment,
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // An inherits statement element
                    case vsCMElement.vsCMElementInheritsStmt: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Inheritance,
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // An include statement element
                    case vsCMElement.vsCMElementIncludeStmt: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.HeaderInclude,
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A macro element
                    case vsCMElement.vsCMElementMacro: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.Name,
                                QualifiedName = e.FullName,
                                ElementType = Element.Macro,
                                Children = await EditorUtils.FindCodeElementsAsync(element.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }

                    // An implements statement element
                    case vsCMElement.vsCMElementImplementsStmt:
                    // An option statement element
                    case vsCMElement.vsCMElementOptionStmt:
                    // An events declaration element
                    case vsCMElement.vsCMElementEventsDeclaration:
                    // A user-defined type declaration element
                    case vsCMElement.vsCMElementUDTDecl:
                    // A declare declaration element
                    case vsCMElement.vsCMElementDeclareDecl:
                    // A define statement element
                    case vsCMElement.vsCMElementDefineStmt: {
                        CodeElement2 e = (CodeElement2)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                QualifiedName = e.FullName,
                                ElementType = Element.Definition,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    // A type definition element
                    case vsCMElement.vsCMElementTypeDef: {
                        CodeType e = (CodeType)element;
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = Element.TypeAlias,
                                Children = await EditorUtils.FindCodeElementsAsync(e.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
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

                    // A VB attributes statement element
                    case vsCMElement.vsCMElementVBAttributeStmt:
                    // A VB attribute group element
                    case vsCMElement.vsCMElementVBAttributeGroup:
                    // An element not in the list
                    case vsCMElement.vsCMElementOther: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = Element.Other,
                                Children = await EditorUtils.FindCodeElementsAsync(element.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                    default: {
                        symbols.Add(
                            new NativeSymbol {
                                Name = element.FullName,
                                ElementType = Element.Unknown,
                                Children = await EditorUtils.FindCodeElementsAsync(element.Children, depth),
                                Location = symbolLocation,
                            }
                        );
                        break;
                    }
                }

                if (element.IsCodeType) {
                    // TODO: find better place for this
                    CodeType e = (CodeType)element;
                    List<NativeSymbol> members = await EditorUtils.FindCodeElementsAsync(e.Members, depth);
                    if (symbols.Count > 0) {
                        List<NativeSymbol> symMembers = symbols.Last().Members;
                        Assumes.NotNullOrEmpty(symMembers);
                        NativeSymbol sym = symbols[symbols.Count - 1];
                        sym.Members = members;
                        if (sym.Typename?.Length == 0)
                            sym.Typename = $"{sym.Name}";
                    }
                    else {
                        symbols.Add(
                            new NativeSymbol {
                                Name = e.FullName,
                                ElementType = Element.Unknown,
                                Namespace = e.Namespace.FullName,
                                Location = symbolLocation,
                            }
                        );
                    }
                }
            }

            --depth;
            return await Task.FromResult(symbols);
        }

        private static async Task<string> FindCodeElementAtLocationAsync(
            CodeElements elements, int line, int column) {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
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

            await EditorUtils.FindCodeElementsAsync(codeElements, 0);
            return sb.ToString();
        }

        private static async Task<Document> GetActiveDocumentAsync() {
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            DTE2 applicationObject = EditorUtils.ServiceProvider.GetService(typeof(DTE)) as DTE2;
            Assumes.Present(applicationObject);
            return applicationObject.ActiveDocument;
        }

        private static Project GetActiveProject() {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE2 applicationObject = EditorUtils.ServiceProvider.GetService(typeof(DTE)) as DTE2;

            Assumes.Present(applicationObject);
            return applicationObject.ActiveDocument?.ProjectItem?.ContainingProject;
        }

        private static Solution GetActiveSolution() {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE2 applicationObject = EditorUtils.ServiceProvider.GetService(typeof(SDTE)) as DTE2;

            Assumes.Present(applicationObject);
            return applicationObject.Solution;
        }

        private static string GetSolutionPath() {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution solution = EditorUtils.GetActiveSolution();
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
                 && File.Exists(EditorUtils.GetSolutionPath() + @"Engine/Source/UE4Editor.Target.cs"))
                    return VSProjectType.UnrealEngine;

                if (Path.GetFileNameWithoutExtension(solution.FullName) == "UE5"
                 && File.Exists(EditorUtils.GetSolutionPath() + @"Engine/Source/UnrealEditor.Target.cs"))
                    return VSProjectType.UnrealEngine;
            }

            return VSProjectType.VisualStudio;
        }

        public static void SaveActiveDocument() {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get full file path
            DTE2 applicationObject = EditorUtils.ServiceProvider.GetService(typeof(DTE2)) as DTE2;
            Assumes.Present(applicationObject);
            Document doc = applicationObject.ActiveDocument;
            if (doc is { ReadOnly: false, Saved: false })
                doc.Save();
        }
    }
}
