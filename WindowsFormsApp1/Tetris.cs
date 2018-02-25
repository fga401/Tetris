using System;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;

namespace WindowsFormsApp1
{
	[Serializable]
	public sealed class TetrisGame : IDisposable, ISerializable
	{
		public event EventHandler PaintEvent;
		public event EventHandler LoseGameEvent;
		public int Row { get => Board.Row; }
		public int Column { get => Board.Column; }
		public int Score { get; private set; } = 0;
		public int Level { get => Math.Min(Score / 120 + 1, 8); }
		public int EliminatedLine { get; private set; } = 0;
		private DateTime playingTime { get; set; }
		public string PlayingTime {get => $"{playingTime.Hour.ToString().PadLeft(2,'0')}:{playingTime.Minute.ToString().PadLeft(2, '0')}:{playingTime.Second.ToString().PadLeft(2, '0')}"; }
        public States State;
        public enum States
        {
            Ready,
            Paused,
            Playing,
            Losing,
        }
		public bool hasEliminatedRow;

		private Board board = new Board();
		private const int MaxTimeInterval = 2000;
		private int millisecondClock = 1;
		private int MillisecondClock
		{
			get => millisecondClock;
			set
			{
				millisecondClock = value % MaxTimeInterval;
			}
		}
		private Thread fallingThread;
		private Thread timingThread;
		private TetrisGame()
		{
			board.LineEliminateEvent += LineEliminate;
			board.IncreaseScoreEvent += IncreaseScore;
			board.LoseGameEvent += LoseGame;
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
			if (State == States.Playing && board.ClockwiseRotate())
			{
				PaintEvent(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void AntiClockwiseRotate()
		{
			if (State == States.Playing && board.AntiClockwiseRotate())
			{
				PaintEvent(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void FallToBottom()
		{
			Console.WriteLine("Call FallToBottom()");
			if (State == States.Playing)
			{
				Console.WriteLine("Reach Line 78");
				while (board.Falling()) { continue; }
				Console.WriteLine("Reach Line 80");
				if (State == States.Playing) PaintEvent(this, null);
				Console.WriteLine("Reach Line 82");
				if (!hasEliminatedRow)
				{
					PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
				}
				else
				{
					hasEliminatedRow = false;
				}
				Console.WriteLine("Reach Line 91");
			}
		}
		public void MoveLeft()
		{
			if (State == States.Playing && board.MoveLeft(1))
			{
				PaintEvent(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void MoveRight()
		{
			if (State == States.Playing && board.MoveRight(1))
			{
				PaintEvent(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public static TetrisGame Initialize(int Level, EventHandler paintEvent, EventHandler loseGameEvent)
		{
			TetrisGame game = new TetrisGame();
			game.Score = 0;
			game.EliminatedLine = 0;
			game.playingTime = new DateTime(1, 1, 1, 0, 0, 0);
			game.State = States.Ready;
			game.board.Initialize();
			return game;
		}
        public static TetrisGame Load(string path, EventHandler paintEvent, EventHandler loseGameEvent)
		{
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			TetrisGame game;
			using (FileStream fs = new FileStream(path, FileMode.Open))
			{
				game = (TetrisGame)binaryFormatter.Deserialize(fs);
			}
			game.State = States.Paused;
			return game;
		}
		public void Start()
		{
			if(State == States.Ready)
			{
				State = States.Playing;
				fallingThread = new Thread(FallingThread);
				timingThread = new Thread(TimingThread);
				fallingThread.Start();
				timingThread.Start();
				board.Start();
				PaintEvent(this, null);
			}
		}
		public void Pause()
		{
			if(State == States.Playing)
			{
				fallingThread.Abort();
				timingThread.Abort();
				State = States.Paused;
				PaintEvent(this, null);
			}
		}
		public void Continue()
		{
			if(State==States.Paused)
			{
				State = States.Playing;
				fallingThread = new Thread(FallingThread);
				timingThread = new Thread(TimingThread);
				fallingThread.Start();
				timingThread.Start();
				PaintEvent(this, null);
			}
		}
		public void Restart()
		{
			if(State == States.Losing)
			{
				board.Initialize();
				board.Start();
				State = States.Playing;
				Score = 0;
				EliminatedLine = 0;
				playingTime = new DateTime(1, 1, 1, 0, 0, 0);
				fallingThread = new Thread(FallingThread);
				timingThread = new Thread(TimingThread);
				fallingThread.Start();
				timingThread.Start();
				PaintEvent(this, null);
			}
		}
		public void Save(string path)
		{
			if (path == null) throw new ArgumentNullException();
			if(State == States.Playing)
			{
				State = States.Paused;
			}
			if(State == States.Paused)
			{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				using (FileStream fs = new FileStream(path, FileMode.Create))
				{
					binaryFormatter.Serialize(fs, this);
				}
			}
		}
		public void Exit() { throw new NotImplementedException(); }
		public void Dispose()
		{
			fallingThread?.Abort();
			timingThread?.Abort();
		}

		private void PlaySoundEffect(Stream stream)
		{
			SoundPlayer player = new SoundPlayer(stream);
			player.Play();
		}
		#region FallingThreadFunctions
        private void FallingThread()
        {
            while (State == States.Playing)
            {
				Thread.Sleep(500 - 50 * Level);
				//Thread.Sleep(50);
				if (State == States.Playing) Falling();    
            }
        }
        private void Falling()
		{
			if (board.Falling())
			{
				PaintEvent(this, null);
			}
		}
		#endregion
		#region TimingThreadFunctions
		private void TimingThread()
		{
			while(State == States.Playing)
			{
				Thread.Sleep(1000);
				playingTime = playingTime.AddSeconds(1);
			}		
		}
		#endregion
		private void LineEliminate(object sender, int increment)
		{
			EliminatedLine += increment;
			PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Eliminate);
			hasEliminatedRow = true;
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
			fallingThread.Abort();
			timingThread.Abort();
			State = States.Losing;
			LoseGameEvent(this, null);
		}

		#region ISerializable
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Score", Score);
			info.AddValue("EliminatedLine", EliminatedLine);
			info.AddValue("PlayingTime", playingTime);
			info.AddValue("Board", board);
		}
		public TetrisGame(SerializationInfo info, StreamingContext context)
		{
			Score = info.GetInt32("Score");
			EliminatedLine = info.GetInt32("EliminatedLine");
			playingTime = info.GetDateTime("PlayingTime");
			board = (Board)info.GetValue("Board",typeof(Board));
			board.LineEliminateEvent += LineEliminate;
			board.IncreaseScoreEvent += IncreaseScore;
			board.LoseGameEvent += LoseGame;
		}
		#endregion

		[Serializable]
		private sealed class Board : ISerializable
		{
			public const int Row = 25;
			public const int Column = 10;
			public event EventHandler<int> LineEliminateEvent;
			public event EventHandler<int> IncreaseScoreEvent;
			public event Action LoseGameEvent;
			public Color?[,] board;
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
					bool isFalling = CurrentPiece.Discard(this) &&
						CurrentPiece.Offset.Max(t => t.OffSetY) + CurrentPieceCenter.CoordY >= Row;
					Console.WriteLine(isFalling);
					if (!isFalling)
					{
						Eliminate();
						ChangeCurrentPiece();
					}
					else
					{
						LoseGameEvent();
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
			public void Initialize()
			{
				board = new Color?[Column, Row];
				CanSpecialPieceGenerate = false;
				NextPiece = CreatePiece(RandomGenerator.Next());
				ChangeCurrentPiece();
			}
			public void Start()
			{
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
				if (count > 0)
				{
					LineEliminateEvent(this, count);
					IncreaseScoreEvent(this, Column * count);
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
			private Piece CreatePiece(int t)
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
				NextPiece = CreatePiece(RandomGenerator.Next());
				//NextPiece = CreatePiece((int)Piece.PieceType.ShapeV);
			}
			IEnumerable<int> TryMoveRightOffset(bool isMoveLeftFirst)
			{
				int coefficient = isMoveLeftFirst ? -1 : 1;
				yield return 1 * coefficient;
				yield return 2 * coefficient;
				yield return -1 * coefficient;
				yield return -2 * coefficient;
			}

			#region ISerializable
			public void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.AddValue("board", board);
				info.AddValue("CanSpecialPieceGenerate", CanSpecialPieceGenerate);
				info.AddValue("NextPiece", NextPiece);
				info.AddValue("CurrentPiece", CurrentPiece);
				info.AddValue("CurrentPieceCenter", CurrentPieceCenter);
			}
			public Board(SerializationInfo info, StreamingContext context)
			{
				board = (Color?[,])info.GetValue("board", typeof(Color?[,]));
				CanSpecialPieceGenerate = info.GetBoolean("CanSpecialPieceGenerate");
				NextPiece = (Piece)info.GetValue("NextPiece", typeof(Piece));
				CurrentPiece = (Piece)info.GetValue("CurrentPiece", typeof(Piece));
				CurrentPieceCenter = ((int,int))info.GetValue("CurrentPieceCenter",typeof((int,int)));
			}
			#endregion
			#region Piece
			[Serializable]
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
			[Serializable]
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

				public PieceI() { Color = Color.OrangeRed; }
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
			[Serializable]
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

				public PieceJ() { Color = Color.DarkViolet; }
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
			[Serializable]
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

				public PieceL() { Color = Color.Gold; }
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
			[Serializable]
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
			[Serializable]
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
			[Serializable]
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
			[Serializable]
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
			[Serializable]
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
									isFailing = true;
								}
							}
						}
					}
					return isFailing;
				}
			}
			[Serializable]
			public class PieceX : Piece
			{
				public PieceX()
				{
					Color = Color.LightSkyBlue;
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
						}
					}
					return false;
				}
			}
			#endregion
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
