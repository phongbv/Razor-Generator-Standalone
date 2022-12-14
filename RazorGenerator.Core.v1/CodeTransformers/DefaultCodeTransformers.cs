using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace RazorGenerator.Core
{
    public class AddGeneratedClassAttribute : RazorCodeTransformerBase
    {
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            string tool = "RazorGenerator";
            Version version = GetType().Assembly.GetName().Version;
            generatedClass.CustomAttributes.Add(
                new CodeAttributeDeclaration(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName,
                        new CodeAttributeArgument(new CodePrimitiveExpression(tool)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(version.ToString()))
            ));
        }
    }

    public class SetImports : RazorCodeTransformerBase
    {
        private readonly IEnumerable<string> _imports;
        private readonly bool _replaceExisting;

        public SetImports(IEnumerable<string> imports, bool replaceExisting = false)
        {
            _imports = imports;
            _replaceExisting = replaceExisting;
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            if (_replaceExisting)
            {
                razorHost.NamespaceImports.Clear();
            }
            foreach (var import in _imports)
            {
                razorHost.NamespaceImports.Add(import);
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            // Sort imports.
            var imports = new List<CodeNamespaceImport>(generatedNamespace.Imports.OfType<CodeNamespaceImport>());
            generatedNamespace.Imports.Clear();
            generatedNamespace.Imports.AddRange(imports.OrderBy(c => c.Namespace, NamespaceComparer.Instance).ToArray());
        }

        private class NamespaceComparer : IComparer<string>
        {
            public static readonly NamespaceComparer Instance = new NamespaceComparer();
            public int Compare(string x, string y)
            {
                if (x == null || y == null)
                {
                    return StringComparer.OrdinalIgnoreCase.Compare(x, y);
                }
                bool xIsSystem = x.StartsWith("System", StringComparison.OrdinalIgnoreCase);
                bool yIsSystem = y.StartsWith("System", StringComparison.OrdinalIgnoreCase);

                if (!(xIsSystem ^ yIsSystem))
                {
                    return x.CompareTo(y);
                }
                else if (xIsSystem)
                {
                    return -1;
                }
                return 1;
            }
        }
    }

    public class MakeTypeStatic : RazorCodeTransformerBase
    {
        public override string ProcessOutput(string codeContent)
        {
            return _razorHost.CodeLanguageUtil.MakeTypeStatic(codeContent);
        }
    }

    public class SetBaseType : RazorCodeTransformerBase
    {
        private readonly string _typeName;
        private readonly bool _override;

        public SetBaseType(string typeName, bool @override = false)
        {
            _typeName = typeName;
            _override = @override;
        }

        public SetBaseType(Type type, bool @override = false)
            : this(type.FullName, @override: @override)
        {
        }

        private bool IsDefaultBaseClass(string baseClass)
        {
            return string.IsNullOrEmpty(baseClass) || typeof(System.Web.WebPages.WebPage).FullName == baseClass;
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            if (_override || IsDefaultBaseClass(razorHost.DefaultBaseClass))
                razorHost.DefaultBaseClass = _typeName;
        }
    }

    public class MakeTypeHelper : RazorCodeTransformerBase
    {
        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            razorHost.StaticHelpers = true;
        }
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            generatedClass.Members.Remove(executeMethod);
        }
    }
}
