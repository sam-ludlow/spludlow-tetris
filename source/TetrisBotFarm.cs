using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Threading;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Create Tetris Clients and then Tetris Bots playing on them
	/// </summary>
	public class TetrisBotFarm
	{
		private static string[] BotNames = null;

		static TetrisBotFarm()
		{
			string namesFilename = Environment.CurrentDirectory + @"\BotNames.txt";

			if (File.Exists(namesFilename) == true)
				BotNames = File.ReadAllLines(namesFilename);

			if (BotNames == null)
			{
				try
				{
					if (Directory.Exists(@"C:\ProgramData\SpludlowV1\Data") == true)
					{
						int length = 4096;
						BotNames = new string[length];
						Random random = new Random();
						for (int i = 0; i < length; ++i)
							BotNames[i] = Tools.RandomString(random, 8);
					}
				}
				catch (Exception ee)
				{
					Spludlow.Log.Error("Tetris Bot Farm, Bot Names", ee);

					BotNames = null;
				}
			}

			if (BotNames == null)
				BotNames = new string[] { "Fred", "Jim", "Sheila" };
		}

		private TetrisClient[] TetrisClients = null;
		private TetrisBot[] TetrisBots = null;

		public Task[] Start(string address, int botCount, int ms)
		{
			Spludlow.Log.Info("Tetris Bot Farm, Starting, Bot Count: " + botCount);

			Task[] clientTasks = new Task[botCount];

			TetrisClients = new TetrisClient[botCount];

			for (int botIndex = 0; botIndex < botCount; ++botIndex)
			{
				TetrisClient tetrisClient = new TetrisClient();
				TetrisClients[botIndex] = tetrisClient;

				string name = BotNames[botIndex % BotNames.Length];

				clientTasks[botIndex] = new Task(() => RunClient(tetrisClient, address, name));
				clientTasks[botIndex].Start();
			}

			Spludlow.Log.Info("Tetris Bot Farm, Started clients");

			TetrisBots = new TetrisBot[botCount];

			for (int botIndex = 0; botIndex < botCount; ++botIndex)
			{
				TetrisBot tetrisBot = new TetrisBot();
				TetrisBots[botIndex] = tetrisBot;
				tetrisBot.Start(TetrisClients[botIndex], ms);
			}

			Spludlow.Log.Info("Tetris Bot Farm, Started");

			return clientTasks;
		}

		public void Run(string address, int botCount, int ms)
		{
			System.Threading.Thread.Sleep(5 * 1000);	//	When running on same service allow server to start

			Task[] clientTasks = Start(address, botCount, ms);

			Task.WaitAll(clientTasks);

			Spludlow.Log.Info("Tetris Bot Farm, Finished waiting for client Tasks");
		}

		public bool Running
		{
			get
			{
				return !(TetrisClients == null);
			}
		}

		public void Stop()
		{
			if (this.Running == false)
				return;

			for (int botIndex = 0; botIndex < TetrisBots.Length; ++botIndex)
			{
				TetrisBots[botIndex].Stop();
			}

			Spludlow.Log.Info("Tetris Bot Farm, Stopped bots");

			for (int botIndex = 0; botIndex < TetrisClients.Length; ++botIndex)
			{
				TetrisClients[botIndex].Stop();
			}

			TetrisClients = null;
			TetrisBots = null;

			Spludlow.Log.Finish("Tetris Bot Farm, Finished");
		}

		private void RunClient(TetrisClient tetrisClient, string address, string name)
		{
			try
			{
				tetrisClient.Run(address, name);
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Bot Farm, Client Run", ee);
			}
		}

		public static void MakeBotNames(string outputFilename, int count)
		{
			HashSet<string> names = new HashSet<string>();

			Random random = new Random();

			while (names.Count < count)
			{
				string name = Tools.RandomString(random, 8);

				if (names.Contains(name) == false)
					names.Add(name);
			}

			StringBuilder text = new StringBuilder();

			foreach (string name in names)
				text.AppendLine(name);

			File.WriteAllText(outputFilename, text.ToString());
		}
	}
}
