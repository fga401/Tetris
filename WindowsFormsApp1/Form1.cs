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
		private int pageIndex;
		private Page CurrentPage
		{
			get => pages[pageIndex];
		}
		private List<Page> pages;
		#region IndexJumpFunctions
		void ExitApp()
		{
			Environment.Exit(0);
		}
		void ToMain()
		{
			pageIndex = 0;
			CurrentPage.Invoke(null);
		}
		void ToLevelSelection()
		{
			pageIndex = 1;
			CurrentPage.Invoke(null);
		}
		void ToGamingEasy()
		{
			pageIndex = 2;
			CurrentPage.Invoke(GamingPage.Mod.Easy);
		}
		void ToGamingMedium()
		{
			pageIndex = 2;
			CurrentPage.Invoke(GamingPage.Mod.Medium);
		}
		void ToGamingHard()
		{
			pageIndex = 2;
			CurrentPage.Invoke(GamingPage.Mod.Hard);
		}
		void ToGamingLoad()
		{
			pageIndex = 2;
			CurrentPage.Invoke(GamingPage.Mod.Load);
		}
		void ToSetting()
		{
			throw new NotSupportedException();
		}
		void ToRank()
		{
			throw new NotSupportedException();
		}
		#endregion

		public TetrisForm()
		{
			InitializeComponent();
			pages = new List<Page>
			{
			new MainPage(ToGamingLoad, ToLevelSelection, ToSetting, ToRank) { GotoPreviousPage = ExitApp },
			new LevelSelectionPage(ToGamingEasy, ToGamingMedium, ToGamingHard) { GotoPreviousPage = ToMain },
			new GamingPage(ToMain) { GotoPreviousPage = ToMain},
			};
			pageIndex = 0;
			foreach (var page in pages)
			{
				page.PaintEvent += DoubleBufferPaintPage;
			}
		}

		private void DoubleBufferPaintPage(object arg)
		{
			BufferedGraphicsContext context = BufferedGraphicsManager.Current;
			using (BufferedGraphics bufferedGraphics = context.Allocate(this.CreateGraphics(), this.DisplayRectangle))
			{
				Graphics graphics = bufferedGraphics.Graphics;
				graphics.FillRectangle(new SolidBrush(Color.BurlyWood), this.DisplayRectangle);
				CurrentPage.PaintPage(graphics, this, arg);
				bufferedGraphics.Render();
			}
		}
#if DEBUG
		private void PaintTest(Graphics graphics)
		{
			graphics.FillRectangle(new SolidBrush(Color.BurlyWood), this.DisplayRectangle);
			graphics.DrawString("Tetris", new Font("Arial Black", 65), Brushes.Black, 55, 60);
			graphics.DrawRectangle(Pens.Red, 65, 90, 290, 70);
			graphics.DrawString("Easy", new Font("Arial Black", 15), Brushes.Black, 180, 340);
			graphics.DrawRectangle(Pens.Red, 181, 344, 56, 20);
			graphics.DrawString("Medium", new Font("Arial Black", 15), Brushes.Black, 162, 380);
			graphics.DrawRectangle(Pens.Red, 163, 384, 88, 20);
			graphics.DrawString("Hard", new Font("Arial Black", 15), Brushes.Black, 180, 420);
			graphics.DrawRectangle(Pens.Red, 181, 424, 56, 20);
			//graphics.DrawString("Rank", new Font("Arial Black", 15), Brushes.Black, 174, 460);
			//graphics.DrawRectangle(Pens.Red, 174, 464, 60, 20);
		}
#endif


		private void LoseGame(object sender,EventArgs e)
		{
			
		}

		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			CurrentPage.KeyHanding(sender,e);
			/*switch (e.KeyData)
			{
				case Keys.W:
					PaintTest(this.CreateGraphics());
					break;
				case Keys.Q:
					DoubleBufferPaintPage();
					break;
				default:
					break;
			}*/
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
			CurrentPage.Invoke(null);
		}

		private void TetrisForm_MouseMove(object sender, MouseEventArgs e)
		{
			CurrentPage.MouseMoveHanding(sender, e);
		}

		private void TetrisForm_MouseDown(object sender, MouseEventArgs e)
		{
			CurrentPage.MouseDownHanding(sender, e);
		}
	}

	
}
