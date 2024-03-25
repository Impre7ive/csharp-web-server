using System;
using System.Collections.Generic;
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
		//private List<Task> tasks = new List<Task>();

		public HttpServer(IPAddress ip, int port, ManualResetEvent signal) { 
			listener = new TcpListener(ip, port);
			this.signal = signal;
		}
		public void Listen() {
			listener.Start();

			do
			{
				Console.WriteLine("Iter");
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
			var buffer = new byte[1024];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			//todo: empty request 
			var requestParams = Parse(request);
			var response = getResponse(requestParams, stream);

			sendResponse(stream, response);
			client.Close();

			return 0;
		}

		private void sendResponse(NetworkStream stream, string response)
		{
			var responseBuffer = Encoding.UTF8.GetBytes(response);
			stream.Write(responseBuffer, 0, responseBuffer.Length);
		}

		private string getResponse(Dictionary<string, string> requestParams, NetworkStream stream)
		{
			StringBuilder sb = new StringBuilder();

			if (Directory.Exists("web"))
			{
				if (File.Exists("web" + requestParams["Path"]))
				{
					if (requestParams["Sec-Fetch-Dest"] == "document")
					{
						var fileContent = File.ReadAllText("web/" + requestParams["Path"]);
						sb.Append($"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n");
						sb.Append(fileContent);
					}
					else if (requestParams["Sec-Fetch-Dest"] == "image")
					{
						byte[] fileData = File.ReadAllBytes("web/" + requestParams["Path"]);

						sb.Append("HTTP/1.1 200 OK\r\n");
						sb.Append("Content-Type: image/png\r\n"); 
						sb.Append($"Content-Length: {fileData.Length}\r\n");
						sb.Append("\r\n"); // End of headers

						byte[] h = Encoding.ASCII.GetBytes(sb.ToString());

						stream.Write(h, 0, h.Length);
						stream.Flush();
						stream.Write(fileData, 0, fileData.Length);
						stream.Flush();
					}
				}
				else
				{
					sb.Append("HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\n\r\n");
					sb.Append("<html><body>404 Not Found</body></html>");
				}
			}

			return sb.ToString();
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
