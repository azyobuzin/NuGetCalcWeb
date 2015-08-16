namespace NuGetCalcWeb.ViewModels.FilePreview
{
    public class AssemblyModel : FilePreviewModel
    {
        public string AssemblyName { get; set; }
        public string AssemblyDescription { get; set; }
        public NamespaceModel[] Namespaces { get; set; }
        public TypeDescription[] TypeDescriptions { get; set; }
    }

    public class NamespaceModel
    {
        public string Name { get; set; }
        public TypeModel[] Types { get; set; }
    }

    public class TypeModel
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string HtmlClass { get; set; }
        public TypeModel[] NestedTypes { get; set; }
    }

    public class TypeDescription
    {
        public string FullName { get; set; }
        public string Code { get; set; }
    }
}
