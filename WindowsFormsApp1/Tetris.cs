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
using System.Runtime.InteropServices;

namespace WindowsFormsApp1
{
	[Serializable]
	public sealed class TetrisGame : ISerializable
	{
		#region Properties
		public Difficulty difficulty { get; private set; }
		public int Row { get => Board.Row; }
		public int Column { get => Board.Column; }
		public int Score { get; private set; } = 0;
		public int Level { get => Math.Min(Score / 120 + 1, 8); }
		public int EliminatedLine { get; private set; } = 0;
		public string PlayingTime { get => $"{playingTime.Hour.ToString().PadLeft(2, '0')}:{playingTime.Minute.ToString().PadLeft(2, '0')}:{playingTime.Second.ToString().PadLeft(2, '0')}"; }
		public States State
		{
			get => state;
			private set
			{
				States oldState = state;
				StateExitEvent(state);
				state = value;
				StateEntryEvent(state);
				PaintEvent?.Invoke(this, null);
				StateChangeEvent?.Invoke(this, new StateChangeEventArgs(oldState, state));
			}
		}
		#endregion

		public event EventHandler<StateChangeEventArgs> StateChangeEvent;
		public event EventHandler PaintEvent;
		public event EventHandler LoseGameEvent;
		public enum Difficulty
		{
			Easy,
			Medium,
			Hard,
		}	     
        public enum States
        {
            Ready,
            Paused,
            Playing,
            Losing,
			Abort,
        }

