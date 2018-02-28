using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
	class Page
	{
		public Action GotoPreviousPage;
		public event Action<object> PaintEvent;
		public List<StringButton> buttons = new List<StringButton>();
		public StringButton ActiveButton
		{
			get
			{
				if (activeButtonIndex == null)
					return null;
				else
					return buttons[activeButtonIndex.Value];
			}
		}
		public virtual void PaintPage(Graphics graphics, Form f, object arg) {}
		public virtual void Invoke(params object[] args)
		{
			SetNullIndex();
			PaintEvent(null);
		}
		public virtual void KeyHanding(object sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Down:
					NextButton();
					break;
				case Keys.Up:
					PreviousButton();
					break;
				case Keys.Space:
				case Keys.Enter:
					ActiveButton?.ButtonClick();
					break;
				case Keys.Escape:
					GotoPreviousPage();
					break;
				default:
					break;
			}
		}
		public virtual void MouseMoveHanding(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.None)
			{
				for (int i = 0; i < buttons.Count; i++)
				{
					if (buttons[i].Enable && buttons[i].activeRectangle.Contains(e.Location.X, e.Location.Y))
					{
						activeButtonIndex = i;
						PaintEvent(null);
						return;
					}
				}
				SetNullIndex();
			}
		}
		public virtual void MouseDownHanding(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (ActiveButton != null && ActiveButton.activeRectangle.Contains(e.Location.X, e.Location.Y)) 
				{
					ActiveButton.ButtonClick();
				}	
			}
		}
		protected int? activeButtonIndex = null;
		protected int EnableButtonCount { get => buttons.Count(b => b.Enable); }
		protected StringButton PreviousButton()
		{
			if (EnableButtonCount > 0)
			{
				if (activeButtonIndex == null)
				{
					activeButtonIndex = buttons.Count - 1;
				}
				else
				{
					DecIndex();
				}
				while (ActiveButton.Enable == false)
				{
					DecIndex();
				}
				PaintEvent(null);
				return ActiveButton;
			}
			else
				return null;
		}
		protected StringButton NextButton()
		{
			if (EnableButtonCount > 0)
			{
				if (activeButtonIndex == null)
				{
					activeButtonIndex = 0;
				}
				else
				{
					IncIndex();
				}
				while (ActiveButton.Enable == false)
				{
					IncIndex();
				}
				PaintEvent(null);
				return ActiveButton;
			}
			else
				return null;
		}
		protected void DecIndex()
		{
			if (activeButtonIndex != null)
				activeButtonIndex = (buttons.Count + activeButtonIndex - 1) % buttons.Count;
		}
		protected void IncIndex()
		{
			if (activeButtonIndex != null)
				activeButtonIndex = (buttons.Count + activeButtonIndex + 1) % buttons.Count;
		}
		protected void SetNullIndex()
		{
			activeButtonIndex = null;
			PaintEvent(null);
		}
		protected void PaintEventPublisher(object arg)
		{
			PaintEvent(arg);
		}
	}

	class MainPage : Page
	{
		bool CanLoad
		{
			set => buttons[0].Enable = value;
		}

		public MainPage(Action @continue, Action newGame, Action setting, Action rank)
		{
			Font font = new Font("Arial Black", 15);
			buttons.Add(new StringButton(155, 344, 102, 20, "Continue", font, 155, 340, @continue));
			buttons.Add(new StringButton(143, 384, 124, 20, "New Game", font, 143, 380, newGame));
			buttons.Add(new StringButton(162, 424, 83, 20, "Setting", font, 162, 420, setting) { Enable = false});
			buttons.Add(new StringButton(174, 464, 60, 20, "Rank", font, 174, 460, rank) { Enable = false });
		}
		public override void PaintPage(Graphics graphics, Form f, object arg)
		{
			TetrisForm form = (TetrisForm)f;
			graphics.DrawString("Tetris", new Font("Arial Black", 65), Brushes.Black, 55, 60);
			buttons.ForEach(b => { if (b.Enable) b.Draw(graphics, b.Equals(ActiveButton)); });
		}
		public override void Invoke(params object[] args)
		{
			try
			{
				using (FileStream fs = new FileStream("loadable", FileMode.Open))
				{
					BinaryFormatter bf = new BinaryFormatter();
					CanLoad = (bool)bf.Deserialize(fs);
				}
			}
			catch (Exception)
			{
				CanLoad = false;
			}
			base.Invoke(args);
		}
	}

	class LevelSelectionPage : Page
	{

		public LevelSelectionPage(Action easy, Action medium, Action hard)
		{
			Font font = new Font("Arial Black", 15);
			buttons.Add(new StringButton(181, 344, 56, 20, "Easy", font, 180, 340, easy));
			buttons.Add(new StringButton(163, 384, 88, 20, "Medium", font, 162, 380, medium));
			buttons.Add(new StringButton(181, 424, 56, 20, "Hard", font, 180, 420, hard));
		}
		public override void PaintPage(Graphics graphics, Form f, object arg)
		{
			TetrisForm form = (TetrisForm)f;
			graphics.DrawString("Tetris", new Font("Arial Black", 65), Brushes.Black, 55, 60);
			buttons.ForEach(b => { if (b.Enable) b.Draw(graphics, b.Equals(ActiveButton)); });
		}
	}

	class GamingPage : Page
	{
		public const int SquarePixelWidth = 24;
		public enum Mod
		{
			Easy,
			Medium,
			Hard,
			Load,
		}

		public TetrisGame game;

		public GamingPage(Action toMain)
		{
			StringButton ExitButton = new StringButton(321, 593, 40, 17, "Exit", new Font("Arial Black", 12), 320, 590, null);
			StringButton PauseButton = new StringButton(301, 563, 82, 17, "   Start", new Font("Arial Black", 12), 300, 560, null);
			StringButton YesButton = new StringButton(106, 278, 36, 17, "Yes", new Font("Arial Black", 12), 105, 275, null) { Enable = false };
			StringButton NoButton = new StringButton(191, 278, 27, 17, "No", new Font("Arial Black", 12), 190, 275, null) { Enable = false };
			StringButton CancelButton = new StringButton(251, 278, 66, 17, "Cancel", new Font("Arial Black", 12), 250, 275, null) { Enable = false };
			buttons.Add(PauseButton);
			buttons.Add(ExitButton);
			buttons.Add(YesButton);
			buttons.Add(NoButton);
			buttons.Add(CancelButton);
			isExitBoxVisible = false;
			ToMain = toMain;
		}
		public override void PaintPage(Graphics graphics, Form f, object arg)
		{
			TetrisForm form = (TetrisForm)f;
			Font defaultFont = new Font("Arial Black", 10);
			PaintTetris((TetrisGame)arg);
			if (isExitBoxVisible) PaintExitBox();
			buttons.GetRange(0, 2).ForEach(b => { b.Draw(graphics, b.Equals(ActiveButton)); });

			void PaintTetris(TetrisGame game)
			{
				//游戏区域
				graphics.FillRectangle(Brushes.DimGray, 20, 20, 250, 610);
				PaintFrame(20, 20, 250, 610);
				PaintStrip(Brushes.PaleGoldenrod, Brushes.SeaShell, 25, 25, 600, 10);
				//画方块
				Color?[,] board = game.GetBoard();
				for (int X = 0; X < game.Column; X++)
				{
					for (int Y = 0; Y < game.Row; Y++)
					{
						if (board[X, Y] != null)
						{
							PaintPieceSquare(board[X, Y].Value, 25 + X * SquarePixelWidth, 25 + (game.Row - Y - 1) * SquarePixelWidth);
						}
					}
				}
				//下一块
				graphics.DrawString("Next Piece:", defaultFont, Brushes.Black, 290, 20);
				PaintFrame(290, 40, 106, 106);
				PaintStrip(Brushes.LightGray, Brushes.WhiteSmoke, 295, 45, 96, 4);
				//下一块方块
				Color?[,] nextPieceBoard = game.GetNextPieceBoard();
				for (int X = 0; X < 4; X++)
				{
					for (int Y = 0; Y < 4; Y++)
					{
						if (nextPieceBoard[X, Y] != null)
						{
							PaintPieceSquare(nextPieceBoard[X, Y].Value, 295 + X * SquarePixelWidth, 45 + (4 - Y - 1) * SquarePixelWidth);
						}
					}
				}
				//其他文字信息
				graphics.DrawString("Score:", defaultFont, Brushes.Black, 290, 170);
				graphics.DrawString($"{game.Score,8}", defaultFont, Brushes.Black, 290, 190);
				graphics.DrawString("Line:", defaultFont, Brushes.Black, 290, 230);
				graphics.DrawString($"{game.EliminatedLine,8}", defaultFont, Brushes.Black, 290, 250);
				graphics.DrawString("Mod:", defaultFont, Brushes.Black, 290, 290);
				graphics.DrawString($"{game.Level,8}", defaultFont, Brushes.Black, 290, 310);
				graphics.DrawString("Time:", defaultFont, Brushes.Black, 290, 350);
				graphics.DrawString($"{game.PlayingTime,12}", defaultFont, Brushes.Black, 290, 370);
				graphics.DrawString("Time:", defaultFont, Brushes.Black, 290, 350);
				graphics.DrawString($"{game.PlayingTime,12}", defaultFont, Brushes.Black, 290, 370);
				graphics.DrawString("State:", defaultFont, Brushes.Black, 290, 410);
				graphics.DrawString($"{game.State,12}", defaultFont, Brushes.Black, 290, 430);

				void PaintPieceSquare(Color color, int PixelX, int PixelY, int width = SquarePixelWidth, int height = SquarePixelWidth)
				{
					graphics.FillRectangle(new SolidBrush(color), PixelX, PixelY, width, height);
					graphics.DrawLine(new Pen(Color.FromArgb(63, 255, 255, 255), 1), PixelX + width, PixelY - 1, PixelX + width - 4, PixelY + 3);
					graphics.DrawLine(new Pen(Color.FromArgb(95, 127, 127, 127), 1), PixelX, PixelY, PixelX + 4, PixelY + 4);
					graphics.DrawLine(new Pen(Color.FromArgb(95, 127, 127, 127), 1), PixelX + width - 1, PixelY + height - 1, PixelX + width - 4, PixelY + height - 4);
					graphics.DrawLine(new Pen(Color.FromArgb(63, 15, 15, 15), 1), PixelX, PixelY + height - 1, PixelX + 4, PixelY + height - 5);
					graphics.FillRectangle(
						new LinearGradientBrush(
							new Point(PixelX + SquarePixelWidth, PixelY),
							new Point(PixelX, PixelY + SquarePixelWidth),
							Color.FromArgb(0x7F, 0xFF, 0xFF, 0xFF),
							Color.FromArgb(0x7F, 0x00, 0x00, 0x00)
							),
						PixelX, PixelY, width, height);
					graphics.FillRectangle(new SolidBrush(Color.FromArgb(63, color)), PixelX + 4, PixelY + 4, width - 8, height - 8);
				}
				
				void PaintStrip(Brush firstBrush, Brush secondBrush, int x, int y, int height, int stripNumber, int width = SquarePixelWidth)
				{
					for (int i = 0; i < stripNumber; i++)
					{
						if (i % 2 == 0)
							graphics.FillRectangle(firstBrush, x + i * width, y, width, height);
						else
							graphics.FillRectangle(secondBrush, x + i * width, y, width, height);
					}
				}

			}
			void PaintFrame(int x, int y, int width, int height)
			{
				graphics.FillRectangle(Brushes.DimGray, x, y, width, height);
				graphics.FillRectangle(Brushes.BurlyWood, x + 5, y + 5, width - 10, height - 10);
				graphics.DrawLines(Pens.White, new Point[] { new Point { X = x + 2, Y = y + 1 }, new Point { X = x + width - 2, Y = y + 1 }, new Point { X = x + width - 2, Y = y + height - 3 } });
				graphics.DrawLines(Pens.Black, new Point[] { new Point { X = x + 4, Y = y + 4 }, new Point { X = x + width - 5, Y = y + 4 }, new Point { X = x + width - 5, Y = y + height - 5 } });
				graphics.DrawLines(Pens.Black, new Point[] { new Point { X = x, Y = y + 1 }, new Point { X = x, Y = y + height - 1 }, new Point { X = x + width - 2, Y = y + height - 1 } });
				graphics.DrawLines(Pens.White, new Point[] { new Point { X = x + 3, Y = y + 5 }, new Point { X = x + 3, Y = y + height - 4 }, new Point { X = x + width - 6, Y = y + height - 4 } });
			}
			void PaintExitBox()
			{
				PaintFrame(60, 220, 300, 100);
				graphics.DrawString("Save the current game?", new Font("Arial Black", 15), Brushes.Black, 80, 230);
				buttons.GetRange(2, 3).ForEach(b => { b.Draw(graphics, b.Equals(ActiveButton)); });
			}
		}
		public override void Invoke(params object[] args)
		{
			Mod mod = (Mod)args[0];
			switch (mod)
			{
				case Mod.Easy:
					game = TetrisGame.Initialize(TetrisGame.Difficulty.Easy, (s, e) => { base.PaintEventPublisher(s); }, (s, e) => { LoseGame(); });
					buttons[0].content = "   Start";
					break;
				case Mod.Medium:
					game = TetrisGame.Initialize(TetrisGame.Difficulty.Medium, (s, e) => { base.PaintEventPublisher(s); }, (s, e) => { LoseGame(); });
					buttons[0].content = "   Start";
					break;
				case Mod.Hard:
					game = TetrisGame.Initialize(TetrisGame.Difficulty.Hard, (s, e) => { base.PaintEventPublisher(s); }, (s, e) => { LoseGame(); });
					buttons[0].content = "   Start";
					break;
				case Mod.Load:
					game = TetrisGame.Load("data", (s, e) => { base.PaintEventPublisher(s); }, (s, e) => { LoseGame(); });
					buttons[0].content = "Continue";
					using (FileStream fs = new FileStream("loadable", FileMode.Create))
					{
						BinaryFormatter bf = new BinaryFormatter();
						bf.Serialize(fs, false);
					}
					break;
				default:
					break;
			}
			buttons[0].ButtonClick = SwitchButtonFunction;
			buttons[1].ButtonClick = ExitButtonClick;
			buttons[2].ButtonClick = SaveAndExit;
			buttons[3].ButtonClick = ToMain;
			buttons[4].ButtonClick = CloseExitBox;
			isGameChanged = false;
			CloseExitBox();
			SetNullIndex();
		}
		public override void KeyHanding(object sender, KeyEventArgs e)
		{
			if(isExitBoxVisible == false)
			{
				switch (game.State)
				{
					case TetrisGame.States.Ready:
						switch (e.KeyData)
						{
							case Keys.Space:
							case Keys.P:
								buttons[0].content = "Continue";
								isGameChanged = true;
								game.Start();
								SetNullIndex();
								break;
							case Keys.Up:
								PreviousButton();
								break;
							case Keys.Down:
								NextButton();
								break;
							case Keys.Escape:
								if (ActiveButton != null) SetNullIndex();
								else Exit();
								break;
							case Keys.Enter:
								ActiveButton?.ButtonClick();
								SetNullIndex();
								break;
							default:
								break;
						}
						break;
					case TetrisGame.States.Paused:
						switch (e.KeyData)
						{
							case Keys.Space:
							case Keys.P:
								buttons[0].content = "  Pause";
								isGameChanged = true;
								game.Continue();
								SetNullIndex();
								break;
							case Keys.Up:
								PreviousButton();
								break;
							case Keys.Down:
								NextButton();
								break;
							case Keys.Escape:
								if (ActiveButton != null) SetNullIndex();
								else
								{
									if (isGameChanged) OpenExitBox();
									else Exit();
								}
								break;
							case Keys.Enter:
								ActiveButton?.ButtonClick();
								SetNullIndex();
								break;
							default:
								break;
						}
						break;
					case TetrisGame.States.Playing:
						switch (e.KeyData)
						{
							case Keys.Space:
							case Keys.P:
								buttons[0].content = "Continue";
								game.Pause();
								SetNullIndex();
								break;
							case Keys.Left:
								game.MoveLeft();
								break;
							case Keys.Right:
								game.MoveRight();
								break;
							case Keys.Down:
								game.FallToBottom();
								break;
							case Keys.Up:
								game.ClockwiseRotate();
								break;
							case Keys.Z:
								game.AntiClockwiseRotate();
								break;
							case Keys.X:
								game.ClockwiseRotate();
								break;
							case Keys.Escape:
								buttons[0].content = "Continue";
								game.Pause();
								OpenExitBox();
								break;
							default:
								break;
						}
						break;
					case TetrisGame.States.Losing:
						switch (e.KeyData)
						{
							case Keys.Space:
							case Keys.P:
								buttons[0].content = " Pause";
								isGameChanged = true;
								game.Restart();
								SetNullIndex();
								break;
							case Keys.Up:
								PreviousButton();
								break;
							case Keys.Down:
								NextButton();
								break;
							case Keys.Escape:
								if (ActiveButton != null) SetNullIndex();
								else Exit();
								break;
							case Keys.Enter:
								ActiveButton?.ButtonClick();
								SetNullIndex();
								break;
							default:
								break;
						}
						break;
					case TetrisGame.States.Abort:
						break;
					default:
						break;
				}
			}
			else
			{
				switch (e.KeyData)
				{
					case Keys.Right:
						NextButton();
						break;
					case Keys.Left:
						PreviousButton();
						break;
					case Keys.Escape:
						buttons[4].ButtonClick();
						PaintEventPublisher(game);
						break;
					case Keys.Space:
					case Keys.Enter:
						ActiveButton?.ButtonClick();
						break;
					default:
						break;
				}
			}
		}
		public override void MouseMoveHanding(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.None)
			{
				for (int i = 0; i < buttons.Count; i++)
				{
					if (buttons[i].Enable && buttons[i].activeRectangle.Contains(e.Location.X, e.Location.Y))
					{
						activeButtonIndex = i;
						PaintEventPublisher(game);
						return;
					}
				}
				SetNullIndex();
			}
		}
		public override void MouseDownHanding(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (ActiveButton.activeRectangle.Contains(e.Location.X, e.Location.Y))
				{
					ActiveButton.ButtonClick();
				}
			}
		}

		private Action ToMain;
		new private StringButton PreviousButton()
		{
			if (EnableButtonCount > 0)
			{
				if (activeButtonIndex == null)
				{
					activeButtonIndex = buttons.Count - 1;
				}
				else
				{
					DecIndex();
				}
				while (ActiveButton.Enable == false)
				{
					DecIndex();
				}
				PaintEventPublisher(game);
				return ActiveButton;
			}
			else
				return null;
		}
		new private StringButton NextButton()
		{
			if (EnableButtonCount > 0)
			{
				if (activeButtonIndex == null)
				{
					activeButtonIndex = 0;
				}
				else
				{
					IncIndex();
				}
				while (ActiveButton.Enable == false)
				{
					IncIndex();
				}
				PaintEventPublisher(game);
				return ActiveButton;
			}
			else
				return null;
		}
		new private void SetNullIndex()
		{
			activeButtonIndex = null;
			PaintEventPublisher(game);
		}
		private bool isGameChanged;
		private bool isExitBoxVisible;
		private void OpenExitBox()
		{
			isExitBoxVisible = true;
			SetNullIndex();
			buttons[0].Enable = false;
			buttons[1].Enable = false;
			buttons[2].Enable = true;
			buttons[3].Enable = true;
			buttons[4].Enable = true;
		}
		private void CloseExitBox()
		{
			isExitBoxVisible = false;
			SetNullIndex();
			buttons[0].Enable = true;
			buttons[1].Enable = true;
			buttons[2].Enable = false;
			buttons[3].Enable = false;
			buttons[4].Enable = false;
		}
		private void SwitchButtonFunction()
		{
			//if (game == null) throw new NullReferenceException();
			if (game.State == TetrisGame.States.Paused)
			{
				buttons[0].content = "  Pause";
				game.Continue();
			}
			else if (game.State == TetrisGame.States.Playing)
			{
				buttons[0].content = "Continue";
				game.Pause();
			}
			else if (game.State == TetrisGame.States.Losing)
			{
				buttons[0].content = "  Pause";
				game.Restart();
			}
			else if (game.State == TetrisGame.States.Ready)
			{
				buttons[0].content = "  Pause";
				game.Start();
			}
		}
		private void SaveAndExit()
		{
			game.Save("data");
			using (FileStream fs = new FileStream("loadable", FileMode.Create))
			{
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(fs, true);
			}
			game.Exit();
			ToMain();
		}
		private void ExitButtonClick()
		{
			if (game.State == TetrisGame.States.Ready||game.State == TetrisGame.States.Losing)
			{
				Exit();
			}
			else
			{
				OpenExitBox();
			}
		}
		private void Exit()
		{
			game.Exit();
			ToMain();
		}
		private void LoseGame()
		{
			buttons[0].content = " Restart";
			isGameChanged = false;
			PaintEventPublisher(game);
		}
	}

	class Setting : Page
	{

	}

	class RankPage : Page
	{

	}

	class StringButton
	{
		public bool Enable { get; set; } = true;
		public Rectangle activeRectangle;
		public string content;
		public Font font;
		public Point drawStringPoint;

		public StringButton(Rectangle activeRectangle, string content, Font font, Point drawStringPoint, Action buttonClock)
		{
			this.activeRectangle = activeRectangle;
			this.content = content;
			this.font = font;
			this.drawStringPoint = drawStringPoint;
			ButtonClick += buttonClock;
		}
		public StringButton(int x, int y, int width, int height, string content, Font font, int drawPointX, int drawPointY, Action buttonClock)
		{
			this.activeRectangle = new Rectangle(x, y, width, height);
			this.content = content;
			this.font = font;
			this.drawStringPoint = new Point(drawPointX, drawPointY);
			ButtonClick += buttonClock;
		}

		public Action ButtonClick;
		public void Draw(Graphics graphics, bool isSelected)
		{
			Brush brush = isSelected ? Brushes.OrangeRed : Brushes.Black;
			graphics.DrawString(content, font, brush, drawStringPoint);
		}
	}

	class MyClass
	{

	}
}
