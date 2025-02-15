using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Container for all Tetris network comminication
	/// </summary>
	public class TetrisMessage
	{
		public ushort MagicNumber;	//	Confirm the data is what we are expecting
		public byte BodyType;		//	What type of data
		public int BodyLength;		//	Data Size

		public byte[] Body;			//	the Data

		public enum BodyTypes : byte
		{
			Text,		//	UTF-8 Text sent from server to all clients
			Board,		//	TetrisBoard from server to clients, binary encoding see constructor
			Move,		//	One byte from client to server
			Name,		//	UTF-8 Text sent from client to server with required display name
			Sound,		//	UTF-8 Text of sound name to play
			Info,       //	TetrisInfo from server to clients, binary encoding see constructor
		};

		public TetrisMessage()
		{

		}

		/// <summary>
		/// Create New
		/// </summary>
		public TetrisMessage(BodyTypes type, byte[] data)
		{
			this.MagicNumber = 0x7E75;	//	TETriS in 2 bytes
			this.BodyType = (byte)type;
			this.BodyLength = data.Length;

			this.Body = data;
		}

		/// <summary>
		/// Receive from socket, will block or return null if waitFinished gets set
		/// Calling code should deal with SocketException
		/// </summary>
		public static TetrisMessage Receive(Socket socket, System.Threading.EventWaitHandle waitFinished)
		{
			byte[] buffer = new byte[2];

			//	Start with async Receive, so it can block if nothing to do
			IAsyncResult aSyncResult = socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null);

			//	Wait for data OR stop signal
			int index = WaitHandle.WaitAny(new WaitHandle[] { waitFinished, aSyncResult.AsyncWaitHandle });

			//	Got Stop signal, abort
			if (index == 0)
				return null;

			int left;

			//	End the async Receive
			try
			{
				left = buffer.Length - socket.EndReceive(aSyncResult);
			}
			catch (ObjectDisposedException)
			{
				//	Socket Disposed elsewhere, abort
				Spludlow.Log.Warning("Tetris TetrisMessage, EndReceive");
				return null;
			}

			//	Using normal (not aync) socket Receive for rest of message 

			//	Finish first read (if neccerassy)
			ReadBuffer(buffer, socket, left);

			TetrisMessage message = new TetrisMessage();

			message.MagicNumber = BitConverter.ToUInt16(buffer, 0);
			if (message.MagicNumber != 0x7E75)
				throw new ApplicationException("Tetris Message, Receive; Bad Magic Number");

			buffer = new byte[1];
			ReadBuffer(buffer, socket, buffer.Length);
			message.BodyType = buffer[0];

			buffer = new byte[4];
			ReadBuffer(buffer, socket, buffer.Length);
			message.BodyLength = BitConverter.ToInt32(buffer, 0);

			message.Body = new byte[message.BodyLength];
			ReadBuffer(message.Body, socket, message.BodyLength);

			return message;

		}

		/// <summary>
		/// Because socket Receive is non blocking (they can return even if they havent got all the data yet)
		/// this method will keep calling recieve until the buffer has what is requested
		/// Normally you pass "remaining" as buffer.Length, with an async receive you may have already read some of the buffer
		/// </summary>
		private static void ReadBuffer(byte[] buffer, Socket socket, int remaining)
		{
			while (remaining > 0)
			{
				remaining -= socket.Receive(buffer, buffer.Length - remaining, remaining, SocketFlags.None);
			}
		}

		/// <summary>
		/// Send over the wire
		/// </summary>
		public bool SocketSend(Socket socket)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(this.MagicNumber);
					writer.Write(this.BodyType);
					writer.Write(this.BodyLength);
					writer.Write(this.Body, 0, this.BodyLength);
				}

				try
				{
					socket.Send(stream.ToArray());

					return true;
				}
				catch (SocketException ee)
				{
					//	10053	An established connection was aborted by the software in your host machine
					//	10054	An existing connection was forcibly closed by the remote host
					//	10060	Timeout

					if (ee.ErrorCode == 10053 || ee.ErrorCode == 10054 || ee.ErrorCode == 10060)
					{
						return false;
					}

					throw ee;
				}
			}
		}

		//public static object Decode(byte[] data)
		//{
		//	using (MemoryStream stream = new MemoryStream())
		//	{
		//		BinaryFormatter formatter = new BinaryFormatter();

		//		stream.Write(data, 0, data.Length);
		//		stream.Seek(0, SeekOrigin.Begin);
		//		return formatter.Deserialize(stream);
		//	}

		//}
		//public static byte[] Encode(object obj)
		//{
		//	BinaryFormatter formatter = new BinaryFormatter();
		//	using (MemoryStream stream = new MemoryStream())
		//	{
		//		formatter.Serialize(stream, obj);
		//		return stream.ToArray();
		//	}
		//}
	}
}
