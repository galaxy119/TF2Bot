using System;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Newtonsoft.Json;

namespace TF2Bot
{
	public class Program
	{
		private readonly Bot bot;
		private static readonly string kCfgFile = "TF2BotConfig.json";

		private static Config _config;
		public Config Config => _config ?? (_config = GetConfig());

		public static void Main(string[] args)
		{
			new Program();
		}

		private Program() => new Bot(this);

		public static Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		public static Config GetConfig()
		{
			if (File.Exists(kCfgFile))
				return JsonConvert.DeserializeObject<Config>(File.ReadAllText(kCfgFile));
			File.WriteAllText(kCfgFile, JsonConvert.SerializeObject(Config.Default, Formatting.Indented));
			return Config.Default;
		}
	}

	public class Config
	{
		public string BotPrefix { get; set; }
		public string BotToken { get; set; }
		public string Address { get; set; }
		public string Port { get; set; }
		public string Hostname { get; set; }
		public string RconPort { get; set; }
		public string RconPassword { get; set; }

		public static readonly Config Default = new Config
		{
			BotPrefix = "!", 
			BotToken = "",
			Address = "",
			Port = "",
			Hostname = "",
			RconPort = "",
			RconPassword = ""
		};
	}
}