using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
	public sealed class TetrisGame
	{
		//public const int SquareDPIWidth = 30;
		public event EventHandler PaintEvent;
		public event EventHandler LoseGameEvent;
		public int Row { get => Board.Row; }
		public int Column { get => Board.Column; }
		public int InvisibleRow { get => Board.MaxRow; }
		public int Score { get; private set; } = 0;
		public int Level { get => Math.Min(Score / 120 + 1, 8); }
		public int EliminatedLine { get; private set; } = 0;
		public string PlayingTime {get => $"{playingTime.Hour.ToString().PadLeft(2,'0')}:{playingTime.Minute.ToString().PadLeft(2, '0')}:{playingTime.Second.ToString().PadLeft(2, '0')}"; }
		public bool isFailing;

		private DateTime playingTime { get; set; }
		private Board board = new Board();

		public TetrisGame()
		{
			board.LineEliminateEvent += LineEliminate;
			board.IncreaseScoreEvent += IncreaseScore;
			board.LoseGameEvent += LoseGame;
		}
		public TetrisGame(EventHandler paintEvent) : this()
		{
			PaintEvent = paintEvent;
		}
		public Color?[,] GetNextPieceBoard()
		{
			return board.GetFourByFourTable();
		}
		public Color?[,] GetBoard()
		{
			return board.GetBoard();
		}
		public void ClockwiseRotate()
		{
			if (board.ClockwiseRotate())
				lock (PaintEvent)
				{
					PaintEvent(this, null);
				}
		}
		public void AntiClockwiseRotate()
		{
			if (board.AntiClockwiseRotate())
				lock (PaintEvent)
				{
					PaintEvent(this, null);
				}
		}
		public void FallToBottom()
		{
			while (board.Falling()) ;
			lock (PaintEvent)
			{
				if (!isFailing) PaintEvent(this, null);
			}
		}
		public void MoveLeft()
		{
			if (board.MoveLeft(1))
				lock (PaintEvent)
				{
					PaintEvent(this, null);
				}
		}
		public void MoveRight()
		{
			if (board.MoveRight(1))
				lock (PaintEvent)
				{
					PaintEvent(this, null);
				}
		}
		public void Start()
		{
			Score = 0;
			EliminatedLine = 0;
			playingTime = new DateTime(1, 1, 1, 0, 0, 0);
			isFailing = false;
			board.Start();
			Thread fallingThread = new Thread(FallingThread);
			fallingThread.Start();
			Thread timingThread = new Thread(TimingThread);
			timingThread.Start();
		}

		private void Falling()
		{
			if (board.Falling())
				lock (PaintEvent)
				{
					if (!isFailing) PaintEvent(this, null);
				}
		}
		private void TimingThread()
		{
			while(!isFailing)
			{
				Thread.Sleep(1000);
				playingTime = playingTime.AddSeconds(1);
			}		
		}
		private void LineEliminate(object sender, int increment)
		{
			EliminatedLine += increment;
		}
		private void IncreaseScore(object sender, int increment)
		{
			int oldScore = Score;
			Score += increment;
			int newScore = Score;
			if (!board.CanSpecialPieceGenerate && (oldScore / 100 < newScore / 100)) board.CanSpecialPieceGenerate = true;
		}
		private void LoseGame()
		{
			if (isFailing == false) LoseGameEvent(this, null);
			isFailing = true;
		}
		private void FallingThread()
		{
			while (!isFailing)
			{
				Falling();
				Thread.Sleep(500 - 50 * Level);
				//Thread.Sleep(200);
			}
		}

		private sealed class Board
		{
			public const int MaxRow = 30;
			public const int Row = 25;
			public const int Column = 10;
			public event EventHandler<int> LineEliminateEvent;
			public event EventHandler<int> IncreaseScoreEvent;
			public event Action LoseGameEvent;
			public Color?[,] board = new Color?[Column, Row];
			public bool CanSpecialPieceGenerate { get; set; } = false;
			private static Random RandomGenerator = new Random();
			private Piece NextPiece { get; set; }
			private Piece CurrentPiece { get; set; }
			private (int CoordX, int CoordY) CurrentPieceCenter
			{
				get => currentPieceCenter;
				set
				{
					if (0 <= value.CoordX && value.CoordX < Column)
						currentPieceCenter = value;
				}
			}

			private (int CoordX, int CoordY) currentPieceCenter;
			public Board() { }

			public Color?[,] GetFourByFourTable()
			{
				return NextPiece.GetFourByFourTable();
			}
			public Color?[,] GetBoard()
			{
				Color?[,] newBoard = (Color?[,])board.Clone();
				int CoordX, CoordY;
				foreach (var square in CurrentPiece.Offset)
				{
					CoordX = square.OffSetX + CurrentPieceCenter.CoordX;
					CoordY = square.OffSetY + CurrentPieceCenter.CoordY;
					if (0 <= CoordX && CoordX < Column && 0 <= CoordY && CoordY < Row)
						newBoard[CoordX, CoordY] = CurrentPiece.Color;
				}
				return newBoard;
			}
			public bool ClockwiseRotate()
			{
				CurrentPiece.ClockwiseRotate(this);
				if (IsLegalPieceMove() == false)
				{
					bool isMoveLeftFirst = CurrentPieceCenter.CoordX > Column / 2;
					foreach (var offset in TryMoveRightOffset(isMoveLeftFirst))
					{
						if (MoveRight(offset) == true)
						{
							return true;
						}
					}
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
				if (IsLegalPieceMove() == false)
				{
					bool isMoveLeftFirst = CurrentPieceCenter.CoordX > Column / 2;
					foreach (var offset in TryMoveRightOffset(isMoveLeftFirst))
					{		
						if (MoveRight(offset) == true)
						{
							return true;
						}
					}
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
				CurrentPieceCenter = CurrentPieceCenter.YAdd(-1);
				if (IsLegalPieceMove() == false)
				{
					CurrentPieceCenter = CurrentPieceCenter.YAdd(1);
					bool isFailing = CurrentPiece.Discard(this);
					if (!isFailing)
					{
						Eliminate();
						ChangeCurrentPiece();
					}
					return false;
				}
				else
				{
					return true;
				}
			}
			public bool MoveLeft(int distance)
			{
				CurrentPieceCenter = CurrentPieceCenter.XAdd(-distance);
				if (IsLegalPieceMove() == false)
				{
					CurrentPieceCenter = CurrentPieceCenter.XAdd(distance);
					return false;
				}
				else
				{
					return true;
				}
			}
			public bool MoveRight(int distance)
			{
				CurrentPieceCenter = CurrentPieceCenter.XAdd(distance);
				if (IsLegalPieceMove() == false)
				{
					CurrentPieceCenter = CurrentPieceCenter.XAdd(-distance);
					return false;
				}
				else
				{
					return true;
				}
			}
			public void Start()
			{
				NextPiece = NewPiece(RandomGenerator.Next());
				//NextPiece = NewPiece(Piece.PieceType.ShapeV);
				ChangeCurrentPiece();
			}

			private static bool IsInBoard(int x, int y)
			{
				return (0 <= x && x < Column && 0 <= y && y < Row);
			}
			private bool IsLegalPieceMove()  //检验砖块变化后是否合规
			{
				int CoordX, CoordY;
				bool isOutOfRow;
				foreach (var square in CurrentPiece.Offset)
				{
					CoordX = CurrentPieceCenter.CoordX + square.OffSetX;
					CoordY = CurrentPieceCenter.CoordY + square.OffSetY;
					isOutOfRow = CoordY >= Row;
					if (!(IsInBoard(CoordX, CoordY) || (isOutOfRow && 0 <= CoordX && CoordX < Column)) || (!isOutOfRow && board[CoordX, CoordY] != null))
					{
						return false;
					}
				}
				return true;
			}
			private void Eliminate()
			{
				bool isRowFilled;
				int count = 0;
				int CoordY = 0;
				while (CoordY < Row)
				{
					isRowFilled = CheckEliminateRow(CoordY);
					if (isRowFilled)
					{
						EliminateRow(CoordY);
						count++;
					}
					else
					{
						CoordY++;
					}
				}
				LineEliminateEvent(this, count);
				IncreaseScoreEvent(this, Column * count);

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
			private Piece NewPiece(int t)
			{
				int mod = CanSpecialPieceGenerate ? Piece.AllPieceTypeNumber : Piece.NormalPieceTypeNumber;
				Piece.PieceType type = (Piece.PieceType)(t % mod);
				Piece newPiece;
				switch (type)
				{
					case Piece.PieceType.ShapeI:
						newPiece = new PieceI(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeJ:
						newPiece = new PieceJ(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeL:
						newPiece = new PieceL(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeT:
						newPiece = new PieceT(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeS:
						newPiece = new PieceS(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeZ:
						newPiece = new PieceZ(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeO:
						newPiece = new PieceO(RandomGenerator.Next());
						break;
					case Piece.PieceType.ShapeV:
						newPiece = new PieceV(RandomGenerator.Next());
						CanSpecialPieceGenerate = false;
						break;
					case Piece.PieceType.ShapeX:
						newPiece = new PieceX(RandomGenerator.Next());
						CanSpecialPieceGenerate = false;
						break;
					default:
						newPiece = null;
						break;
				}
				return newPiece;
			}
			private void ChangeCurrentPiece()
			{
				CurrentPiece = NextPiece;
				int YOffset = -CurrentPiece.Offset.Min(t => t.OffSetY);
				CurrentPieceCenter = (Column / 2 - RandomGenerator.Next() % 2, Row + YOffset);
				NextPiece = NewPiece(RandomGenerator.Next());
				//NextPiece = NewPiece(Piece.PieceType.ShapeX);
			}
			IEnumerable<int> TryMoveRightOffset(bool isMoveLeftFirst)
			{
				int coefficient = isMoveLeftFirst ? -1 : 1;
				yield return 1 * coefficient;
				yield return 2 * coefficient;
				yield return -1 * coefficient;
				yield return -2 * coefficient;

			}


			public abstract class Piece
			{
				public static int NormalPieceTypeNumber = 7;
				public static int AllPieceTypeNumber = 9;
				public enum PieceType
				{
					ShapeI,
					ShapeJ,
					ShapeL,
					ShapeT,
					ShapeS,
					ShapeZ,
					ShapeO,
					ShapeV,
					ShapeX,
				}
				public Color Color { get; protected set; }
				public (int CoordX, int CoordY) CenterInFourByFourTable { get; protected set; } //图案在4*4的表格上的中心
				public (int OffSetX, int OffSetY)[] Offset { get; protected set; }  //图案相对背板中心的偏移量
				public abstract void ClockwiseRotate(Board board);
				public abstract void AntiClockwiseRotate(Board board);
				public virtual bool Discard(Board board)
				{
					bool isFailing = false;
					int CoordX, CoordY;
					foreach (var square in board.CurrentPiece.Offset)
					{
						CoordX = board.CurrentPieceCenter.CoordX + square.OffSetX;
						CoordY = board.CurrentPieceCenter.CoordY + square.OffSetY;
						if (IsInBoard(CoordX, CoordY))
							board.board[CoordX, CoordY] = this.Color;
						else
						{
							isFailing = true;
							board.LoseGameEvent();
						}
					}
					return isFailing;
				}
				public Color?[,] GetFourByFourTable()
				{
					Color?[,] newBoard = new Color?[4, 4];
					int CoordX, CoordY;
					foreach (var square in Offset)
					{
						CoordX = square.OffSetX + CenterInFourByFourTable.CoordX;
						CoordY = square.OffSetY + CenterInFourByFourTable.CoordY;
						newBoard[CoordX, CoordY] = Color;
					}
					return newBoard;
				}
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
					if (Cursor == 0) board.CurrentPieceCenter = board.CurrentPieceCenter.XAdd(-1);
					Cursor = Cursor - 1;
				}
				public override void ClockwiseRotate(Board board)
				{
					if (Cursor == 1) board.CurrentPieceCenter = board.CurrentPieceCenter.XAdd(1);
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
					if (Cursor == 1) board.CurrentPieceCenter = board.CurrentPieceCenter.XAdd(-1);
					Cursor = Cursor - 1;
				}
				public override void ClockwiseRotate(Board board)
				{
					if (Cursor == 0) board.CurrentPieceCenter = board.CurrentPieceCenter.XAdd(1);
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
					if (Cursor == 0) board.CurrentPieceCenter = board.CurrentPieceCenter.XAdd(-1);
					Cursor = Cursor + 1;
				}
				public override void ClockwiseRotate(Board board)
				{
					if (Cursor == 1) board.CurrentPieceCenter = board.CurrentPieceCenter.XAdd(1);
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
			public class PieceV : Piece
			{
				public PieceV()
				{
					Color = Color.Olive;
					CenterInFourByFourTable = (1, 0);
					Offset = new[] { (-1, 1), (0, 0), (1, 1) };
				}
				public PieceV(int rotationTimes) : this() { }

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
				public override bool Discard(Board board)
				{
					bool isFailing = false;
					int CoordX, CoordY;
					for (int i = 0; i < 3; i++)
					{
						for (int j = i - 2; j <= 2 - i; j++)
						{
							CoordX = board.CurrentPieceCenter.CoordX + j;
							CoordY = board.CurrentPieceCenter.CoordY - i;
							if(IsInBoard(CoordX,CoordY))
							{
								board.board[CoordX, CoordY] = this.Color;
							}
							else
							{
								if (CoordY >= Row)
								{
									board.LoseGameEvent();
									isFailing = true;
								}
							}
						}
					}
					return isFailing;
				}
			}
			public class PieceX : Piece
			{
				public PieceX()
				{
					Color = Color.White;
					CenterInFourByFourTable = (1, 1);
					Offset = new[] { (-1, 1), (0, 0), (1, 1), (-1, -1), (1, -1) };
				}
				public PieceX(int rotationTimes) : this() { }

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
				public override bool Discard(Board board)
				{
					bool isFailing = false;
					int CoordX, CoordY;
					for (int i = -2; i < 2; i++)
					{
						for (int j = -2; j <= 2; j++)
						{
							CoordX = board.CurrentPieceCenter.CoordX + j;
							CoordY = board.CurrentPieceCenter.CoordY + i;
							if (IsInBoard(CoordX, CoordY))
							{
								board.board[CoordX, CoordY] = null;
							}
							else
							{
								if (CoordY >= Row)
								{
									board.LoseGameEvent();
									isFailing = true;
								}
							}
						}
					}
					return isFailing;
				}
			}
		}
	}
	public static class TupleExtendMethod
	{
		public static (int, int)  XAdd(this (int,int) self,int addend)
		{
			return (self.Item1 + addend, self.Item2);
		}
		public static (int, int) YAdd(this (int, int) self, int addend)
		{
			return (self.Item1, self.Item2 + addend);
		}
	}
}
