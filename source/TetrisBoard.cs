using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Spludlow.Tetris
{
	/// <summary>
	/// The board is storing each space in a nibble (4 bit integer, range 0-15, 0x0-0xF)
	/// So you get 2 spaces in one byte. this is to give the most efficent board
	/// </summary>
	public class TetrisBoard
	{
		public int BoardKey;    //	-1:next peice, 0:player board +ve target board MaxValue:dont store, used for blank

		public bool ReDraw;

		public int Width;
		public int Height;
		public byte[] Board;

		public TetrisBoard(byte[] data)
		{
			this.Deserialize(data);
		}
		public TetrisBoard(int width, int height)
		{
			this.Width = width;
			this.Height = height;

			this.Board = new byte[this.BoardLength()];
		}

		private int BoardLength()
		{
			int size = this.Width * this.Height;
			int length = size / 2;
			if (size % 2 != 0)  // odd numbers need another nibble
				++length;

			return length;
		}

		public void Poke(int x, int y, byte value)
		{
			int index = (x % this.Width) + y * this.Width;

			bool lowNibble = (index % 2) == 0;
			index /= 2;

			int data = this.Board[index];

			if (lowNibble == true)
				data = (data & 0xF0) | value;
			else
				data = (data & 0x0F) | (value << 4);

			this.Board[index] = (byte)data;
		}

		public byte Peek(int x, int y)
		{
			int index = (x % this.Width) + y * this.Width;

			bool lowNibble = (index % 2) == 0;
			index /= 2;

			int data = this.Board[index];

			if (lowNibble == true)
				data = (data & 0x0F);
			else
				data = (data & 0xF0) >> 4;

			return (byte)data;
		}

		public static TetrisBoard Merge(TetrisBoard board, int x, int y, TetrisBoard shape, bool active)
		{
			TetrisBoard target = new TetrisBoard(board.Width, board.Height);

			Buffer.BlockCopy(board.Board, 0, target.Board, 0, board.Board.Length);

			for (int yShape = 0; yShape < 4; ++yShape)
			{
				for (int xShape = 0; xShape < 4; ++xShape)
				{
					byte value = shape.Peek(xShape, yShape);
					if (value != 0)
					{
						if (active == true)
							value += 8;

						target.Poke(x + xShape, y + yShape, value);
					}

				}
			}

			return target;
		}

		public TetrisBoard Copy()
		{
			TetrisBoard target = new TetrisBoard(this.Width, this.Height);
			Buffer.BlockCopy(this.Board, 0, target.Board, 0, this.Board.Length);
			return target;
		}

		public byte[] Serialize()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream, TetrisServer.Encoding))
				{
					writer.Write(this.BoardKey);
					writer.Write(this.ReDraw);
					writer.Write(this.Width);
					writer.Write(this.Height);
					writer.Write(this.Board);
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
					this.BoardKey = reader.ReadInt32();
					this.ReDraw = reader.ReadBoolean();
					this.Width = reader.ReadInt32();
					this.Height = reader.ReadInt32();

					this.Board = reader.ReadBytes(this.BoardLength());
				}
			}
		}
	}
}
