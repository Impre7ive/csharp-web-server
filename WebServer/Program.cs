using System.Net;

namespace WebServer
{
	internal class Program
	{
		private static ManualResetEvent stopSignal = new ManualResetEvent(false);
		private static IPAddress ip = IPAddress.Any;
		private static int httpPort  = 80;
		private static string WebFolder = "web";

		static void Main(string[] args)
		{
			WelcomeMessage();

			var httpServer = new HttpServer(ip, httpPort, stopSignal, WebFolder);
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

		private static void WelcomeMessage()
		{
			Console.WriteLine("Welcome to C# Web Server.");
			Console.WriteLine("Press Q to stop server...");
		}
	}
}