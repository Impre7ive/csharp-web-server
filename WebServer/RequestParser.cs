namespace WebServer
{
	public class RequestParser
	{
		public readonly string[] binaryExtensions = {
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
		public readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>
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

		public Dictionary<string, string> Parse(string request)
		{
			var requestLineEnd = request.IndexOf("\r\n");
			var requestLine = request[..requestLineEnd].Split(" ");

			var result = new Dictionary<string, string>
			{
				{ "Method", requestLine[0] },
				{ "Path", requestLine[1] == "/" ? "/index.html" : requestLine[1] },
				{ "Protocol", requestLine[2] }
			};

			foreach (string part in request[(requestLineEnd + 2)..].Split("\r\n"))
			{
				if (part.Length > 0)
				{
					result.Add(part[..part.IndexOf(":")], part[(part.IndexOf(":") + 2)..].Trim());
				}
			}

			return result;
		}

		public ContentType? GetFileContentType(Dictionary<string, string> requestParams, string webFolder)
		{
			var result = new ContentType();

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
				result.Extension = requestParams["Path"][requestParams["Path"].IndexOf('.')..];
				result.IsText = binaryExtensions.Contains(result.Extension) == false;
				result.Mime = MimeTypes.ContainsKey(result.Extension) == true ? MimeTypes[result.Extension] : "Unsupported mime-type";
			}

			if (!File.Exists(webFolder + requestParams["Path"]))
			{
				return null;
			}

			return result;
		}

		private bool IsDocumentHTML(string path)
		{
			return path.IndexOf("html") != -1 || path.IndexOf('.') == -1;
		}
	}
}
