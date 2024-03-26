using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer
{
	internal class HttpServer
	{
		private ManualResetEvent signal;
		private TcpListener listener;
		private readonly string WebFolder;
		string[] binaryExtensions = { 
			".doc", 
			".docx", 
			".gif", 
			".jpeg", 
			".jpg", 
			".mp3", 
			".mp4", 
			".otf", 
			".pdf", 
			".png", 
			".ppt", 
			".pptx", 
			".rar", 
			".ttf", 
			".xls", 
			".xlsx", 
			".woff", 
			".woff2", 
			".zip" 
		};
		Dictionary<string, string> MimeTypes = new Dictionary<string, string>
		{
			{ ".css", "text/css" },
			{ ".csv", "text/csv" },
			{ ".doc", "application/msword" },
			{ ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
			{ ".gif", "image/gif" },
			{ ".jpeg", "image/jpeg" },
			{ ".jpg", "image/jpeg" },
			{ ".json", "application/json" },
			{ ".mp3", "audio/mpeg" },
			{ ".mp4", "video/mp4" },
			{ ".pdf", "application/pdf" },
			{ ".png", "image/png" },
			{ ".ppt", "application/vnd.ms-powerpoint" },
			{ ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
			{ ".rar", "application/x-rar-compressed" },
			{ ".txt", "text/plain" },
			{ ".xml", "application/xml" },
			{ ".xls", "application/vnd.ms-excel" },
			{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
			{ ".zip", "application/zip" }
		};

		public HttpServer(IPAddress ip, int port, ManualResetEvent signal, string WebFolder) 
		{
			listener = new TcpListener(ip, port);
			this.signal = signal;
			this.WebFolder = WebFolder;

			ShowMessage($"Server socket - {ip.ToString()}:{port}");
			ShowMessage($"Server www directory - /{WebFolder}");
			ShowMessage($"Found {MimeTypes.Count} supported types - {string.Join(", ", MimeTypes.Keys.Select(k => k.ToString()))}");
		}

		public void Listen() {
			listener.Start();

			do
			{
				var client = listener.AcceptTcpClient();
				Task.Run(() => HandleHttpsRequestAsync(client));
				Thread.Sleep(50);
			}
			while (!signal.WaitOne(0));

			listener.Stop();
		}

		private async Task HandleHttpsRequestAsync(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			var buffer = new byte[1024 * 4];
			var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			if (request.Length != 0)
			{
				var requestParams = Parse(request);
				await SendResponseAsync(requestParams, stream);
			}

			client.Close();
		}

		private async Task SendTextResponseAsync(NetworkStream stream, StringBuilder response)
		{
			var responseBuffer = Encoding.UTF8.GetBytes(response.ToString());
			await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
			await stream.FlushAsync();
		}

		private async Task SendBinaryResponseAsync(NetworkStream stream, byte[] binary)
		{
			await stream.WriteAsync(binary, 0, binary.Length);
			await stream.FlushAsync();
		}

		private async Task<StringBuilder> SetHTMLResponseAsync(StringBuilder sb, Dictionary<string, string> requestParams, ContentType contentType)
		{
			var fileContent = await File.ReadAllTextAsync("web/" + requestParams["Path"]);
			sb.Append($"HTTP/1.1 200 OK\r\nContent-Type: {contentType.Mime}\r\n\r\n");
			sb.Append(fileContent);
			return sb;
		}

		private StringBuilder NotFound(StringBuilder sb)
		{
			sb.Append("HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\n\r\n");
			sb.Append("<html><body>404 Not Found</body></html>");
			return sb;
		}

		private StringBuilder SetImageRequestHeader(StringBuilder sb, Dictionary<string, string> requestParams, int fileSize, ContentType contentType)
		{
			sb.Append("HTTP/1.1 200 OK\r\n");
			sb.Append($"Content-Type: {contentType.Mime}\r\n");
			sb.Append($"Content-Length: {fileSize}\r\n");
			sb.Append("\r\n");
			return sb;
		}

		private void ShowMessage(string message)
		{
			Console.WriteLine($"[{DateTime.Now}]: {message}.");
		}

		private async Task SendResponseAsync(Dictionary<string, string> requestParams, NetworkStream stream)
		{
			StringBuilder sb = new StringBuilder();

			if (!Directory.Exists("web"))
			{
				return;
			}
			
			var fileContentType = GetFileContentType(requestParams);

			ShowMessage($"Requested file with path - {requestParams["Path"]}");

			if (fileContentType != null)
			{
				if (fileContentType.IsText)
				{
					await SetHTMLResponseAsync(sb, requestParams, fileContentType);
					await SendTextResponseAsync(stream, sb);
				}
				else
				{
					byte[] binary = File.ReadAllBytes(WebFolder + requestParams["Path"]);
					SetImageRequestHeader(sb, requestParams, binary.Length, fileContentType);
					await SendTextResponseAsync(stream, sb);
					await SendBinaryResponseAsync(stream, binary);
				}

				ShowMessage($"Code: \x1b[32m200\x1b[0m. File {fileContentType.Path.Substring(1)} has been sent successfully");
			}
			else
			{
				NotFound(sb);
				await SendTextResponseAsync(stream, sb);
				ShowMessage($"Code: \u001b[31m404\u001b[0m. File {requestParams["Path"]} not found");
			}		
		}

		private ContentType? GetFileContentType(Dictionary<string, string> requestParams)
		{
			ContentType? result = null;

			result = new ContentType(); 

			if (IsDocumentHTML(requestParams["Path"]))
			{
				result.Path = requestParams["Path"].IndexOf("html") == -1 ? requestParams["Path"] + ".html" : requestParams["Path"];
				result.Extension = ".html";
				result.IsText = true;
				result.Mime = MimeTypes.ContainsKey(".html") == true ? MimeTypes[".html"] : "text/html";
			}
			else
			{
				result.Path = requestParams["Path"];
				result.Extension = requestParams["Path"].Substring(requestParams["Path"].IndexOf('.'));
				result.IsText = binaryExtensions.Contains(result.Extension) == false ? true : false;
				result.Mime = MimeTypes.ContainsKey(result.Extension) == true ? MimeTypes[result.Extension] : "Unsupported mime-type";
			}

			if (!File.Exists(WebFolder + requestParams["Path"]))
			{
				return null;
			}

			return result;
		}

		private bool IsDocumentHTML(string path)
		{
			return path.IndexOf("html") != -1 || path.IndexOf('.') == -1 ? true : false;
		}

		private Dictionary<string, string> Parse(string request)
		{
			var requestLineEnd = request.IndexOf("\r\n"); 
			var requestLine = request.Substring(0, requestLineEnd).Split(" ");

			var result = new Dictionary<string, string>
			{
				{ "Method", requestLine[0] },
				{ "Path", requestLine[1] == "/" ? "/index.html" : requestLine[1] },
				{ "Protocol", requestLine[2] }
			};

			foreach (string part in request.Substring(requestLineEnd + 2).Split("\r\n"))
			{
				if (part.Length > 0)
				{
					result.Add(part.Substring(0, part.IndexOf(":")), part.Substring(part.IndexOf(":") + 2).Trim());
				}	
			}

			return result;
		}
	}
}
