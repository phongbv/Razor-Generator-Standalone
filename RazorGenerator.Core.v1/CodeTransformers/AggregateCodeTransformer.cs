using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public abstract class AggregateCodeTransformer : RazorCodeTransformerBase
    {

        protected abstract IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get;
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            base.Initialize(razorHost, directives);

            foreach (var transformer in CodeTransformers)
            {
                transformer.Initialize(razorHost, directives);
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            foreach (var transformer in CodeTransformers)
            {
                transformer.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            }
        }

        public override string ProcessOutput(string codeContent)
        {
            foreach (var transformer in CodeTransformers)
            {
                codeContent = transformer.ProcessOutput(codeContent);
            }
            return codeContent;
        }
    }
}
