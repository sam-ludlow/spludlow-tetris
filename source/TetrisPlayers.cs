using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Thread Safe Wrapper for the Players dictionary used by the server
	/// </summary>
	public class TetrisPlayers
	{
		private Dictionary<int, TetrisPlayer> Players = new Dictionary<int, TetrisPlayer>();

		private int LastPlayerId = 0;

		public TetrisPlayers()
		{
		}

		public void Add(TetrisPlayer player)
		{
			lock (this.Players)
			{
				player.ClientId = ++LastPlayerId;
				this.Players.Add(player.ClientId, player);
			}
		}

		public TetrisPlayer Remove(int playerId)
		{
			TetrisPlayer player = null;

			lock (this.Players)
			{
				if (this.Players.ContainsKey(playerId) == true)
				{
					player = this.Players[playerId];
					this.Players.Remove(playerId);
				}
			}

			return player;
		}

		public TetrisPlayer[] CurrentPlayers()
		{
			TetrisPlayer[] players;

			lock (this.Players)
			{
				players = new TetrisPlayer[this.Players.Keys.Count];
				int index = 0;
				foreach (int clientId in this.Players.Keys)
					players[index++] = this.Players[clientId];
			}

			return players;
		}

		public TetrisPlayer Get(int playerId)
		{
			lock (this.Players)
			{
				if (this.Players.ContainsKey(playerId) == true)
					return this.Players[playerId];
			}
			return null;
		}

		public int TargetNext(int currentTargetPlayerId)
		{
			List<int> keyList;

			lock (this.Players)
			{
				keyList = new List<int>(this.Players.Keys);
			}

			int index = -1;
			if (currentTargetPlayerId != 0 && keyList.Contains(currentTargetPlayerId) == true)
				index = keyList.IndexOf(currentTargetPlayerId);

			++index;
			if (index >= keyList.Count)
				return 0;

			return keyList[index];
		}
	}
}
