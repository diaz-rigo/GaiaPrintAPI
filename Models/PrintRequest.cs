namespace GaiaPrintAPI.Models
{
    public class PrintRequest
    {
        public string PrinterName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string? Payload { get; set; }
        public bool? CutPaper { get; set; }
        public string? CutType { get; set; }
        public int? FeedLines { get; set; }
        public string? EncodingName { get; set; }
    }

    public class PrinterInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
