using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spludlow.Tetris
{
	/// <summary>
	/// Define the shapes
	/// Create all 4 rotations of the 7 peices and store them in array of 4x4 TetrisBoards
	/// </summary>
	public class TetrisShapes
	{
		public TetrisBoard[] ShapeBoards;

		public TetrisShapes()
		{
			int shapeCount = 7;

			ShapeBoards = new TetrisBoard[shapeCount * 4];

			string[] shapesText = new string[]
			{
				"    ",
				"1111",
				"    ",
				"    ",

				"    ",
				"222 ",
				"  2 ",
				"    ",

				"    ",
				"333 ",
				"3   ",
				"    ",

				"    ",
				" 44 ",
				" 44 ",
				"    ",

				"    ",
				" 55 ",
				"55  ",
				"    ",

				"    ",
				"666 ",
				" 6  ",
				"    ",

				"    ",
				"77  ",
				" 77 ",
				"    ",
			};

			//	For each shape
			for (int shapeIndex = 0; shapeIndex < shapeCount; ++shapeIndex)
			{
				TetrisBoard shape = new TetrisBoard(4, 4);

				//	Read the first rotation from the text
				for (int y = 0; y < 4; ++y)
				{
					string line = shapesText[shapeIndex * 4 + y];

					for (int x = 0; x < 4; ++x)
					{
						char ch = line[x];
						if (ch != ' ')
							shape.Poke(x, y, Byte.Parse(ch.ToString()));
					}
				}

				//	Perform the 3 other rotations
				for (int rotation = 0; rotation < 4; ++rotation)
				{
					ShapeBoards[shapeIndex * 4 + rotation] = shape;

					TetrisBoard newShape = new TetrisBoard(4, 4);

					for (int y = 0; y < 4; ++y)
					{
						for (int x = 0; x < 4; ++x)
						{
							byte value = shape.Peek(x, y);
							if (value != 0)
								newShape.Poke((4 - y) - 1, x, value);
						}
					}
					shape = newShape;
				}
			}

			//	For every shape we want to move to the top left corner
			for (int shapeIndex = 0; shapeIndex < shapeCount; ++shapeIndex)
			{
				//	For each rotation
				for (int rotation = 0; rotation < 4; ++rotation)
				{
					TetrisBoard shape = ShapeBoards[shapeIndex * 4 + rotation];

					//	move up
					while (EmptyRow(shape) == true)
					{
						for (int y = 0; y < 4; ++y)
						{
							for (int x = 0; x < 4; ++x)
							{
								if (y == 3)
									shape.Poke(x, y, 0);
								else
									shape.Poke(x, y, shape.Peek(x, y + 1));
							}
						}
					}

					//	move left
					while (EmptyColumn(shape) == true)
					{
						for (int x = 0; x < 4; ++x)
						{
							for (int y = 0; y < 4; ++y)
							{
								if (x == 3)
									shape.Poke(x, y, 0);
								else
									shape.Poke(x, y, shape.Peek(x + 1, y));
							}
						}
					}
				}
			}

			//	Fix longy, move verticals 1 to the right
			foreach (int rotation in new int[] { 1, 3 })
			{
				TetrisBoard shape = ShapeBoards[0 * 4 + rotation];

				for (int x = 3; x >= 0; --x)
				{
					for (int y = 0; y < 4; ++y)
					{
						if (x == 0)
							shape.Poke(x, y, 0);
						else
							shape.Poke(x, y, shape.Peek(x - 1, y));
					}
				}
			}
		}

		private static bool EmptyRow(TetrisBoard shape)
		{
			for (int x = 0; x < shape.Width; ++x)
			{
				if (shape.Peek(x, 0) > 0)
					return false;
			}

			return true;
		}

		private static bool EmptyColumn(TetrisBoard shape)
		{
			for (int y = 0; y < shape.Height; ++y)
			{
				if (shape.Peek(0, y) > 0)
					return false;
			}

			return true;
		}
	}
}
