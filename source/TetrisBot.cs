using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Run a Tetris Bot on a supplied Tetris Client
	/// </summary>
	public class TetrisBot
	{
		private bool AskStop = true;

		public TetrisBot()
		{
		}

		public void Start(TetrisClient client, int ms)
		{
			this.AskStop = false;

			Task.Run(() => Run(client, ms));
		}

		public void Stop()
		{
			this.AskStop = true;
		}

		public bool Running
		{
			get
			{
				return !this.AskStop;
			}
		}

		public void Run(TetrisClient client, int ms)
		{
			Random random = new Random();

			try
			{
				while (AskStop == false)
				{
					NextMove(client, ms);

					Thread.Sleep(250);

					if (random.Next(100) == 0)
					{
						client.TargetNext();
						Thread.Sleep(100);
					}
				}
			}
			catch (ObjectDisposedException)
			{
				AskStop = true;
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Bot, Run Loop", ee);
			}
		}

		/// <summary>
		/// Make the next move on a client
		/// </summary>
		public static void NextMove(TetrisClient client, int ms)
		{
			int bestMoveX = 0;
			int bestRot = 0;
			int bestScore = 0;

			//	for each rotation
			for (int rot = 0; rot < 4; ++rot)
			{
				TetrisBoard board = client.GetBoards(0)[0];

				if (board == null)	//	Not got board yet
				{
					Thread.Sleep(1000);
					return;
				}

				//	Calculate scores at each X postion
				int[] moveXScore = TetrisBot.Calculate(board);

				int moveX = moveXScore[0];
				int score = moveXScore[1];

				//	Keep note of X and rot for best score
				if (score > bestScore)
				{
					bestScore = score;
					bestMoveX = moveX;
					bestRot = rot;
				}

				//	Rotate for next test
				client.RotateCW();

				Thread.Sleep(ms);
			}

			//	Rotate to where best result was
			for (int rot = 0; rot < bestRot; ++rot)
				client.RotateCW();

			//	Move to correct X position
			for (int move = 0; move < Math.Abs(bestMoveX); ++move)
			{
				if (bestMoveX < 0)
					client.Left();
				else
					client.Right();

				Thread.Sleep(ms);
			}

			//	Perform the drop
			client.Up();
		}

		/// <summary>
		/// Calculate the score for each X postion
		/// </summary>
		public static int[] Calculate(TetrisBoard clientBoard)
		{
			//	Comb out current shape current=(9-15) placed=(1-8)
			
			int shapeX = clientBoard.Width;
			int shapeY = clientBoard.Height;

			int maxX = 0;
			int maxY = 0;

			TetrisBoard shape = new TetrisBoard(4, 4);

			for (int pass = 0; pass < 2; ++pass)
			{
				for (int y = 0; y < clientBoard.Height; ++y)
				{
					for (int x = 0; x < clientBoard.Width; ++x)
					{
						byte value = clientBoard.Peek(x, y);

						if (value >= 9)
						{
							if (pass == 0)
							{
								shapeX = Math.Min(x, shapeX);
								shapeY = Math.Min(y, shapeY);
								maxX = Math.Max(x, maxX);
								maxY = Math.Max(y, maxY);
							}
							else
							{
								byte fillValue = (byte)(value - 8);
								shape.Poke(x - shapeX, y - shapeY, fillValue);
							}
						}
					}
				}
			}

			int shapeWidth = (maxX - shapeX) + 1;
			int shapeHeight = (maxY - shapeY) + 1;

			//	Create a map of blocks that want filling, anything within curent shape that will empty underneith
			List<int[]> wantedBlocks = new List<int[]>();

			for (int x = 0; x < shapeWidth; ++x)
			{
				for (int y = shapeHeight - 1; y >= 0; --y)
				{
					if (shape.Peek(x, y) == 0)
						wantedBlocks.Add(new int[] { x, y });
					else
						break;

				}

				//	Add 2 rows on the bottom of the map to keep holes open
				wantedBlocks.Add(new int[] { x, shapeHeight });
				wantedBlocks.Add(new int[] { x, shapeHeight + 1 });
			}


			int bestX = 0;
			int bestBestY = 0;

			//	For each X position
			for (int x = 0; x < (clientBoard.Width - (shapeWidth - 1)); ++x)
			{
				int bestY = 0;
				int emptyCount = 0;

				//	For each Y postion
				for (int y = 0; y < (clientBoard.Height - (shapeHeight - 1)); ++y)
				{
					//	If can go here
					if (ShapePositionValid(clientBoard, shape, x, y) == true)
					{
						bestY = y;

						//	Conut up empties in map
						emptyCount = 0;
						foreach (int[] xy in wantedBlocks)
						{
							int px = x + xy[0];
							int py = y + xy[1];
							if (py < clientBoard.Height && clientBoard.Peek(px, py) == 0)
								++emptyCount;
						}
					}
					else
					{
						break;
					}
				}

				//	Calculate score
				bestY += shapeHeight;
				bestY -= (emptyCount * 2);

				//	Keep best
				if (bestY > bestBestY)
				{
					bestX = x;
					bestBestY = bestY;
				}
			}

			int moveX = bestX - shapeX;

			return new int[] { moveX, bestBestY };
		}

		/// <summary>
		/// Modified version of what's in server, wont detect the current shape
		/// </summary>
		public static bool ShapePositionValid(TetrisBoard playerBoard, TetrisBoard shape, int x, int y)
		{
			for (int shapeY = 0; shapeY < 4; ++shapeY)
			{
				for (int shapeX = 0; shapeX < 4; ++shapeX)
				{
					byte shapeValue = shape.Peek(shapeX, shapeY);
					if (shapeValue == 0)
						continue;

					int boardX = x + shapeX;
					int boardY = y + shapeY;

					if (boardX < 0 || boardY < 0)
						return false;

					if (boardX >= playerBoard.Width || boardY >= playerBoard.Height)
						return false;

					byte boardValue = playerBoard.Peek(boardX, boardY);

					if (boardValue > 0 && boardValue < 9)
						return false;
				}
			}

			return true;
		}
	}
}
