using System.Text;

namespace WebServer
{
	public class ResponseGenerator
	{
		public async Task<StringBuilder> SetHTMLResponseAsync(StringBuilder sb, Dictionary<string, string> requestParams, ContentType contentType)
		{
			var fileContent = await File.ReadAllTextAsync("web/" + requestParams["Path"]);
			sb.Append($"HTTP/1.1 200 OK\r\nContent-Type: {contentType.Mime}\r\n\r\n");
			sb.Append(fileContent);
			return sb;
		}

		public StringBuilder NotFound(StringBuilder sb)
		{
			sb.Append("HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\n\r\n");
			sb.Append("<html><body>404 Not Found</body></html>");
			return sb;
		}

		public StringBuilder SetImageRequestHeader(StringBuilder sb, int fileSize, ContentType contentType)
		{
			sb.Append("HTTP/1.1 200 OK\r\n");
			sb.Append($"Content-Type: {contentType.Mime}\r\n");
			sb.Append($"Content-Length: {fileSize}\r\n");
			sb.Append("\r\n");
			return sb;
		}
	}
}
