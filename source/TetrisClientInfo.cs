using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Information sent from the server to clients
	/// </summary>
	public class TetrisClientInfo
	{
		public int ClientId;
		public string DisplayName = "";

		public int TargetClientId;
		public string TargetDisplayName = "";

		public int Width;
		public int Height;

		public TetrisClientInfo()
		{

		}
		public TetrisClientInfo(byte[] data)
		{
			this.Deserialize(data);
		}

		public byte[] Serialize()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream, TetrisServer.Encoding))
				{
					writer.Write(this.ClientId);
					writer.Write(this.DisplayName);
					writer.Write(this.TargetClientId);
					writer.Write(this.TargetDisplayName);
					writer.Write(this.Width);
					writer.Write(this.Height);
				}
				return stream.ToArray();
			}

		}

		public void Deserialize(byte[] data)
		{
			using (MemoryStream stream = new MemoryStream(data))
			{
				using (BinaryReader reader = new BinaryReader(stream, TetrisServer.Encoding))
				{
					this.ClientId = reader.ReadInt32();
					this.DisplayName = reader.ReadString();
					this.TargetClientId = reader.ReadInt32();
					this.TargetDisplayName = reader.ReadString();
					this.Width = reader.ReadInt32();
					this.Height = reader.ReadInt32();
				}
			}
		}
	}
}
