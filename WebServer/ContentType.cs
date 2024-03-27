namespace WebServer
{
	public class ContentType
	{
		public bool IsText { get; set; }
		public string Extension { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public string Mime { get; set; } = string.Empty;
	}
}
