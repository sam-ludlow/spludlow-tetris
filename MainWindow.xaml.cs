using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Spludlow.Tetris;

namespace SpludlowTetris
{
	/// <summary>
	/// Spludlow Tetris Main Playing Window
	/// </summary>
	public partial class MainWindow : Window
	{
		public TetrisSettings _TetrisSettings = null;

		public TetrisServer _TetrisServer = null;
		public TetrisClient _TetrisClient = null;

		private TetrisAudio _TetrisAudio = null;
		private TetrisInput _TetrisInput = null;

		private bool _BoardSetup = false;

		private SolidColorBrush[] _SolidColorBrushs;

		private static Color _BackGroundColour = Colors.Black;

		private static double _DimPlaced = 0.8;

		private TetrisBot _TetrisBot = new TetrisBot();
		private TetrisBotFarm _TetrisBotFarm = new TetrisBotFarm();

		private List<int> _Keys;

		public MainWindow()
		{
			InitializeComponent();

			this.Loaded += MainWindow_Loaded;
			this.Closed += MainWindow_Closed;
			this.SizeChanged += MainWindow_SizeChanged;
			this.StateChanged += MainWindow_StateChanged;
			this.KeyDown += MainWindow_KeyDown;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			this._TetrisSettings = new TetrisSettings();

			this.Width = this._TetrisSettings.State.WindowWidth;
			this.Height = this._TetrisSettings.State.WindowHeight;

			if (this._TetrisSettings.State.WindowX != -1)
			{
				this.WindowStartupLocation = WindowStartupLocation.Manual;
				this.Left = this._TetrisSettings.State.WindowX;
				this.Top = this._TetrisSettings.State.WindowY;
			}

			this.ReadKeysSettings();

			//	Colours		0	1-7(-1) 8:gery 9-15(-9)
			_SolidColorBrushs = new SolidColorBrush[16];
			_SolidColorBrushs[0] = new SolidColorBrush(_BackGroundColour);
			_SolidColorBrushs[8] = new SolidColorBrush(Colors.Gray);

			for (int index = 0; index < 7; ++index)
			{
				double angle = index * (360.0 / 7);
				_SolidColorBrushs[index + 1] = new SolidColorBrush(Spludlow.Media.Colour.FromHSV(angle, 1, _DimPlaced));
				_SolidColorBrushs[index + 1 + 8] = new SolidColorBrush(Spludlow.Media.Colour.FromHSV(angle, 1, 1));
			}

			//	Audio
			try
			{
				_TetrisAudio = new TetrisAudio(Environment.CurrentDirectory + @"\SOUNDS");

				_TetrisAudio.Volume(this._TetrisSettings.State.Volume / 10.0F);
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Form Audio Error", ee);
			}

			//	Input
			try
			{
				this._TetrisInput = new TetrisInput();
				this._TetrisInput.MoveEvent += _TetrisInput_MoveEvent;

			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Form Input Error", ee);
			}

			//	Help
			this.AppendText("F1 for Settings" + Environment.NewLine + "More info:" + Environment.NewLine + "http://tetris.spludlow.co.uk/" + Environment.NewLine +
				"Client V " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + Environment.NewLine);

			try
			{

				if (TetrisClient.OnlyProcess() == true)
					_StartStopServer();

				_StartStopClient();
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Autostart", ee);

				MessageBox.Show("Problem automatically starting client and/or server please check configuration (F1)" + Environment.NewLine + ee.Message);
			}
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			try
			{
				this._TetrisSettings.State.WindowWidth = (int)this.Width;
				this._TetrisSettings.State.WindowHeight = (int)this.Height;
				this._TetrisSettings.State.WindowX = (int)this.Left;
				this._TetrisSettings.State.WindowY = (int)this.Top;

				this.WriteKeysSettings();

				if (this._TetrisSettings != null)
					this._TetrisSettings.Dispose();

				if (this._TetrisBot != null)
					this._TetrisBot.Stop();

				if (this._TetrisBotFarm != null)
					this._TetrisBotFarm.Stop();

				if (this._TetrisInput != null)
					this._TetrisInput.Dispose();

				if (this._TetrisClient != null)
					this._TetrisClient.Stop();

				if (this._TetrisAudio != null)
					this._TetrisAudio.Dispose();

				if (this._TetrisServer != null)
					this._TetrisServer.Stop();

				Spludlow.Log.Finish("Spludlow Tetris, Clean Application Exit");
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Spludlow Tetris, Application Exit", ee);
			}
		}

