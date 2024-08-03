using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CppReferenceDocsExtension.Core.Lang
{
    [Serializable]
    [DebuggerDisplay("[{this.ElementType}] {this.ToString()}")]
    public class NativeSymbol
    {
        public string Name;
        public string QualifiedName;
        public string Namespace;
        public string Typename;
        public string ReturnType;
        public string Value;

        public Element ElementType;
        public Qualifier Qualifiers;
        public SymbolLocation Location;

        public List<NativeSymbol> Parameters = [];
        public List<NativeSymbol> Members = [];
        public List<NativeSymbol> Children = [];
        public List<NativeSymbol> DerivedTypes = [];

        public string ToStringRecursive(int depth = 0) {
            StringBuilder sb = new();

            if (this.ElementType != Element.Parameter) {
                string indent = new(' ', depth * 3);
                string symInfo = $"{this}";

                if (symInfo.Length > 0)
                    sb.AppendLine($"{indent}{symInfo}");

                ++depth;

                foreach (NativeSymbol child in this.Children) {
                    string childInfo = child.ToStringRecursive(depth);
                    if (childInfo.Length > 0)
                        sb.Append(childInfo);
                }

                foreach (NativeSymbol member in this.Members) {
                    string memberInfo = member.ToStringRecursive(depth);
                    if (memberInfo.Length > 0)
                        sb.Append(memberInfo);
                }

                --depth;
            }

            return sb.ToString();
        }

        public override string ToString() {
            string ret;
            switch (this.ElementType) {
                case Element.Namespace:
                    ret = $"{this.Name}";
                    break;

                case Element.FunctionInvocation:
                case Element.Function:
                    string parameters = string.Join(", ", this.Parameters);
                    ret = $"{this.Typename} {this.Name}({parameters})".TrimStart(' ');
                    break;

                case Element.HeaderInclude:
                    ret = $"#include <{this.Name}>";
                    break;

                case Element.Interface:
                case Element.Union:
                case Element.Class:
                case Element.Struct:
                    ret = $"{this.Name}";
                    break;

                case Element.Property:
                case Element.Enum:
                case Element.Parameter:
                case Element.Variable:
                case Element.Macro:
                case Element.Delegate:
                case Element.Attribute:
                case Element.LocalDeclaration:
                case Element.PropertySetter:
                case Element.PropertyGetter:
                case Element.Unknown:
                case Element.Definition:
                case Element.Assignment:
                case Element.Inheritance:
                case Element.TypeAlias:
                    ret = $"{this.Typename} {this.Name}";
                    break;

                case Element.Other:
                default:
                    ret = $"{this.Typename} {this.Namespace} {this.Name}";
                    break;
            }

            return ret;
        }
    }
}
