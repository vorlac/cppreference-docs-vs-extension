using System;

namespace CppReferenceDocsExtension.Core.Lang
{
    public struct SymbolLocation
    {
        public string Filename;
        public int StartLine;
        public int StartOffset;
        public int EndLine;
        public int EndOffset;
    }

    [Flags]
    public enum Qualifier
    {
        None = 0,
        Const = 1 << 0,
        ConstExpr = 1 << 1,
        ConstEval = 1 << 2,
        ConstInit = 1 << 3,
        Static = 1 << 4,
        Inline = 1 << 5,
        Mutable = 1 << 6,
        Volatile = 1 << 7,
    }

    public enum AccessSpecifier
    {
        Unknown,
        Public,
        Private,
        Protected,
    }

    public enum Element
    {
        Unknown,
        Namespace,
        Interface,
        Property,
        Union,
        Class,
        Struct,
        Function,
        FunctionInvocation,
        Parameter,
        Variable,
        Macro,
        Definition,
        Delegate,
        Attribute,
        HeaderInclude,
        Assignment,
        Inheritance,
        TypeAlias,
        LocalDeclaration,
        PropertySetter,
        PropertyGetter,
        Enum,
        Other,
    }
}