		private void ReadKeysSettings()
		{
			string[] parts = this._TetrisSettings.State.Keys.Split(new char[] { ',' });

			if (parts.Length != 7)
				throw new ApplicationException("Spludlow Tetris: Not 7 parts to keys settings: '" + this._TetrisSettings.State.Keys + "'");

			this._Keys = new List<int>();

			foreach (string part in parts)
				this._Keys.Add((int)Enum.Parse(typeof(Key), part.Trim()));
		}

		private void WriteKeysSettings()
		{
			this._TetrisSettings.State.Keys = "";

			foreach (int keyInt in this._Keys)
			{
				if (this._TetrisSettings.State.Keys.Length > 0)
					this._TetrisSettings.State.Keys += ", ";

				this._TetrisSettings.State.Keys += ((Key)keyInt).ToString();
			}
		}

		private void MainWindow_StateChanged(object sender, EventArgs e)
		{
			_SetSize();
		}

		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_SetSize();
		}

		private void _TetrisInput_MoveEvent(TetrisInput.InputCommand command)
		{
			if (this._TetrisClient == null)
				return;

			this._TetrisClient.Move((byte)command);
		}

		private void _StartStopServer()
		{
			if (this._TetrisServer == null)
			{
				Task.Run(() => _RunServer());
			}
			else
			{
				_TetrisServer.Stop();
				this._TetrisServer = null;
			}
		}

		private void _RunServer()
		{
			try
			{
				_TetrisServer = new TetrisServer();
				_TetrisServer.Run(this._TetrisSettings.State.ServerAddress, this._TetrisSettings.State.ServerWidth, this._TetrisSettings.State.ServerHeight, this._TetrisSettings.State.ServerTick);
			}
			catch (Exception ee)
			{
				MessageBox.Show("Server Error" + Environment.NewLine + ee.ToString());
			}

			_TetrisServer = null;
		}

		private void _StartStopClient()
		{
			if (this._TetrisClient == null)
			{
				this.Title = this._TetrisSettings.State.ClientDisplayName;

				Task.Run(() => _RunClient());
			}
			else
			{
				this.Title = "No Connection";

				if (this._TetrisInput != null)
					this._TetrisInput.Stop();

				this._TetrisClient.Stop();
				this._TetrisClient = null;
			}
		}

		private void _RunClient()
		{
			try
			{
				_TetrisClient = new TetrisClient();

				//	Wire up the events
				_TetrisClient.DrawBoardEvent += _TetrisClient_DrawBoardEvent;
				_TetrisClient.ReceiveInfoEvent += _TetrisClient_ReceiveInfoEvent;
				_TetrisClient.ReceiveTextEvent += _TetrisClient_ReceiveTextEvent;
				_TetrisClient.ReceiveSoundEvent += _TetrisClient_ReceiveSoundEvent;

				//	Set up joystock
				if (this._TetrisSettings.State.InputIndex > 0)
					this._TetrisInput.Start(this._TetrisSettings.State.InputIndex - 1);

				//	Run the client
				_TetrisClient.Run(this._TetrisSettings.State.ClientAddress, this._TetrisSettings.State.ClientDisplayName);
			}
			catch (Exception ee)
			{
				MessageBox.Show("Client Error" + Environment.NewLine + ee.ToString());
			}

			_TetrisClient = null;

			this._BoardSetup = false;
		}

		private void _TetrisClient_ReceiveTextEvent(string text)
		{
			this.AppendText(text + Environment.NewLine);
		}

		private void AppendText(string text)
		{
			this.Dispatcher.Invoke(() =>
			{
				StringBuilder builder = new StringBuilder(this.TextBoxText.Text);
				builder.Append(text);

				if (builder.Length > 1024)
					builder.Remove(0, 512);

				this.TextBoxText.Text = builder.ToString();
				this.TextBoxText.ScrollToEnd();
			});
		}

		private void _TetrisClient_ReceiveSoundEvent(string sound)
		{
			if (this._TetrisAudio == null)
				return;

			if (sound.StartsWith("!") == true)
			{
				if (this._TetrisSettings.State.NoServerSounds == true)
					return;
				sound = sound.Substring(1);
			}

			bool queue = false;

			if (sound.EndsWith("*") == true)
			{
				sound = sound.Substring(0, sound.Length - 1);
				queue = true;
			}

			this._TetrisAudio.Play(sound, queue);
		}

