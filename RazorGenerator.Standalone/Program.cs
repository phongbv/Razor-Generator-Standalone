using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using RazorGenerator.Core;

namespace RazorGenerator.Standalone
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var projectRoot = args[0];
            var projectName = args[1];
            var filePath = args[2];
            // Project File Path to update on it
            var projectFilePath = projectRoot + "\\" + projectName + ".csproj";
            // Parent directory contain current view file
            var codeGenDirectory = Directory.GetParent(filePath).FullName;
            var viewName = Path.GetFileName(filePath);
            var generatedFileName =
                $"{Path.Combine(codeGenDirectory, Path.GetFileNameWithoutExtension(viewName))}.generated.cs";
            var relativeGeneratedFilePath = generatedFileName.Replace(projectRoot + "\\", "");
            var relativeViewPath = filePath.Replace(projectRoot + "\\", "");

            XDocument document = XDocument.Load(projectFilePath);
            var templateItem = document.Descendants().First(e => e.Name.LocalName == "Compile");

            var compileContainer = templateItem.Parent;

            var itemNamespace = String.Empty;
            using (var hostManager = new HostManager(projectRoot))
            {
                var projectRelativePath = GetProjectRelativePath(filePath, projectRoot);

                var host = hostManager.CreateHost(filePath, projectRelativePath, itemNamespace);
                var result = host.GenerateCode();
                File.WriteAllText(generatedFileName, result);
                if (GetElementByTagNameAndAttribute(document, relativeGeneratedFilePath, templateItem.Name.LocalName, "Include") == null)
                {
                    var newItem = new XElement(templateItem.Name,
                        new XAttribute("Include", relativeGeneratedFilePath),
                        new XElement(templateItem.Name.Namespace + "AutoGen", "True"),
                        new XElement(templateItem.Name.Namespace + "DesignTime", "True"),
                        new XElement(templateItem.Name.Namespace + "DependentUpon", viewName));
                    compileContainer.Add(newItem);
                    document.Save(projectFilePath);
                }
                
            }
        }

        private static string GetProjectRelativePath(string filePath, string projectRoot)
        {
            if (filePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return filePath.Substring(projectRoot.Length);
            }

            return filePath;
        }

        private static XElement GetElementByTagNameAndAttribute(XDocument document, string relativeFileName, string tagName, string attribute)
        {
            return document.Descendants().FirstOrDefault(e =>
                e.Name.LocalName == tagName && e.HasAttributes && e.Attributes().Any(attr =>
                    attr.Name.LocalName == attribute && attr.Value == relativeFileName));
        }
    }
}