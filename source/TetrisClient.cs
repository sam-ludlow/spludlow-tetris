using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Client class used by the UI or Bot that handles server communtication
	/// The UI should handle the 4 events and can call the Move methods
	/// The Boards are cached here. Normally a UI can just handle the DrawBoardEvent but the Boards can be accessed directly like with the Bot
	/// </summary>
	public class TetrisClient
	{
		private System.Threading.EventWaitHandle _WaitFinished = null;

		private Socket _ServerSocket = null;

		public object InfoLock = new object();
		public TetrisClientInfo Info = null;

		public object BoardLock = new object();
		public Dictionary<int, TetrisBoard> Boards = new Dictionary<int, TetrisBoard>();
		public Dictionary<int, TetrisBoard> PreviousBoards = new Dictionary<int, TetrisBoard>();

		public delegate void DrawBoard(int boardKey, TetrisBoard board, TetrisBoard previousBoard, bool reDraw);
		public event DrawBoard DrawBoardEvent;

		public delegate void ReceiveText(string text);
		public event ReceiveText ReceiveTextEvent;

		public delegate void ReceiveInfo(TetrisClientInfo info);
		public event ReceiveInfo ReceiveInfoEvent;

		public delegate void ReceiveSound(string sound);
		public event ReceiveSound ReceiveSoundEvent;

		public class ClientState
		{
			public ClientState()
			{
				this.ClientDisplayName = Tools.RandomString(8);
				this.HostOrAddressAndPort = "127.0.0.1";
				this.NoServerSounds = !OnlyProcess();
				this.Volume = 2;    //10;
				this.InputIndex = 0;
				this.ServerTick = 1000;
				this.ServerWidth = 10;
				this.ServerHeight = 22;

			}

			public string ClientDisplayName { get; set; }

			public string HostOrAddressAndPort { get; set; }

			public bool NoServerSounds { get; set; }
			public int Volume { get; set; }

			public int InputIndex { get; set; }

			public int ServerTick { get; set; }
			public int ServerWidth { get; set; }
			public int ServerHeight { get; set; }

			public bool ClientRunning { get; set; }
			public bool ServerRuning { get; set; }

			public string Command { get; set; }
		}

		public static bool OnlyProcess()
		{
			return (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length == 1);
		}

		public void Run(string hostNameOrAddressAndPortNumber, string displayName)
		{
			_WaitFinished = new EventWaitHandle(false, EventResetMode.ManualReset);

			IPEndPoint serverEndPoint = TetrisServer.CreateEndpoint(hostNameOrAddressAndPortNumber);

			using (_ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
			{
				_ServerSocket.Connect(serverEndPoint);

				Spludlow.Log.Info("Tetris Client, Connected to server");

				TetrisMessage message;

				//	Send Display Name
				message = new TetrisMessage(TetrisMessage.BodyTypes.Name, TetrisServer.Encoding.GetBytes(displayName));
				message.SocketSend(_ServerSocket);

				while (true)
				{
					try
					{
						message = TetrisMessage.Receive(_ServerSocket, _WaitFinished);

						if (message == null)
							break;
					}
					catch (SocketException ee)
					{
						if (ee.ErrorCode == 10053 || ee.ErrorCode == 10054 || ee.ErrorCode == 10060)
						{
							Spludlow.Log.Info("Tetris Client, Closed from Server" + ee.ErrorCode);
							break;
						}

						Spludlow.Log.Error("Tetris Client, Receive SocketException: " + ee.ErrorCode, ee);
						throw ee;
					}

					switch ((TetrisMessage.BodyTypes)message.BodyType)
					{
						case TetrisMessage.BodyTypes.Text:
							string text = TetrisServer.Encoding.GetString(message.Body);

							if (this.ReceiveTextEvent != null)
								this.ReceiveTextEvent(text);
							break;

						case TetrisMessage.BodyTypes.Board:
							TetrisBoard board = new TetrisBoard(message.Body);
							TetrisBoard previousBoard = null;

							if (board.BoardKey != Int32.MaxValue)
							{
								lock (this.BoardLock)
								{
									if (this.Boards.ContainsKey(board.BoardKey) == true)
									{
										if (this.PreviousBoards.ContainsKey(board.BoardKey) == true)
											this.PreviousBoards[board.BoardKey] = this.Boards[board.BoardKey];
										else
											this.PreviousBoards.Add(board.BoardKey, this.Boards[board.BoardKey]);
									}

									if (this.Boards.ContainsKey(board.BoardKey) == true)
										this.Boards[board.BoardKey] = board;
									else
										this.Boards.Add(board.BoardKey, board);

									if (this.PreviousBoards.ContainsKey(board.BoardKey) == true)
										previousBoard = this.PreviousBoards[board.BoardKey];
								}
							}

							if (this.DrawBoardEvent != null)
								this.DrawBoardEvent(board.BoardKey, board, previousBoard, board.ReDraw);
							break;

						case TetrisMessage.BodyTypes.Sound:
							string sound = TetrisServer.Encoding.GetString(message.Body);
							if (this.ReceiveSoundEvent != null)
								this.ReceiveSoundEvent(sound);
							break;

						case TetrisMessage.BodyTypes.Info:
							lock (this.InfoLock)	//	?? need
							{
								this.Info = new TetrisClientInfo(message.Body);
								if (this.ReceiveInfoEvent != null)
									this.ReceiveInfoEvent(this.Info);
							}
							break;

					}
				}
			}

			Spludlow.Log.Info("Tetris Client, Disconnected");
		}

		public void Stop()
		{
			if (_WaitFinished != null)
				_WaitFinished.Set();

			Spludlow.Log.Info("Tetris Client, Asked to Stop");
		}

		public void ReDrawBoards()
		{
			lock (this.BoardLock)
			{
				foreach (int boardKey in this.Boards.Keys)
				{
					if (boardKey <= 0 || boardKey == this.Info.TargetClientId)
					{
						TetrisBoard[] boards = GetBoards(boardKey);
						this.DrawBoardEvent(boardKey, boards[0], boards[1], true);
					}
				}
			}
		}

		public TetrisBoard[] GetBoards(int boardKey)
		{
			TetrisBoard board = null;
			if (this.Boards.ContainsKey(boardKey) == true)
				board = this.Boards[boardKey];

			TetrisBoard previousBoard = null;
			if (this.PreviousBoards.ContainsKey(boardKey) == true)
				previousBoard = this.PreviousBoards[boardKey];

			return new TetrisBoard[] { board, previousBoard };
		}


		public void Up()
		{
			Move(0);
		}

		public void Down()
		{
			Move(1);
		}

		public void Left()
		{
			Move(2);
		}

		public void Right()
		{
			Move(3);
		}

		public void RotateCW()
		{
			Move(4);
		}

		public void RotateCCW()
		{
			Move(5);
		}

		public void TargetNext()
		{
			Move(6);
		}

		public void Move(byte command)
		{
			byte[] data = new byte[] { command };
			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Move, data);
			message.SocketSend(_ServerSocket);
		}

		public void SendText(string text)
		{
			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Text, TetrisServer.Encoding.GetBytes(text));
			message.SocketSend(_ServerSocket);
		}
	}
}