		private void _TetrisClient_ReceiveInfoEvent(TetrisClientInfo info)
		{
			if (this._BoardSetup == false)
				_SetSize();

			string target = "no target";
			if (info.TargetClientId != 0)
			{
				target = info.TargetDisplayName;

				if (info.ClientId == info.TargetClientId)
					target += " (SELF)";
			}


			this.Dispatcher.Invoke(() =>
			{
				this.TextBlockTarget.Text = target;

				//	More like score board
			});
		}

		private const int MinPixelSize = 3;
		private const int MaxTextSize = 24;

		public void _SetSize()
		{
			if (this._TetrisClient == null || this._TetrisClient.Info == null)
				return;

			double pixelSize = (this.CanvasWindow.ActualHeight - 1) / this._TetrisClient.Info.Height;

			if (pixelSize < MinPixelSize)
				pixelSize = MinPixelSize;

			double pixelSizeTarget = pixelSize / 3;

			double textSize = pixelSize * 0.5;
			if (textSize > MaxTextSize)
				textSize = MaxTextSize;

			this.Dispatcher.Invoke(() =>
			{
				this.ResizeBoard(this.CanvasBoard, pixelSize, this._TetrisClient.Info.Width, this._TetrisClient.Info.Height);

				double x = this._TetrisClient.Info.Width * pixelSize + 1;

				//	Next Peice
				double y = 0;

				Canvas.SetLeft(this.CanvasNext, x);
				Canvas.SetTop(this.CanvasNext, y);
				this.ResizeBoard(this.CanvasNext, pixelSize, 4, 4);

				//	Target Text
				y += 4 * pixelSize + 1;

				Canvas.SetLeft(this.TextBlockTarget, x);
				Canvas.SetTop(this.TextBlockTarget, y);
				this.TextBlockTarget.Height = textSize * 1.2;
				this.TextBlockTarget.FontSize = textSize;

				//	Target Board
				y += textSize * 1.3;

				Canvas.SetLeft(this.CanvasTarget, x);
				Canvas.SetTop(this.CanvasTarget, y);
				this.ResizeBoard(this.CanvasTarget, pixelSizeTarget, this._TetrisClient.Info.Width, this._TetrisClient.Info.Height);

				//	Hyperlink
				y += this._TetrisClient.Info.Height * pixelSizeTarget;

				Canvas.SetLeft(this.GridLink, x);
				Canvas.SetTop(this.GridLink, y);
				this.GridLink.Height = pixelSize;
				this.TextBlockLink.FontSize = textSize;

				//	Server Text
				y += pixelSize;

				if (textSize > MaxTextSize)
					textSize = MaxTextSize;

				Canvas.SetLeft(this.TextBoxText, x);
				Canvas.SetTop(this.TextBoxText, y);
				this.TextBoxText.FontSize = textSize;
				this.TextBoxText.Width = MinSize(this.CanvasWindow.ActualWidth - x);
				this.TextBoxText.Height = MinSize(this.CanvasWindow.ActualHeight - y);
			});

			this._BoardSetup = true;
		}

		private static double MinSize(double value)
		{
			if (value < 1)
				return 1;

			return value;
		}

		private void ResizeBoard(Canvas canvas, double pixelSizeD, int width, int height)
		{
			int pixelSize = (int)pixelSizeD;

			canvas.Width = pixelSize * width + 1;
			canvas.Height = pixelSize * height + 1;

			int rectSize = pixelSize - 1;

			if (this._BoardSetup == false)
				canvas.Children.Clear();

			for (int yIndex = 0; yIndex < height; ++yIndex)
			{
				int yPos = 0 + (yIndex * pixelSize) + 1;

				for (int xIndex = 0; xIndex < width; ++xIndex)
				{
					Rectangle rectangle;

					if (this._BoardSetup == false)
					{
						rectangle = new Rectangle();
						canvas.Children.Add(rectangle);
					}
					else
					{
						rectangle = (Rectangle)canvas.Children[xIndex + (yIndex * width)];
					}

					rectangle.Width = rectSize;
					rectangle.Height = rectSize;

					Canvas.SetLeft(rectangle, 0 + (xIndex * pixelSize) + 1);
					Canvas.SetTop(rectangle, yPos);
				}
			}
		}

