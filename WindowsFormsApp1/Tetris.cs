using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
	public class TetrisGame
	{
		public const int SquareDPIWidth = 30;
		private int level;
		public int Score { get; private set; } = 0;
		public int Level { get => level; set => level = (value > 0 && value <= 8) ? value : throw new ArgumentOutOfRangeException(); }
		public int Line { get; private set; } = 0;
		public int PlayingTime { get; private set; }
		private static Random RandomGenerator = new Random();

		public class Board
		{
			public const int MaxRow = 25;
			public const int Row = 20;
			public const int Column = 10;
			public Color?[,] board = new Color?[Column, Row];
			protected Piece CurrentPiece { get; set; }
			protected (int CoordX, int CoordY) PieceCenter { get; set; }
			public bool isFailing;

			public Board() { }

			public Color?[,] GetBoard()
			{
				Color?[,] newBoard = (Color?[,])board.Clone();
				int CoordX, CoordY;
				foreach (var square in CurrentPiece.Offset)
				{
					CoordX = square.OffSetX + PieceCenter.CoordX;
					CoordY = square.OffSetY + PieceCenter.CoordY;
					if (0 <= CoordX && CoordX < Column && 0 <= CoordY && CoordY < Row) 
						newBoard[CoordX, CoordY] = CurrentPiece.Color;
				}
				return newBoard;
			}
			public bool ClockwiseRotate()
			{
				CurrentPiece.ClockwiseRotate(this);
				if (CheckPieceChange() == false)
				{
					CurrentPiece.AntiClockwiseRotate(this);
					return false;
				}
				else
				{
					return true;
				}
			}
			public bool AntiClockwiseRotate()
			{
				CurrentPiece.AntiClockwiseRotate(this);
				if (CheckPieceChange() == false)
				{
					CurrentPiece.ClockwiseRotate(this);
					return false;
				}
				else
				{
					return true;
				}
			}
			public bool Falling()
			{
				PieceCenter = TupleYInc(PieceCenter, -1);
				if (CheckPieceChange() == false)
				{
					PieceCenter = TupleYInc(PieceCenter, +1);
					DiscardCurrentPiece();
					NewPiece(RandomGenerator.Next() % Piece.PieceTypeNumber);
					return false;
				}
				else
				{
					return true;
				}
			}
			public bool MoveLeft()
			{
				PieceCenter = TupleXInc(PieceCenter, -1);
				if (CheckPieceChange() == false)
				{
					PieceCenter = TupleXInc(PieceCenter, +1);
					return false;
				}
				else
				{
					return true;
				}
			}
			public bool MoveRight()
			{
				PieceCenter = TupleXInc(PieceCenter, +1);
				if (CheckPieceChange() == false)
				{
					PieceCenter = TupleXInc(PieceCenter, -1);
					return false;
				}
				else
				{
					return true;
				}
			}
			public void Start()
			{
				isFailing = false;
				NewPiece(RandomGenerator.Next() % Piece.PieceTypeNumber);
			}

			private static bool IsInBoard(int x, int y)
			{
				return (0 <= x && x < Column && 0 <= y && y < Row);
			}
			private bool CheckPieceChange()  //检验砖块变化后是否合规
			{
				int CoordX, CoordY;
				bool isOutOfRow;
				foreach (var square in CurrentPiece.Offset)
				{
					CoordX = PieceCenter.CoordX + square.OffSetX;
					CoordY = PieceCenter.CoordY + square.OffSetY;
					isOutOfRow = CoordY >= Row;
					if (!(IsInBoard(CoordX, CoordY) || isOutOfRow) || (!isOutOfRow && board[CoordX, CoordY] != null)) 
					{
						return false;
					}
				}
				return true;
			}
			private void Eliminate()
			{
				bool isRowFilled;
				int CoordY = 0;
				while (CoordY < Row)
				{
					isRowFilled = CheckEliminateRow(CoordY);
					if (isRowFilled)
					{
						EliminateRow(CoordY);
					}
					else
					{
						CoordY++;
					}
				}

				bool CheckEliminateRow(int row)
				{
					for (int CoordX = 0; CoordX < Column; CoordX++)
					{
						if (board[CoordX, row] == null)
						{
							return false;
						}
					}
					return true;
				}
				void EliminateRow(int row)
				{
					for (int coordY = row; coordY < Row - 1; coordY++)
					{
						for (int coordX = 0; coordX < Column; coordX++)
						{
							board[coordX, coordY] = board[coordX, coordY + 1];
						}
					}
					for (int coordX = 0; coordX < Column; coordX++)
					{
						board[coordX, Row - 1] = null;
					}
				}
			}
			private void NewPiece(int type)
			{
				Piece.PieceType Type = (Piece.PieceType)type;
				switch (Type)
				{
					case Piece.PieceType.ShapeI:
						CurrentPiece = new PieceI(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeJ:
						CurrentPiece = new PieceJ(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeL:
						CurrentPiece = new PieceL(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeT:
						CurrentPiece = new PieceT(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeS:
						CurrentPiece = new PieceS(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeZ:
						CurrentPiece = new PieceZ(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeO:
						CurrentPiece = new PieceO(RandomGenerator.Next());
						break;
					default:
						break;
				}
				int YOffset = -CurrentPiece.Offset.Min(t => t.OffSetY);
				PieceCenter = (Column / 2, Row + YOffset);
			}
			private void DiscardCurrentPiece()
			{
				int CoordX, CoordY;
				foreach (var square in CurrentPiece.Offset)
				{
					CoordX = PieceCenter.CoordX + square.OffSetX;
					CoordY = PieceCenter.CoordY + square.OffSetY;
					if (IsInBoard(CoordX, CoordY))
						board[CoordX, CoordY] = CurrentPiece.Color;
					else
					{
						Fail();
					}
				}
				Eliminate();
			}
			private void Fail()
			{
				isFailing = true;
			}
			

			public abstract class Piece
			{
				public static int PieceTypeNumber = 7;
				public enum PieceType
				{
					ShapeI,
					ShapeJ,
					ShapeL,
					ShapeT,
					ShapeS,
					ShapeZ,
					ShapeO,
					ShapeX,
					ShapeDot,
					ShapeSquareO,
					ShapeN,
				}
				public Color Color { get; protected set; }
				public (int CoordX, int CoordY) CenterInFourByFourTable { get; protected set; } //图案在4*4的表格上的中心
				public (int OffSetX, int OffSetY)[] Offset { get; protected set; }  //图案相对背板中心的偏移量
				public abstract void ClockwiseRotate(Board board);
				public abstract void AntiClockwiseRotate(Board board);
				public virtual void SpecialAbility(Board board) { }
			}
			public class PieceI : Piece
			{
				private static (int CoordX, int CoordY)[] centerInFourByFourTables = new[] { (1, 1), (1, 1) };
				private static (int OffSetX, int OffSetY)[][] offsets = new[]
				{
				new[] { (0, 2), (0, 1), (0, 0), (0, -1) },
				new[] { (-1, 0), (0, 0), (1, 0), (2, 0) },
				};
				private int cursor = 0;
				private int Cursor
				{
					get { return cursor; }
					set
					{
						cursor = (value + offsets.Count()) % offsets.Count();
						CenterInFourByFourTable = centerInFourByFourTables[Cursor];
						Offset = offsets[Cursor];
					}
				}

				public PieceI() { Color = Color.Maroon; }
				public PieceI(int rotationTimes) : this() { Cursor = rotationTimes; }

				public override void AntiClockwiseRotate(Board board)
				{
					if (Cursor == 0) board.PieceCenter = (board.PieceCenter.CoordX - 1, board.PieceCenter.CoordY);
					Cursor = Cursor - 1;
				}
				public override void ClockwiseRotate(Board board)
				{
					if (Cursor == 1) board.PieceCenter = (board.PieceCenter.CoordX + 1, board.PieceCenter.CoordY);
					Cursor = Cursor + 1;
				}
			}
			public class PieceJ : Piece
			{
				private static (int CoordX, int CoordY)[] centerInFourByFourTables = new[] { (2, 1), (1, 1), (1, 2), (2, 2) };
				private static (int OffSetX, int OffSetY)[][] offsets = new[]
				{
				new[] { (-1, 0), (0, 0), (0, 1), (0, 2) },
				new[] { (0, 1), (0, 0), (1, 0), (2, 0) },
				new[] { (1, 0), (0, 0), (0, -1), (0, -2) },
				new[] { (0, -1), (0, 0), (-1, 0), (-2, 0) },
				};
				private int cursor = 0;
				private int Cursor
				{
					get { return cursor; }
					set
					{
						cursor = (value + offsets.Count()) % offsets.Count();
						CenterInFourByFourTable = centerInFourByFourTables[Cursor];
						Offset = offsets[Cursor];
					}
				}

				public PieceJ() { Color = Color.Silver; }
				public PieceJ(int rotationTimes) : this() { Cursor = rotationTimes; }

				public override void AntiClockwiseRotate(Board board)
				{
					Cursor = Cursor - 1;
				}

				public override void ClockwiseRotate(Board board)
				{
					Cursor = Cursor + 1;
				}
			}
			public class PieceL : Piece
			{
				private static (int CoordX, int CoordY)[] centerInFourByFourTables = new[] { (1, 1), (2, 1), (2, 2), (1, 2) };
				private static (int OffSetX, int OffSetY)[][] offsets = new[]
				{
				new[] { (1, 0), (0, 0), (0, 1), (0, 2) },
				new[] { (0, 1), (0, 0), (-1, 0), (-2, 0) },
				new[] { (-1, 0), (0, 0), (0, -1), (0, -2) },
				new[] { (0, -1), (0, 0), (1, 0), (2, 0) },
				};
				private int cursor = 0;
				private int Cursor
				{
					get { return cursor; }
					set
					{
						cursor = (value + offsets.Count()) % offsets.Count();
						CenterInFourByFourTable = centerInFourByFourTables[Cursor];
						Offset = offsets[Cursor];
					}
				}

				public PieceL() { Color = Color.Purple; }
				public PieceL(int rotationTimes) : this() { Cursor = rotationTimes; }

				public override void AntiClockwiseRotate(Board board)
				{

					Cursor = Cursor + 1;
				}

				public override void ClockwiseRotate(Board board)
				{
					Cursor = Cursor - 1;
				}
			}
			public class PieceO : Piece
			{
				public PieceO()
				{
					Color = Color.Navy;
					CenterInFourByFourTable = (1, 1);
					Offset = new[] { (0, 0), (0, 1), (1, 0), (1, 1) };
				}
				public PieceO(int rotationTimes) : this() { }

				public override void AntiClockwiseRotate(Board board)
				{
					//do nothing
					return;
				}

				public override void ClockwiseRotate(Board board)
				{
					//do nothing
					return;
				}
			}
			public class PieceZ : Piece
			{
				private static (int CoordX, int CoordY)[] centerInFourByFourTables = new[] { (2, 1), (2, 1) };
				private static (int OffSetX, int OffSetY)[][] offsets = new[]
				{
				new[] { (1, 0), (0, 0), (0, 1), (-1, 1) },
				new[] { (0, 1), (0, 0), (-1, 0), (-1, -1) },
				};
				private int cursor = 0;
				private int Cursor
				{
					get { return cursor; }
					set
					{
						cursor = (value + offsets.Count()) % offsets.Count();
						CenterInFourByFourTable = centerInFourByFourTables[Cursor];
						Offset = offsets[Cursor];
					}
				}

				public PieceZ() { Color = Color.Teal; }
				public PieceZ(int rotationTimes) : this() { Cursor = rotationTimes; }

				public override void AntiClockwiseRotate(Board board)
				{
					if (Cursor == 1) board.PieceCenter = (board.PieceCenter.CoordX - 1, board.PieceCenter.CoordY);
					Cursor = Cursor - 1;
				}
				public override void ClockwiseRotate(Board board)
				{
					if (Cursor == 0) board.PieceCenter = (board.PieceCenter.CoordX + 1, board.PieceCenter.CoordY);
					Cursor = Cursor + 1;
				}
			}
			public class PieceS : Piece
			{
				private static (int CoordX, int CoordY)[] centerInFourByFourTables = new[] { (1, 1), (1, 1) };
				private static (int OffSetX, int OffSetY)[][] offsets = new[]
				{
				new[] { (-1, 0), (0, 0), (0, 1), (1, 1) },
				new[] { (0, 1), (0, 0), (1, 0), (1, -1) },
				};
				private int cursor = 0;
				private int Cursor
				{
					get { return cursor; }
					set
					{
						cursor = (value + offsets.Count()) % offsets.Count();
						CenterInFourByFourTable = centerInFourByFourTables[Cursor];
						Offset = offsets[Cursor];
					}
				}

				public PieceS() { Color = Color.DarkGreen; }
				public PieceS(int rotationTimes) : this() { Cursor = rotationTimes; }

				public override void AntiClockwiseRotate(Board board)
				{
					if (Cursor == 0) board.PieceCenter = (board.PieceCenter.CoordX - 1, board.PieceCenter.CoordY);
					Cursor = Cursor + 1;
				}
				public override void ClockwiseRotate(Board board)
				{
					if (Cursor == 1) board.PieceCenter = (board.PieceCenter.CoordX + 1, board.PieceCenter.CoordY);
					Cursor = Cursor - 1;
				}
			}
			public class PieceT : Piece
			{
				private static (int CoordX, int CoordY)[] centerInFourByFourTables = new[] { (1, 1), (1, 2), (2, 1), (1, 1) };
				private static (int OffSetX, int OffSetY)[][] offsets = new[]
				{
				new[] { (0, -1), (0, 0), (1, 0), (-1, 0) },  //下
				new[] { (-1, 0), (0, 0), (0, -1), (0, 1) },  //左
				new[] { (0, 1), (0, 0), (-1, 0), (1, 0) },  //上
				new[] { (1, 0), (0, 0), (0, 1), (0, -1) },  //右
				};
				private int cursor = 0;
				private int Cursor
				{
					get { return cursor; }
					set
					{
						cursor = (value + offsets.Count()) % offsets.Count();
						CenterInFourByFourTable = centerInFourByFourTables[Cursor];
						Offset = offsets[Cursor];
					}
				}

				public PieceT() { Color = Color.Brown; }
				public PieceT(int rotationTimes) : this() { Cursor = rotationTimes; }

				public override void AntiClockwiseRotate(Board board)
				{
					Cursor = Cursor - 1;
				}

				public override void ClockwiseRotate(Board board)
				{
					Cursor = Cursor + 1;
				}
			}
		}

		static (int, int) TupleXInc((int, int) t, int inc)
		{
			return (t.Item1 + inc, t.Item2);
		}
		static (int, int) TupleYInc((int, int) t, int inc)
		{
			return (t.Item1, t.Item2 + inc);
		}
	}
}