		public static TetrisGame Initialize(Difficulty level)
		{
			TetrisGame game = new TetrisGame();
			game.difficulty = level;
			game.Score = 0;
			game.EliminatedLine = 0;
			game.playingTime = new DateTime(1, 1, 1, 0, 0, 0);
			game.board.Initialize(level);
			game.State = States.Ready;
			return game;
		}
		public static TetrisGame Load(string path)
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
				PaintEvent?.Invoke(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void AntiClockwiseRotate()
		{
			if (State == States.Playing && board.AntiClockwiseRotate())
			{
				PaintEvent?.Invoke(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void FallToBottom()
		{
			if (State == States.Playing)
			{
				while (board.Falling()) { continue; }
				if (State == States.Playing) PaintEvent?.Invoke(this, null);
				if (!hasEliminatedRow)
				{
					PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
				}
				else
				{
					hasEliminatedRow = false;
				}
			}
		}
		public void MoveLeft()
		{
			if (State == States.Playing && board.MoveLeft(1))
			{
				PaintEvent?.Invoke(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void MoveRight()
		{
			if (State == States.Playing && board.MoveRight(1))
			{
				PaintEvent?.Invoke(this, null);
				PlaySoundEffect(WindowsFormsApp1.Properties.Resources.Change);
			}
		}
		public void Start()
		{
			if(State == States.Ready)
			{
				State = States.Playing;
				board.Start();
			}
		}
		public void Pause()
		{
			if(State == States.Playing)
			{
				State = States.Paused;
			}
		}
		public void Continue()
		{
			if(State==States.Paused)
			{
				State = States.Playing;
			}
		}
		public void Restart()
		{
			if(State == States.Losing)
			{
				board.Initialize(difficulty);
				board.Start();
				State = States.Playing;
				Score = 0;
				EliminatedLine = 0;
				playingTime = new DateTime(1, 1, 1, 0, 0, 0);
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
		public void Exit()
		{
			State = States.Abort;
		}
		public void Refresh()
		{
			PaintEvent?.Invoke(this, EventArgs.Empty);
		}

		#region ISerializable
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Score", Score);
			info.AddValue("EliminatedLine", EliminatedLine);
			info.AddValue("PlayingTime", playingTime);
			info.AddValue("Board", board);
			info.AddValue("FallTimer", fallTimer);
			info.AddValue("ClockTimer", clockTimer);
			info.AddValue("Difficulty", difficulty);
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
			fallTimer = (MSTimer)info.GetValue("FallTimer", typeof(MSTimer));
			clockTimer = (MSTimer)info.GetValue("ClockTimer", typeof(MSTimer));
			difficulty = (Difficulty)info.GetValue("Difficulty", typeof(Difficulty));
		}
		#endregion

		#region Private
		private TetrisGame()
		{
			fallTimer = new MSTimer(450);
			fallTimer.TimerAction += Falling;
			clockTimer = new MSTimer(1000);
			clockTimer.TimerAction += Timing;
			board = new Board();
			board.LineEliminateEvent += LineEliminate;
			board.IncreaseScoreEvent += IncreaseScore;
			board.LoseGameEvent += LoseGame;
		}

		private DateTime playingTime;
		private States state;

		private Board board;
		private MSTimer fallTimer;
		private MSTimer clockTimer;

		private void StateEntryEvent(States state)
		{
			switch (state)
			{
				case States.Ready:
					break;
				case States.Paused:
					break;
				case States.Playing:
					fallTimer.Start();
					clockTimer.Start();
					break;
				case States.Losing:
					break;
				default:
					break;
			}
		}
		private void StateExitEvent(States state)
		{
			switch (state)
			{
				case States.Paused:
					break;
				case States.Playing:
					fallTimer.Pause();
					clockTimer.Pause();
					break;
				case States.Losing:
					break;
				default:
					break;
				case States.Ready:
					break;
			}
		}

		#region board.EventDelegations
		private bool hasEliminatedRow;

		private void PlaySoundEffect(Stream stream)
		{
			Thread thread = new Thread(() =>
			{
				SoundPlayer player = new SoundPlayer(stream);
				player.Play();
			});
			thread.Start();
		}
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
		private void LoseGame(object sender, EventArgs e)
		{
			State = States.Losing;
			LoseGameEvent?.Invoke(this, null);
		}
		#endregion

		#region MSClock.EventDelegations
		private void Falling(object sender, EventArgs e)
		{
			if (State == States.Playing && board.Falling())
			{
				PaintEvent(this, null);
				fallTimer.Interval = 500 - 50 * Level;
			}
		}
		private void Timing(object sender, EventArgs e)
		{
			if (State == States.Playing)
			{
				playingTime = playingTime.AddSeconds(1);
				PaintEvent(this, null);
			}
		}
		#endregion

		[Serializable]
		private sealed class Board : ISerializable
		{
			public const int Row = 25;
			public const int Column = 10;
			public event EventHandler<int> LineEliminateEvent;
			public event EventHandler<int> IncreaseScoreEvent;
			public event EventHandler LoseGameEvent;
			public Color?[,] board;
			public bool CanSpecialPieceGenerate = false;
			
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
					if (!isFalling)
					{
						Eliminate();
						ChangeCurrentPiece();
					}
					else
					{
						LoseGameEvent(this, EventArgs.Empty);
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
			public void Initialize(Difficulty difficulty)
			{
				board = new Color?[Column, Row];
				CanSpecialPieceGenerate = false;
				NextPiece = CreatePiece(RandomGenerator.Next());
				ChangeCurrentPiece();
				SetInitalPiece(difficulty);
			}
			public void Start()
			{
				ChangeCurrentPiece();
			}

			#region private
			private static Random RandomGenerator = new Random();
			private Piece NextPiece;
			private Piece CurrentPiece;
			private (int CoordX, int CoordY) CurrentPieceCenter;

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
			private void SetInitalPiece(Difficulty difficulty)
			{
				int times;
				int coef = -1;
				switch (difficulty)
				{
					case Difficulty.Easy:
						times = 0;
						break;
					case Difficulty.Medium:
						times = 3;
						break;
					case Difficulty.Hard:
						times = 6;
						break;
					default:
						times = 0;
						break;
				}
				for (int i = 0; i < times; i++)
				{
					bool isRowFinished = false;
					int Cursor = 0;
					while (!isRowFinished)
					{
						int leftOffset = -CurrentPiece.Offset.Min(t => t.OffSetX);
						int RightOffset = CurrentPiece.Offset.Max(t => t.OffSetX);
						while (MoveRight(coef)) ;
						for (int j = 0; j < Cursor; j++)
						{
							MoveRight(-coef);
						}
						while (Falling()) ;
						Cursor += leftOffset + 1 + RightOffset;
						if (Cursor >= Column) isRowFinished = true;
					}
					coef = -1 * coef;
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
			#endregion

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
				CurrentPieceCenter = ((int, int))info.GetValue("CurrentPieceCenter", typeof((int, int)));
			}
			#endregion

			#region ClassPiece
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
							if (IsInBoard(CoordX, CoordY))
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
		[Serializable]
		private class MSTimer : IDisposable
		{
			[DllImport("winmm")]
			static extern void timeBeginPeriod(int t);
			[DllImport("winmm")]
			static extern void timeEndPeriod(int t);

			[NonSerialized] private Thread timer;
			private int time = 1;
			private int interval;
			private const int minTimeUnit = 50;
			public event EventHandler TimerAction;
			public int Time
			{
				get => time;
				set
				{
					time = value % (Interval / minTimeUnit);
				}
			}
			public int Interval
			{
				get => interval;
				set
				{
					if (value > 0) interval = value;
					else throw new ArgumentOutOfRangeException();
				}
			}

			public MSTimer(int interval)
			{
				Interval = interval;
				Time = 1;
			}
			public void Start()
			{
				timer = new Thread(Tick);
				timeBeginPeriod(1);
				timer.Start();
			}
			public void Pause()
			{
				timeEndPeriod(1);
				timer.Abort();
			}
			private void Tick()
			{
				while (true)
				{
					if (Time == 0)
					{
						Thread thread = new Thread(() => { TimerAction(this, EventArgs.Empty); });
						thread.Start();
					}
					Thread.Sleep(minTimeUnit);
					Time = Time + 1;
				}
			}

			public void Dispose()
			{
				if (timer.ThreadState == ThreadState.Running) timer.Abort();
			}
		}
		#endregion

		public class StateChangeEventArgs
		{
			public States oldState;
			public States newState;

			public StateChangeEventArgs(States oldState, States newState)
			{
				this.oldState = oldState;
				this.newState = newState;
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
