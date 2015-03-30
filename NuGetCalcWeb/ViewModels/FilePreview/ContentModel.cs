namespace NuGetCalcWeb.ViewModels.FilePreview
{
    public class ContentModel : FilePreviewModel
    {
        public ContentModel(string content)
        {
            this.Content = content;
        }

        public string Content { get; set; }
    }
}
