using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Thread for listening for new connections
	/// Each player has a thread for recieving from the client
	/// Another thread provides the down tick
	/// </summary>
	public class TetrisServer
	{
		public static Encoding Encoding = Encoding.UTF8;

		private TetrisPlayers _TetrisPlayers = new TetrisPlayers();

		private System.Threading.EventWaitHandle _WaitFinished = null;

		public enum ServerModes { Creative, Standard, Battle, Levels };

		private ServerModes _ServerMode = ServerModes.Standard;
		//private bool CreativeMode = false;

		//	By using [ThreadStatic] every thread will get it's own Random object automatically
		[ThreadStatic]
		private static Random _Random;
		private static int RandomNext(int maxValue)
		{
			if (_Random == null)
			{
				_Random = new Random();
				Spludlow.Log.Info("new Random");
			}
				
			return _Random.Next(maxValue);
		}

		private TetrisShapes _TetrisShapes = new TetrisShapes();

		public int _Width = 0;
		public int _Height = 0;

		public void Stop()
		{
			if (_WaitFinished != null)
				_WaitFinished.Set();

			Spludlow.Log.Info("Tetris Server, Asked to Stop");
		}




		/// <summary>
		/// The main tetris server thread
		/// Starts the down tick timer
		/// Listens for incoming connections
		/// Uses async Accept so can also wait for stop signal with clean exit
		/// Initilises the player and runs a client thread for each connection
		/// This method will block (not exit) while the server is running
		/// </summary>
		public void Run(string hostNameOrAddressAndPortNumber, int width, int height, int milliseconds)
		{
			try
			{
				_Width = width;
				_Height = height;

				if (milliseconds == 0)
					this._ServerMode = ServerModes.Creative;

				_WaitFinished = new EventWaitHandle(false, EventResetMode.ManualReset);

				IPEndPoint serverEndPoint = CreateEndpoint(hostNameOrAddressAndPortNumber);

				//	Create the main server socket, used only or accepting incoming connections
				using (Socket listener = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
				{
					//	Start using the socket on specified address
					listener.Bind(serverEndPoint);

					//	Start listeding for clients
					listener.Listen(256);

					//	Run the tick thread
					if (this._ServerMode != ServerModes.Creative)
					{
						Thread tickThread = new Thread(() => this.TickThread(milliseconds));
						tickThread.Start();
					}

					Spludlow.Log.Info("Tetris Server, Listening");

					//	This loop will efectivly sleep until a client connects
					while (true)
					{
						//	begin an async Accept
						IAsyncResult aSyncResult = listener.BeginAccept(null, null);

						//	Wait for the finished flag being set or the Accept
						int index = WaitHandle.WaitAny(new WaitHandle[] { _WaitFinished, aSyncResult.AsyncWaitHandle });

						//	Clean exit if it was the finish flag
						if (index == 0)
							break;

						//	end the async Accept
						Socket clientSocket = null;

						clientSocket = listener.EndAccept(aSyncResult);

						string ipAddress = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();

						//	Set up a player object
						TetrisPlayer player = new TetrisPlayer();
						player.ClientSocket = clientSocket;

						player.Score = new TetrisPlayerScore();
						player.Score.TimeConnect = DateTime.Now;
						player.Score.TimeGameStart = DateTime.Now;

						//	Initilise player
						ResetPlayer(player, true);

						_TetrisPlayers.Add(player);

						//	Start the client thread
						Thread clientThread = new Thread(() => this.ClientThread(player));
						clientThread.Start();

						Spludlow.Log.Info("Tetris Server, Accepted Connection: " + player.ClientId + ", " + ipAddress);
					}

					Spludlow.Log.Info("Tetris Server, Stopping");

					foreach (TetrisPlayer player in this._TetrisPlayers.CurrentPlayers())
					{
						player.ClientSocket.Dispose();
						Spludlow.Log.Info("Tetris Server, Disposed Client Socket: " + player.ClientId);
					}
				}

				Spludlow.Log.Finish("Tetris Server, Stopped");
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Server, Fatal Error", ee);
				throw ee;
			}

		}

		/// <summary>
		/// Server's client thread one for each player
		/// Recieves Tetris messages from the clients and act acordingly, like perform client moves
		/// </summary>
		public void ClientThread(TetrisPlayer player)
		{
			try
			{
				while (player.Removed == false)
				{
					TetrisMessage message;

					try
					{
						message = TetrisMessage.Receive(player.ClientSocket, _WaitFinished);

						if (message == null)
						{
							Spludlow.Log.Finish("Tetris Server, Client Thread End, Asked to stop: " + player.ClientId);
							return;
						}
					}
					catch (SocketException ee)
					{
						if (ee.ErrorCode == 10053 || ee.ErrorCode == 10054 || ee.ErrorCode == 10060)
						{
							this.RemovePlayer(player.ClientId);

							Spludlow.Log.Finish("Tetris Server, Client Thread End, Lost connection: " + player.ClientId);
							return;
						}

						Spludlow.Log.Error("Tetris Server, Client Thread, Receive Socket Error: " + ee.ErrorCode + ", " + player.ClientId, ee);
						throw ee;
					}

					switch ((TetrisMessage.BodyTypes)message.BodyType)
					{
						case TetrisMessage.BodyTypes.Text:
							string clientText = TetrisServer.Encoding.GetString(message.Body);
							Spludlow.Log.Info("Tetris Server, Client Thread: Text from client: " + clientText);
							if (clientText.StartsWith("@") == true)
							{
								switch (clientText)
								{
									case "@BR":
										this.ToggleBattle();
										break;
								}
							}
							break;

						case TetrisMessage.BodyTypes.Move:
							Move(player, message.Body[0]);
							break;

						case TetrisMessage.BodyTypes.Name:
							player.DisplayName = TetrisServer.Encoding.GetString(message.Body);

							Spludlow.Log.Info("Tetris Server, Client Thread: Display Name: " + player.ClientId + ", " + player.DisplayName);

							this.SendText(player, "Server V " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

							this.SendText("\"" + player.DisplayName + "\" has joined, id:" + player.ClientId);

							this.SendSound("OPEN");

							break;
					}
				}

				Spludlow.Log.Finish("Tetris Server, Client Thread End, Player Removed Flag set: " + player.ClientId);
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Server, ClientThread, Fatal Error", ee);

				this.RemovePlayer(player.ClientId);
				//throw ee;
			}

		}

		public void ToggleBattle()
		{
			if (this._ServerMode != ServerModes.Battle)
			{
				this._ServerMode = ServerModes.Battle;

				foreach (TetrisPlayer player in this._TetrisPlayers.CurrentPlayers())
					this.ResetPlayer(player, true);

				this.SendText("BATTLE ROYALE STARTED");
			}

		}

		public void SocketSend(TetrisPlayer player, TetrisMessage message)
		{
			if (player.Removed == true)
				return;

			try
			{
				if (message.SocketSend(player.ClientSocket) == false)
					this.RemovePlayer(player.ClientId);
			}
			catch (ObjectDisposedException)
			{
				Spludlow.Log.Warning("Tetris TetrisServer, SocketSend: ObjectDisposedException");
				return;
			}

		}

		private void RemovePlayer(int clientId)
		{
			TetrisPlayer player = this._TetrisPlayers.Remove(clientId);
			if (player != null)
			{
				player.Removed = true;

				this.SendText("\"" + player.DisplayName + "\" has left");

				this.SendSound("CLOSE");

				player.ClientSocket.Dispose();

				Spludlow.Log.Info("Tetris Server, RemovePlayer: " + clientId);
			}
		}

		/// <summary>
		/// The .net timers did not work well under heavy testing. So rolled own timer thread
		/// </summary>
		private void TickThread(int milliseconds)
		{
			int waitTime = milliseconds;

			int warn = (int)((decimal)milliseconds * 0.5M);

			while (this._WaitFinished.WaitOne(waitTime) == false)
			{
				DateTime start = DateTime.Now;

				foreach (TetrisPlayer player in this._TetrisPlayers.CurrentPlayers())
					this.Move(player, 1);

				this.SendSound("TICK");

				int processingTime = (int)(DateTime.Now - start).TotalMilliseconds;

				waitTime = milliseconds - processingTime;

				if (waitTime < warn)
					Spludlow.Log.Warning("Tetris Server, Server Overload, Tick took : " + processingTime + " / " + milliseconds);

				if (waitTime < 0)
					waitTime = 0;
			}

			Spludlow.Log.Finish("Tetris Server, Tick Thread finished");
		}

		public void Move(TetrisPlayer player, byte command)
		{

			try
			{
				DateTime start = DateTime.Now;

				lock (player._PlayerLock)
				{
					this.MoveWork(player, command);
				}
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Server, Move Method: ", ee);
			}
		}
		public void MoveWork(TetrisPlayer player, byte command)
		{
			bool moved = false;

			switch (command)
			{
				case 0:     //	U
					if (this._ServerMode != ServerModes.Creative)
					{
						while (this.PositionValid(player, 0, 1, 0, true) == true)
						{
						}
						this.Merge(player, true);
					}
					else
					{
						moved = this.PositionValid(player, 0, -1, 0, true);
					}
					break;

				case 1:     //	D
					moved = this.PositionValid(player, 0, 1, 0, true);

					if (moved == false)
						this.Merge(player, true);
					break;

				case 2:     //	L
					moved = this.PositionValid(player, -1, 0, 0, true);
					break;

				case 3:     //	R
					moved = this.PositionValid(player, 1, 0, 0, true);
					break;

				case 4:     //	CW
					moved = Rotate(player, 1);
					if (moved == true)
						this.SendSound(player, "ROTATE");
					this.SendNextPeiceBoard(player);
					break;
				case 5:     //	CCW
					moved = Rotate(player, -1);
					if (moved == true)
						this.SendSound(player, "ROTATE");
					this.SendNextPeiceBoard(player);
					break;

				case 6:     //	Target Next
					player.TargetClientId = _TetrisPlayers.TargetNext(player.TargetClientId);

					this.SendInfo(player);

					this.SendTargetBoard(player);

					break;
			}

			if (moved == true)
				this.SendBoard(player);

		}

		/// <summary>
		/// Can shape be placed
		/// </summary>
		public bool PositionValid(TetrisPlayer player, int xMove, int yMove, int rotMove, bool performMove)
		{
			int x = player.X + xMove;
			int y = player.Y + yMove;
			int rot = player.Rotate + rotMove;
			if (rot > 3)
				rot = 0;
			if (rot < 0)
				rot = 3;

			TetrisBoard shape = _TetrisShapes.ShapeBoards[(player.Shape - 1) * 4 + rot];

			for (int shapeY = 0; shapeY < 4; ++shapeY)
			{
				for (int shapeX = 0; shapeX < 4; ++shapeX)
				{
					byte value = shape.Peek(shapeX, shapeY);
					if (value == 0)
						continue;

					int boardX = x + shapeX;
					int boardY = y + shapeY;

					if (boardX < 0 || boardY < 0)
						return false;

					if (boardX >= player.Board.Width || boardY >= player.Board.Height)
						return false;

					if (player.Board.Peek(boardX, boardY) != 0)
						return false;
				}
			}

			if (performMove == true)
			{
				player.X = x;
				player.Y = y;
				player.Rotate = rot;
			}

			return true;
		}


		/// <summary>
		/// Copy board, place current shape if required
		/// </summary>
		public TetrisBoard Merge(TetrisPlayer player, bool place)
		{
			TetrisBoard shape = _TetrisShapes.ShapeBoards[(player.Shape - 1) * 4 + player.Rotate];

			TetrisBoard board = TetrisBoard.Merge(player.Board, player.X, player.Y, shape, !place);
					   			 		  
			if (place == true)
			{
				player.Board = board;

				int lines = 0;

				if (this._ServerMode != ServerModes.Creative)
					lines = FindLines(player);

				int sendLines = lines - 1;

				bool sendSound = false;

				if (sendLines > 0)
				{
					if (player.TargetClientId > 0)
					{
						TetrisPlayer targetPlayer = this._TetrisPlayers.Get(player.TargetClientId);

						if (targetPlayer != null)
						{
							targetPlayer.IncomingRows += sendLines;

							this.SendText("\"" + player.DisplayName + "\" > " + new string('X', sendLines) + " > \"" + targetPlayer.DisplayName + "\"");

							for (int count = 0; count < sendLines; ++count)
								this.SendSound("SEND*");

							sendSound = true;
						}
						else
						{
							player.TargetClientId = 0;
						}
					}
				}

				if (lines > 0 && sendSound == false)
				{
					for (int count = 0; count < lines; ++count)
						this.SendSound(player, "LINE*");
				}

				this.SendSound(player, "DROP");

				ResetPlayer(player, false);
			}

			return board;
		}


		public void ResetPlayer(TetrisPlayer player, bool newBoard)
		{
			if (newBoard == true)
			{
				player.Board = new TetrisBoard(_Width, _Height);

				player.Level = 0;
			}

			if (player.NextShape == 0)
				player.NextShape = RandomNext(7) + 1;
			player.Shape = player.NextShape;
			player.NextShape = RandomNext(7) + 1;

			player.X = (player.Board.Width / 2) - 2;
			player.Y = 0;

			player.Rotate = 0;

			if (player.IncomingRows > 0)
			{
				for (int count = 0; count < player.IncomingRows; ++count)
					this.SendSound("RECEIVE*");

				this.InsertLines(player);
			}



			if (this.PositionValid(player, 0, 0, 0, false) == false)    //	Game over
			{
				player.Board = new TetrisBoard(_Width, _Height);

				++player.Score.CountGameOver;

				this.GameOverText(player);

				this.SendSound("OVER");

				player.Score.TimeGameStart = DateTime.Now;
			}

			this.SendInfo(player);

			this.SendNextPeiceBoard(player);

			this.SendBoard(player);
		}


		private void GameOverText(TetrisPlayer player)
		{
			StringBuilder text = new StringBuilder();

			string mainLine = "##   R.I.P. \"" + player.DisplayName + "\"   ##";
			string sepLine = new string('#', mainLine.Length);

			text.AppendLine(sepLine);
			text.AppendLine(mainLine);
			text.AppendLine(sepLine);
			text.AppendLine("  Alive for: " + Tools.TimeTook(player.Score.TimeGameStart));
			text.AppendLine("      Died " + player.Score.CountGameOver + " times.");
			text.Append(sepLine);


			this.SendText(text.ToString());
		}







		public void SendTargetBoard(TetrisPlayer player)
		{
			TetrisBoard board = new TetrisBoard(this._Width, this._Height);
			board.BoardKey = Int32.MaxValue;
			board.ReDraw = true;

			if (player.TargetClientId > 0)
			{
				TetrisPlayer testPlayer = this._TetrisPlayers.Get(player.TargetClientId);

				if (testPlayer != null)
				{
					board = this.Merge(testPlayer, false);
					board.BoardKey = player.TargetClientId;
					board.ReDraw = true;
				}
				else
				{
					//	player gone
				}
			}

			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Board, board.Serialize());
			SocketSend(player, message);
		}

		public void SendBoard(TetrisPlayer player)
		{
			List<TetrisPlayer> players = new List<TetrisPlayer>();

			players.Add(player);

			if (player.ClientId != 0)
			{
				foreach (TetrisPlayer testPlayer in this._TetrisPlayers.CurrentPlayers())
				{
					if (testPlayer.TargetClientId == player.ClientId)
						players.Add(testPlayer);
				}
			}
			
			TetrisBoard board = this.Merge(player, false);
			board.BoardKey = 0;

			for (int index = 0; index < players.Count; ++index)
			{
				TetrisPlayer sendPlayer = players[index];
				if (index == 1)
					board.BoardKey = player.ClientId;

				TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Board, board.Serialize());
				this.SocketSend(sendPlayer, message);
			}
		}

		public void SendNextPeiceBoard(TetrisPlayer player)
		{
			TetrisBoard nextShapeBoard = _TetrisShapes.ShapeBoards[(player.NextShape - 1) * 4 + player.Rotate].Copy();
			nextShapeBoard.BoardKey = -1;

			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Board, nextShapeBoard.Serialize());
			this.SocketSend(player, message);
		}

		public void SendSound(string sound)
		{
			sound = "!" + sound;

			TetrisPlayer[] players = this._TetrisPlayers.CurrentPlayers();

			foreach (TetrisPlayer player in players)
				this.SendSound(player, sound);
		}

		public void SendSound(TetrisPlayer player, string sound)
		{
			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Sound, TetrisServer.Encoding.GetBytes(sound));
			this.SocketSend(player, message);
		}

		public void SendText(string text)
		{
			TetrisPlayer[] players = this._TetrisPlayers.CurrentPlayers();

			foreach (TetrisPlayer player in players)
				this.SendText(player, text);
		}

		public void SendText(TetrisPlayer player, string text)
		{
			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Text, TetrisServer.Encoding.GetBytes(text));
			this.SocketSend(player, message);
		}

		public void SendInfo(TetrisPlayer player)
		{
			TetrisClientInfo info = new TetrisClientInfo();
			info.ClientId = player.ClientId;
			info.DisplayName = player.DisplayName;

			info.Width = this._Width;
			info.Height = this._Height;

			if (player.TargetClientId > 0)
			{
				TetrisPlayer targetPlayer = this._TetrisPlayers.Get(player.TargetClientId);

				if (targetPlayer != null)
				{
					info.TargetClientId = player.TargetClientId;
					info.TargetDisplayName = targetPlayer.DisplayName;
				}
				else
				{
					//	Gone
				}
			}

			if (info.DisplayName == null)
				info.DisplayName = "";
			if (info.TargetDisplayName == null)
				info.TargetDisplayName = "";

			TetrisMessage message = new TetrisMessage(TetrisMessage.BodyTypes.Info, info.Serialize());

			this.SocketSend(player, message);
		}







		//	Logic


		private bool Rotate(TetrisPlayer player, int rotateDir)
		{
			bool moved = this.PositionValid(player, 0, 0, rotateDir, true);

			if (moved == true)
				return true;

			for (int move = 1; move < 3; ++move)
			{
				foreach (int dir in new int[] { -1, 1 })
				{
					moved = this.PositionValid(player, move * dir, 0, rotateDir, true);

					if (moved == true)
						return true;
				}
			}

			return false;
		}

		public int FindLines(TetrisPlayer player)
		{
			int count = 0;

			for (int y = 0; y < player.Board.Height; ++y)
			{
				int lineCount = 0;

				for (int x = 0; x < player.Board.Width; ++x)
				{
					if (player.Board.Peek(x, y) != 0)
						++lineCount;
				}

				if (lineCount == player.Board.Width)
				{
					RemoveLine(player, y);
					++count;
				}
			}

			return count;
		}

		public void RemoveLine(TetrisPlayer player, int yIndex)
		{
			for (int y = yIndex; y >= 0; --y)
			{
				for (int x = 0; x < player.Board.Width; ++x)
				{
					if (y == 0)
						player.Board.Poke(x, y, 0);
					else
						player.Board.Poke(x, y, player.Board.Peek(x, y - 1));
				}
			}
		}

		public void InsertLines(TetrisPlayer player)
		{
			int count = player.IncomingRows;

			if (count >= player.Board.Height)
				count = player.Board.Height - 1;

			int top = player.Board.Height - count;

			//	Move Current Board Up
			for (int y = 0; y < top; ++y)
			{
				for (int x = 0; x < player.Board.Width; ++x)
					player.Board.Poke(x, y, player.Board.Peek(x, y + count));
			}

			int missX = RandomNext(player.Board.Width);


			//	Grey out below
			for (int y = top; y < player.Board.Height; ++y)
			{
				for (int x = 0; x < player.Board.Width; ++x)
				{
					if (missX == x)
						player.Board.Poke(x, y, 0);
					else
						player.Board.Poke(x, y, 8);
				}
			}

			player.IncomingRows -= count;
		}


		public static IPEndPoint CreateEndpoint(string hostNameOrAddressAndPortNumber)
		{
			int portNumber = 32199;
			string hostNameOrAddress = hostNameOrAddressAndPortNumber;

			int index = hostNameOrAddressAndPortNumber.IndexOf(":");
			if (index != -1)
			{
				hostNameOrAddress = hostNameOrAddressAndPortNumber.Substring(0, index).Trim();
				portNumber = Int32.Parse(hostNameOrAddressAndPortNumber.Substring(index + 1).Trim());
			}

			if (hostNameOrAddress == "*")
				return new IPEndPoint(IPAddress.Any, portNumber);

			IPAddress useAddress = null;

			if (IPAddress.TryParse(hostNameOrAddress, out useAddress) == false)
			{
				IPHostEntry hostEntry = System.Net.Dns.GetHostEntry(hostNameOrAddress);

				foreach (IPAddress address in hostEntry.AddressList)
				{
					if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
						continue;

					if (useAddress == null)
						useAddress = address;
					else
						throw new ApplicationException("More than 1 IPv4 Address found (try using the IP not the hostname): " + hostNameOrAddress);
				}

				if (useAddress == null)
					throw new ApplicationException("No IPv4 Address found: " + hostNameOrAddress);
			}

			return new IPEndPoint(useAddress, portNumber);
		}

	}
}
