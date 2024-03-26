using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
	internal class HttpServer
	{
		private ManualResetEvent signal;
		private TcpListener listener;
		private const string WebFolder = "web";
		string[] binaryExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mp3", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".otf", ".ttf", ".woff", ".woff2", ".zip", ".rar" };
		Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
        {
			{ ".pdf", "application/pdf" },
            { ".css", "text/css" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".mp4", "video/mp4" },
            { ".mp3", "audio/mpeg" },
            { ".txt", "text/plain" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".csv", "text/csv" },
            { ".rar", "application/x-rar-compressed" },
            { ".xml", "application/xml" },
            { ".json", "application/json" },
            { ".zip", "application/zip" }
        };

		public HttpServer(IPAddress ip, int port, ManualResetEvent signal) 
		{
			listener = new TcpListener(ip, port);
			this.signal = signal;
		}
		public void Listen() {
			listener.Start();

			do
			{
				var client = listener.AcceptTcpClient();
				Task.Run(() => HandleHttpsRequest(client));
				Thread.Sleep(50);
			}
			while (!signal.WaitOne(0));

			listener.Stop();
		}

		private int HandleHttpsRequest(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			var buffer = new byte[1024 * 4];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			if (request.Length != 0)
			{
				var requestParams = Parse(request);
				SendResponse(requestParams, stream);
			}

			client.Close();

			return 0;
		}

		private void SendTextResponse(NetworkStream stream, StringBuilder response)
		{
			var responseBuffer = Encoding.UTF8.GetBytes(response.ToString());
			stream.Write(responseBuffer, 0, responseBuffer.Length);
			stream.Flush();
		}

		private void SendBinaryResponse(NetworkStream stream, byte[] binary)
		{
			stream.Write(binary, 0, binary.Length);
			stream.Flush();
		}

		private StringBuilder SetHTMLResponse(StringBuilder sb, Dictionary<string, string> requestParams, ContentType contentType)
		{
			var fileContent = File.ReadAllText("web/" + requestParams["Path"]);
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

		private void SendResponse(Dictionary<string, string> requestParams, NetworkStream stream)
		{
			StringBuilder sb = new StringBuilder();

			if (!Directory.Exists("web"))
			{
				return;
			}
			
			var fileContentType = GetFileContentType(requestParams);

			if (fileContentType != null)
			{
				if (fileContentType.IsText)
				{
					SetHTMLResponse(sb, requestParams, fileContentType);
					SendTextResponse(stream, sb);
				}
				else
				{
					byte[] binary = File.ReadAllBytes(WebFolder + requestParams["Path"]);
					SetImageRequestHeader(sb, requestParams, binary.Length, fileContentType);
					SendTextResponse(stream, sb);
					SendBinaryResponse(stream, binary);
				}
			}
			else
			{
				NotFound(sb);
				SendTextResponse(stream, sb);
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
