using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer
{
	public class HttpServer
	{
		private const int requestBufferSize = 1024 * 4;
		private readonly RequestParser parser = new RequestParser();
		private readonly ResponseGenerator responseGenerator = new ResponseGenerator();
		private readonly ManualResetEvent signal;
		private readonly TcpListener listener;
		private readonly IInformer logger;
		private readonly string WebFolder;

		public HttpServer(
			IPAddress ip, 
			int port, 
			ManualResetEvent signal, 
			string WebFolder, 
			IInformer logger) 
		{
			listener = new TcpListener(ip, port);
			this.signal = signal;
			this.WebFolder = WebFolder;
			this.logger = logger;

			logger.ShowMessage($"Server socket - {ip}:{port}");
			logger.ShowMessage($"Server www directory - /{WebFolder}");
			logger.ShowMessage($"Found {parser.MimeTypes.Count} supported types - {string.Join(", ", parser.MimeTypes.Keys.Select(k => k.ToString()))}");
		}

		public void Listen() {
			listener.Start();

			do
			{
				var client = listener.AcceptTcpClient();
				Task.Run(() => HandleHttpRequestAsync(client));
				Thread.Sleep(50);
			}
			while (!signal.WaitOne(0));

			listener.Stop();
		}

		private async Task HandleHttpRequestAsync(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			var request = await GetRequestMessage(stream);

			if (request.Length != 0)
			{
				var requestParams = parser.Parse(request);
				await SendResponseAsync(requestParams, stream);
			}

			client.Close();
		}

		private static async Task<string> GetRequestMessage(NetworkStream stream)
		{
			var buffer = new byte[requestBufferSize];
			var bytesRead = await stream.ReadAsync(buffer);
			var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			return request;
		}

		private async Task SendResponseAsync(Dictionary<string, string> requestParams, NetworkStream stream)
		{
			var sb = new StringBuilder();

			if (!Directory.Exists("web"))
			{
				return;
			}
			
			var fileContentType = parser.GetFileContentType(requestParams, WebFolder);

			logger.ShowMessage($"Requested file with path - {requestParams["Path"]}");

			if (fileContentType != null)
			{
				if (fileContentType.IsText)
				{
					await responseGenerator.SetHTMLResponseAsync(sb, requestParams, fileContentType);
					await SendTextResponseAsync(stream, sb);
				}
				else
				{
					var binary = GetBinaryFile(requestParams);
					responseGenerator.SetImageRequestHeader(sb, binary.Length, fileContentType);
					await SendTextResponseAsync(stream, sb);
					await SendBinaryResponseAsync(stream, binary);
				}

				logger.ShowMessage($"Code: \x1b[32m200\x1b[0m. File {fileContentType.Path.Substring(1)} has been sent successfully");
			}
			else
			{
				responseGenerator.NotFound(sb);
				await SendTextResponseAsync(stream, sb);
				logger.ShowMessage($"Code: \u001b[31m404\u001b[0m. File {requestParams["Path"]} not found");
			}		
		}

		private byte[] GetBinaryFile(Dictionary<string, string> requestParams)
		{
			return File.ReadAllBytes(WebFolder + requestParams["Path"]);
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
	}
}
