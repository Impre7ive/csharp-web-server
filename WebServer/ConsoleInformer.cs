namespace WebServer
{
	public class ConsoleInformer : IInformer
	{
		public void ShowMessage(string message)
		{
			Console.WriteLine($"[{DateTime.Now}]: {message}.");
		}

		public void WelcomeMessage()
		{
			Console.WriteLine("Welcome to C# Web Server.");
			Console.WriteLine("Press Q to stop server...");
		}
	}
}
