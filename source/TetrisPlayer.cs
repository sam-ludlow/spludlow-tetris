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
	/// Used by the Server only
	/// Stores each players current state
	/// </summary>
	public class TetrisPlayer
	{
		public int ClientId = 0;
		public string DisplayName = "";

		public Socket ClientSocket;

		public object _PlayerLock = new object();	//	Prevent cross thread problems, before saw some board corruption

		public int X;
		public int Y;

		public int Rotate;  //	0, 1, 2, 3

		public int Shape;

		public int NextShape;

		public TetrisBoard Board;

		public int TargetClientId = 0;

		public int IncomingRows = 0;

		public TetrisPlayerScore Score;

		public int Level = 0;

		public bool Removed = false;
	}

	/// <summary>
	/// Player Score statistics
	/// </summary>
	public class TetrisPlayerScore
	{
		public DateTime TimeConnect;
		public DateTime TimeGameStart;

		public int CountGameOver;
	}
}
