namespace NuGetCalcWeb.ViewModels
{
    public class ErrorModel
    {
        public ErrorModel(string header, string message = null, string detail = null)
        {
            this.Header = header;
            this.Message = message;
            this.Detail = detail;
        }

        public string Header { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
    }
}