using System.Net;

namespace WebServer
{
	public class Program
	{
		private static readonly ManualResetEvent stopSignal = new ManualResetEvent(false);
		private static readonly IPAddress ip = IPAddress.Any;
		private static readonly int httpPort  = 80;
		private static readonly string WebFolder = "web";

		static void Main()
		{
			var logger = new ConsoleInformer();
			logger.WelcomeMessage();

			var httpServer = new HttpServer(ip, httpPort, stopSignal, WebFolder, logger);
			var httpHandlerThread = new Thread(new ThreadStart(httpServer.Listen));
			httpHandlerThread.Start();

			ConsoleKey key;

			do
			{
				key = Console.ReadKey(false).Key;
			} while (key != ConsoleKey.Q);

			stopSignal.Set();
			httpHandlerThread.Join();
		}
	}
}