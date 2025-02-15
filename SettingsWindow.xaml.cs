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
using System.Windows.Shapes;

using Spludlow.Tetris;

namespace SpludlowTetris
{
	/// <summary>
	/// Spludlow Tetris Settings Window
	/// </summary>
	public partial class SettingsWindow : Window
	{
		private TetrisSettings.TetrisSettingsState ClientState;

		private List<int> Keys;

		private List<string> KeyNames = new List<string>(new string[] { "UP", "DOWN", "LEFT", "RIGHT", "CW", "CCW", "TARGET" });

		public SettingsWindow(TetrisSettings.TetrisSettingsState clientState, List<int> keys, bool clientRunning, bool serverRunning)
		{
			InitializeComponent();

			this.ClientState = clientState;

			this.Keys = keys;

			for (int vol = 0; vol <= 10; ++vol)
				this.ComboBoxVolume.Items.Add(vol);

			this.ComboBoxInputIndex.Items.Add("KeyDown");
			foreach (string joyStickText in TetrisInput.DeviceInstancesText())
				this.ComboBoxInputIndex.Items.Add(joyStickText);

			this.ButtonClient.Content = (clientRunning == true ? "STOP Client" : "START Client");
			this.ButtonServer.Content = (serverRunning == true ? "STOP Server" : "START Server");

			this.GridSettings.DataContext = this.ClientState;

			this.ShowKeys(-1);

			this.PreviewKeyDown += SettingsWindow_PreviewKeyDown;

			this.Title = "Spludlow Tetris Settings - Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}

		private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				e.Handled = true;
				this.Close();
			}

			for (int index = 0; index < KeyNames.Count; ++index)
			{
				Button button = (Button)this.FindName("Button" + KeyNames[index]);

				if (((string)button.Content).Contains(":") == false)
				{
					this.Keys[index] = (int)e.Key;
					this.ShowKeys(-1);
					e.Handled = true;
				}
			}
		}

		private void ShowKeys(int selectedIndex)
		{
			for (int index = 0; index < KeyNames.Count; ++index)
			{
				Button button = (Button)this.FindName("Button" + KeyNames[index]);

				if (selectedIndex == index)
					button.Content = KeyNames[index];
				else
					button.Content = KeyNames[index] + " : " + ((Key)Keys[index]).ToString();
			}
		}

		private void ButtonClient_Click(object sender, RoutedEventArgs e)
		{
			this.ClientState.Command = "CLIENT";
			this.Close();
		}

		private void ButtonServer_Click(object sender, RoutedEventArgs e)
		{
			this.ClientState.Command = "SERVER";
			this.Close();
		}

		private void Button_Click_Key(object sender, RoutedEventArgs e)
		{
			Button selectedButton = (Button)sender;

			int selectedIndex = this.KeyNames.IndexOf(selectedButton.Name.Substring(6));

			this.ShowKeys(selectedIndex);

			e.Handled = true;
		}
	}
}
