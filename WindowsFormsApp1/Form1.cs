using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WindowsFormsApp1
{
	public partial class TetrisForm : Form
	{
		private string currentPageKey;
		private Page CurrentPage { get => pages[currentPageKey]; }
		private Dictionary<string, Page> pages;

		#region IndexJumpFunctions
		void ExitApp()
		{
			Environment.Exit(0);
		}
		void ToMain()
		{
			currentPageKey = "MainPage";
			CurrentPage.Activate(null);
		}
		void ToLevelSelection()
		{
			currentPageKey = "LevelSelectionPage";
			CurrentPage.Activate(null);
		}
		void ToGamingEasy()
		{
			currentPageKey = "GamingPage";
			CurrentPage.Activate(GamingPage.Mod.Easy);
		}
		void ToGamingMedium()
		{
			currentPageKey = "GamingPage";
			CurrentPage.Activate(GamingPage.Mod.Medium);
		}
		void ToGamingHard()
		{
			currentPageKey = "GamingPage";
			CurrentPage.Activate(GamingPage.Mod.Hard);
		}
		void ToGamingLoad()
		{
			currentPageKey = "GamingPage";
			CurrentPage.Activate(GamingPage.Mod.Load);
		}
		void ToSetting()
		{
			throw new NotSupportedException();
		}
		void ToLeaderBoard()
		{
			throw new NotSupportedException();
		}
		#endregion

		public TetrisForm()
		{
			InitializeComponent();
			pages = new Dictionary<string, Page>
			{
				{"MainPage", new MainPage(ExitApp, ToGamingLoad, ToLevelSelection, ToSetting, ToLeaderBoard)},
				{"LevelSelectionPage", new LevelSelectionPage(ToMain, ToGamingEasy, ToGamingMedium, ToGamingHard)},
				{"GamingPage", new GamingPage(null, ToMain)},
				{"LeaderBoardPage", new LeaderBoardPage(ToMain)},
			};
			currentPageKey = "MainPage";
			foreach (var page in pages.Values)
			{
				page.PaintEvent += DoubleBufferPaintPage;
			}
		}

		private void DoubleBufferPaintPage(object sender, EventArgs e)
		{
			BufferedGraphicsContext context = BufferedGraphicsManager.Current;
			using (BufferedGraphics bufferedGraphics = context.Allocate(this.CreateGraphics(), this.DisplayRectangle))
			{
				Graphics graphics = bufferedGraphics.Graphics;
				graphics.FillRectangle(new SolidBrush(Color.BurlyWood), this.DisplayRectangle);
				CurrentPage.PaintPage(graphics, this);
				bufferedGraphics.Render();
			}
		}
#if DEBUG
		private void PaintTest(Graphics graphics)
		{
			graphics.FillRectangle(new SolidBrush(Color.BurlyWood), this.DisplayRectangle);
			graphics.DrawString("LeaderBoard", new Font("Arial Black", 25), Brushes.Black, 85, 25);
			graphics.DrawLine(new Pen(Color.Black, 4), 40, 125, 380, 125);
			graphics.DrawString("Medium", new Font("Arial Black", 12), Brushes.Black, 40, 135);
			graphics.DrawString("Hard", new Font("Arial Black", 12), Brushes.Black, 40, 175);
			graphics.DrawString("Hard", new Font("Arial Black", 12), Brushes.Black, 40, 495);
			graphics.DrawString("4560", new Font("Arial Black", 12), Brushes.Black, 140, 135);
			graphics.DrawString("7890", new Font("Arial Black", 12), Brushes.Black, 140, 175);
			graphics.DrawString("7890", new Font("Arial Black", 12), Brushes.Black, 140, 495);
			graphics.DrawString("Tester", new Font("Arial Black", 12), Brushes.Black, 270, 135);
			graphics.DrawLine(new Pen(Color.Black, 4), 40, 530, 380, 530);

			graphics.DrawString("Easy", new Font("Arial Black", 12), Brushes.Black, 70, 85);
			graphics.DrawRectangle(Pens.Red, 71, 87, 47, 18);
			graphics.DrawString("Medium", new Font("Arial Black", 12), Brushes.Black, 170, 85);
			graphics.DrawRectangle(Pens.Red, 171, 87, 72, 18);
			graphics.DrawString("Hard", new Font("Arial Black", 12), Brushes.Black, 290, 85);
			graphics.DrawRectangle(Pens.Red, 291, 87, 46, 18);
			graphics.DrawString("Previous", new Font("Arial Black", 12), Brushes.Black, 50, 550);
			graphics.DrawRectangle(Pens.Red, 51, 552, 80, 18);
			graphics.DrawString("Next", new Font("Arial Black", 12), Brushes.Black, 310, 550);
			graphics.DrawRectangle(Pens.Red, 311, 552, 46, 18);

		}
#endif

		private void LoseGame(object sender,EventArgs e)
		{
			
		}

		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			CurrentPage.KeyHandler(sender,e);
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
#if DEBUG
			Program.ConsoleGame.FreeConsole();
#endif
			Environment.Exit(0);
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			//DoubleBufferPaint(sender, e);
		}

		private void TetrisForm_Shown(object sender, EventArgs e)
		{
			//PaintTest(CreateGraphics());
			CurrentPage.Activate(null);
		}

		private void TetrisForm_MouseMove(object sender, MouseEventArgs e)
		{
			CurrentPage.MouseMoveHandler(sender, e);
		}

		private void TetrisForm_MouseDown(object sender, MouseEventArgs e)
		{
			CurrentPage.MouseDownHandler(sender, e);
		}
	}

	
}
