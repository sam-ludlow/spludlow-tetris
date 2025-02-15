using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Configuration;

using Spludlow.Tetris;

namespace Spludlow.Tetris
{
	public class TetrisSettings : IDisposable
	{
		private FileStream FileStream = null;

		public class TetrisSettingsState
		{
			public string Command;

			public int WindowWidth;
			public int WindowHeight;

			public int WindowX;
			public int WindowY;

			public string Keys;

			public int BotSpeed;
			public int BotFarmCount;

			public string ClientDisplayName { get; set; }

			public string ClientAddress { get; set; }

			public bool NoServerSounds { get; set; }
			public int Volume { get; set; }

			public int InputIndex { get; set; }

			public string ServerAddress { get; set; }

			public int ServerTick { get; set; }
			public int ServerWidth { get; set; }
			public int ServerHeight { get; set; }
		}

		public TetrisSettingsState State;

		public TetrisSettings()
		{
			string directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SpludlowV1\SpludlowTetris";

			if (Directory.Exists(directory) == false)
				Directory.CreateDirectory(directory);

			for (int processId = 0; this.FileStream == null; ++processId)
			{
				string filename = directory + @"\TetrisSettingsState-" + processId.ToString("0000") + ".xml";

				this.FileStream = this.Open(filename);
			}
			
			if (this.FileStream.Length == 0)
			{
				this.State = new TetrisSettingsState();

				//	Default Values
				this.State.WindowWidth = 800;
				this.State.WindowHeight = 600;

				this.State.WindowX = -1;
				this.State.WindowY = -1;

				this.State.Keys = "J, N, S, D, K, L, A";

				this.State.BotSpeed = 250;
				this.State.BotFarmCount = 2;

				this.State.ClientDisplayName = Tools.RandomString(8);

				this.State.ClientAddress = "127.0.0.1";

				this.State.NoServerSounds = ! TetrisClient.OnlyProcess();
				this.State.Volume = 2;

				this.State.InputIndex = 0;

				this.State.ServerAddress = "127.0.0.1";

				this.State.ServerTick = 1000;
				this.State.ServerWidth = 10;
				this.State.ServerHeight = 22;

				//	app.config override default values
				string value;

				value = ConfigurationManager.AppSettings["SpludlowTetris.WindowSize"];
				if (value != null && (value = value.Trim()).Length > 0)
				{
					string[] parts = value.Split(new char[] { ',' });
					this.State.WindowWidth = Int32.Parse(parts[0].Trim());
					this.State.WindowHeight = Int32.Parse(parts[1].Trim());
				}

				value = ConfigurationManager.AppSettings["SpludlowTetris.ClientAddress"];
				if (value != null && (value = value.Trim()).Length > 0)
					this.State.ClientAddress = value;

				value = ConfigurationManager.AppSettings["SpludlowTetris.BotSpeed"];
				if (value != null && (value = value.Trim()).Length > 0)
					this.State.BotSpeed = Int32.Parse(value);

				value = ConfigurationManager.AppSettings["SpludlowTetris.BotFarmCount"];
				if (value != null && (value = value.Trim()).Length > 0)
					this.State.BotFarmCount = Int32.Parse(value);

				value = ConfigurationManager.AppSettings["SpludlowTetris.InputIndex"];
				if (value != null && (value = value.Trim()).Length > 0)
					this.State.InputIndex = Int32.Parse(value);

				//	Validation

				int joyStickCount = TetrisInput.DeviceInstancesText().Length;
				if (this.State.InputIndex == -1)
					this.State.InputIndex = joyStickCount;
				if (this.State.InputIndex > joyStickCount)
					this.State.InputIndex = 0;

				Spludlow.Serialization.Write(this.State, this.FileStream);
			}
			else
			{
				this.State = (TetrisSettingsState)Spludlow.Serialization.Read(this.FileStream, typeof(TetrisSettingsState));
			}
		}

		private FileStream Open(string filename)
		{
			try
			{
				return new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException ee)
			{
				if ((uint)ee.HResult != 0x80070020)
					throw ee;

				return null;
			}
		}

		public void Dispose()
		{
			if (this.FileStream != null)
			{
				this.FileStream.SetLength(0);

				Spludlow.Serialization.Write(this.State, this.FileStream);

				this.FileStream.Close();
				this.FileStream = null;
			}
		}
	}
}