		private void _TetrisClient_DrawBoardEvent(int boardKey, TetrisBoard board, TetrisBoard previousBoard, bool reDraw)
		{
			if (this._BoardSetup == false)
				return;

			if (boardKey == -1) //	Next
				this._DrawBoard(this.CanvasNext, board, previousBoard, reDraw);

			if (boardKey == 0)  //	Players board
				this._DrawBoard(this.CanvasBoard, board, previousBoard, reDraw);

			if (boardKey > 0)   //	Target board
				this._DrawBoard(this.CanvasTarget, board, previousBoard, reDraw);
		}

		public void _DrawBoard(Canvas canvas, TetrisBoard board, TetrisBoard lastBoard, bool reDraw)
		{
			this.Dispatcher.Invoke(() =>
			{
				for (int yIndex = 0; yIndex < board.Height; ++yIndex)
				{
					int down = yIndex * board.Width;

					for (int xIndex = 0; xIndex < board.Width; ++xIndex)
					{
						byte data = board.Peek(xIndex, yIndex);

						Rectangle rectangle = (Rectangle)canvas.Children[xIndex + down];

						if (rectangle.Fill != _SolidColorBrushs[data])
							rectangle.Fill = _SolidColorBrushs[data];
					}
				}
			});
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			int index = _Keys.IndexOf((int)e.Key);

			if (index != -1)
			{
				if (this._TetrisClient != null)
					this._TetrisClient.Move((byte)index);
			}
			else
			{
				switch (e.Key)
				{
					case Key.F1:
						this._Dialogue();
						this._SetJoystick(false);
						break;

					case Key.F2:
						this._SetJoystick(true);
						break;

					case Key.F3:
						if (this._TetrisBotFarm.Running == false)
							this._TetrisBotFarm.Start(this._TetrisSettings.State.ClientAddress, this._TetrisSettings.State.BotFarmCount, this._TetrisSettings.State.BotSpeed);
						else
							this._TetrisBotFarm.Stop();
						break;

					case Key.F4:
						if (this._TetrisBot.Running == false)
							this._TetrisBot.Start(this._TetrisClient, this._TetrisSettings.State.BotSpeed);
						else
							this._TetrisBot.Stop();
						break;

					case Key.F5:
						this._TetrisSettings.State.Volume = 0;
						break;
					case Key.F6:
						if (--this._TetrisSettings.State.Volume < 0)
							this._TetrisSettings.State.Volume = 0;
						break;
					case Key.F7:
						if (++this._TetrisSettings.State.Volume > 10)
							this._TetrisSettings.State.Volume = 10;
						break;
					case Key.F8:
						this._TetrisSettings.State.Volume = 10;
						break;
					case Key.F9:
						this._TetrisSettings.State.NoServerSounds = !this._TetrisSettings.State.NoServerSounds;
						break;

					case Key.System:
						if (e.SystemKey == Key.F10)
							MessageBox.Show(this.ActualWidth + ", " + this.ActualHeight);
						e.Handled = true;
						break;

					case Key.F11:
						this._StartStopServer();
						break;
					case Key.F12:
						this._StartStopClient();
						break;

					case Key.Pause:
						this._TetrisClient.SendText("@BR");
						break;
				}

				if (this._TetrisAudio != null)
					this._TetrisAudio.Volume((float)this._TetrisSettings.State.Volume / 10.0F);

			}
		}

		private void _SetJoystick(bool cycle)
		{
			if (this._TetrisInput == null)
				return;

			this._TetrisInput.Stop();

			int joyStickCount = TetrisInput.DeviceInstancesText().Length;

			if (cycle == true)
				++this._TetrisSettings.State.InputIndex;

			if (this._TetrisSettings.State.InputIndex > joyStickCount)
				this._TetrisSettings.State.InputIndex = 0;

			if (this._TetrisSettings.State.InputIndex > 0)
				this._TetrisInput.Start(this._TetrisSettings.State.InputIndex - 1);
		}

		private void _Dialogue()
		{
			bool clientRunning = (this._TetrisClient != null);
			bool serverRuning = (this._TetrisServer != null);

			this._TetrisSettings.State.Command = "";

			SettingsWindow settingsWindow = new SettingsWindow(this._TetrisSettings.State, this._Keys, clientRunning, serverRuning);

			settingsWindow.ShowDialog();

			if (this._TetrisSettings.State.Command == "SERVER")
				_StartStopServer();

			if (this._TetrisSettings.State.Command == "CLIENT")
				_StartStopClient();

			this._TetrisSettings.State.Command = null;
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start(((Hyperlink)e.Source).NavigateUri.ToString());
		}
	}
}
