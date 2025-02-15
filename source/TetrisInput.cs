using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using SharpDX.DirectInput;  //	SharpDX.dll & SharpDX.DirectInput.dll

namespace Spludlow.Tetris
{
	/// <summary>
	/// Handle GamePads and JoySticks using SharpDX
	/// </summary>
	public class TetrisInput : IDisposable
	{
		public enum InputCommand
		{
			Up, Down, Left, Right, Button0, Button1, Button2
		}

		public static int AutoRepeatRate = 32;                          //	Milliseconds of sleep in joystick loop
		public static int AutoRepeatDelay = 500 / AutoRepeatRate;       //	Number of loops before autorepeating
		public static int CentreMargin = 0x3fff;
		public static int CenterMin = 0x7fff - CentreMargin;
		public static int CenterMax = 0x7fff + CentreMargin;

		private DirectInput DirectInput = null;
		private Joystick Joystick = null;
		private DeviceType DeviceType;

		private Thread Thread = null;

		public delegate void Move(InputCommand command);
		public event Move MoveEvent;

		public TetrisInput()
		{
			this.DirectInput = new DirectInput();
		}

		public void Start(int joystickIndex)
		{
			DeviceInstance[] deviceInstances = DeviceInstances(this.DirectInput);

			if (deviceInstances.Length == 0 || (joystickIndex >= deviceInstances.Length))
			{
				Spludlow.Log.Error("Tetris Input, joystick not available: index:" + joystickIndex + ", deviceInstances:" + deviceInstances);
				return;
			}

			this.Joystick = new Joystick(this.DirectInput, deviceInstances[joystickIndex].InstanceGuid);

			this.DeviceType = deviceInstances[joystickIndex].Type;

			this.Joystick.Properties.BufferSize = 256;

			this.Joystick.Acquire();

			this.Thread = new Thread(new ThreadStart(this.Run));
			this.Thread.Start();
		}

		public void Stop()
		{
			if (this.Thread != null)
			{
				this.Thread.Abort();
				this.Thread.Join();
			}
			this.Thread = null;

			if (this.Joystick != null)
			{
				this.Joystick.Unacquire();
				this.Joystick.Dispose();
			}
			this.Joystick = null;
		}

		public void Dispose()
		{
			this.Stop();

			if (this.DirectInput != null)
				this.DirectInput.Dispose();
		}

		public static string[] DeviceInstancesText()
		{
			List<string> lines = new List<string>();

			using (DirectInput directInput = new DirectInput())
			{
				foreach (DeviceInstance deviceInstance in DeviceInstances(directInput))
					lines.Add(deviceInstance.Type + " : " + deviceInstance.InstanceName + " : " + deviceInstance.InstanceGuid.ToString());
			}

			return lines.ToArray();
		}

		public static DeviceInstance[] DeviceInstances(DirectInput directInput)
		{
			List<DeviceInstance> deviceInstances = new List<DeviceInstance>();

			DeviceType[] deviceTypes = new DeviceType[] { DeviceType.Gamepad, DeviceType.Joystick };
			foreach (DeviceType deviceType in deviceTypes)
			{
				foreach (DeviceInstance deviceInstance in directInput.GetDevices(deviceType, DeviceEnumerationFlags.AllDevices))
					deviceInstances.Add(deviceInstance);
			}

			return deviceInstances.ToArray();
		}

		public void Run()
		{
			try
			{
				int commandCount = Enum.GetValues(typeof(InputCommand)).Length;
				bool[] commandsCurrent = new bool[commandCount];
				int[] commandRepeatCounts = new int[commandCount];

				while (true)
				{
					this.Joystick.Poll();

					JoystickUpdate[] joystickUpdates = this.Joystick.GetBufferedData();

					foreach (JoystickUpdate stick in joystickUpdates)
					{
						if (stick.Offset == JoystickOffset.Y)
						{
							switch (stick.Value)
							{
								case 0:
									//if (this.DeviceType == DeviceType.Joystick)	//	Disable up for joysticks, too easy to catch
									//{
									//	commandsCurrent[(int)InputCommands.Up] = true;
									//	commandsCurrent[(int)InputCommands.Down] = false;
									//}
									break;
								case 32511:
									commandsCurrent[(int)InputCommand.Up] = false;
									commandsCurrent[(int)InputCommand.Down] = false;
									break;
								case 65535:
									commandsCurrent[(int)InputCommand.Up] = false;
									commandsCurrent[(int)InputCommand.Down] = true;
									break;
								default:
									if (stick.Value > CenterMin && stick.Value < CenterMax)
									{
										commandsCurrent[(int)InputCommand.Up] = false;
										commandsCurrent[(int)InputCommand.Down] = false;
									}
									break;
							}
						}

						if (this.DeviceType == DeviceType.Gamepad)
						{
							if (stick.Offset == JoystickOffset.Z)
							{
								if (stick.Value > 60000)	//== 65408)
									commandsCurrent[(int)InputCommand.Up] = true;
								else
									commandsCurrent[(int)InputCommand.Up] = false;
							}
						}

						if (stick.Offset == JoystickOffset.X)
						{
							switch (stick.Value)
							{
								case 0:
									commandsCurrent[(int)InputCommand.Left] = true;
									commandsCurrent[(int)InputCommand.Right] = false;
									break;
								case 32511:
									commandsCurrent[(int)InputCommand.Left] = false;
									commandsCurrent[(int)InputCommand.Right] = false;
									break;
								case 65535:
									commandsCurrent[(int)InputCommand.Left] = false;
									commandsCurrent[(int)InputCommand.Right] = true;
									break;
								default:
									if (stick.Value > CenterMin && stick.Value < CenterMax)
									{
										commandsCurrent[(int)InputCommand.Left] = false;
										commandsCurrent[(int)InputCommand.Right] = false;
									}
									break;
							}
						}

						if (stick.Offset == JoystickOffset.PointOfViewControllers0)
						{
							switch (stick.Value)
							{
								case -1:    //	M
									commandsCurrent[(int)InputCommand.Up] = false;
									commandsCurrent[(int)InputCommand.Down] = false;
									commandsCurrent[(int)InputCommand.Left] = false;
									commandsCurrent[(int)InputCommand.Right] = false;
									break;
								case 0:     //	U
									commandsCurrent[(int)InputCommand.Up] = true;
									commandsCurrent[(int)InputCommand.Down] = false;
									commandsCurrent[(int)InputCommand.Left] = false;
									commandsCurrent[(int)InputCommand.Right] = false;
									break;
								case 9000:  //	R
									commandsCurrent[(int)InputCommand.Up] = false;
									commandsCurrent[(int)InputCommand.Down] = false;
									commandsCurrent[(int)InputCommand.Left] = false;
									commandsCurrent[(int)InputCommand.Right] = true;
									break;
								case 18000: //	D
									commandsCurrent[(int)InputCommand.Up] = false;
									commandsCurrent[(int)InputCommand.Down] = true;
									commandsCurrent[(int)InputCommand.Left] = false;
									commandsCurrent[(int)InputCommand.Right] = false;
									break;
								case 27000: //	L
									commandsCurrent[(int)InputCommand.Up] = false;
									commandsCurrent[(int)InputCommand.Down] = false;
									commandsCurrent[(int)InputCommand.Left] = true;
									commandsCurrent[(int)InputCommand.Right] = false;
									break;
							}
						}


						if (stick.Offset == JoystickOffset.Buttons0)
							commandsCurrent[(int)InputCommand.Button0] = (stick.Value == 128);

						if (stick.Offset == JoystickOffset.Buttons1)
						{
							if (this.DeviceType == DeviceType.Joystick)
								commandsCurrent[(int)InputCommand.Up] = (stick.Value == 128);   //	for joystick use CCW for UP(drop)
							else
								commandsCurrent[(int)InputCommand.Button1] = (stick.Value == 128);
						}


						if (stick.Offset == JoystickOffset.Buttons2)
							commandsCurrent[(int)InputCommand.Button2] = (stick.Value == 128);
					}

					foreach (InputCommand command in Enum.GetValues(typeof(InputCommand)))
					{
						if (commandsCurrent[(int)command] == false)
						{
							commandRepeatCounts[(int)command] = 0;
							continue;
						}

						bool perform = false;

						if (commandRepeatCounts[(int)command] == 0)
							perform = true;

						if (++commandRepeatCounts[(int)command] > AutoRepeatDelay)
							perform = true;

						if (perform == false)
							continue;

						this.MoveEvent(command);
					}

					System.Threading.Thread.Sleep(AutoRepeatRate);
				}
			}
			catch (Exception ee)
			{
				Spludlow.Log.Error("Tetris Input: Error in main loop", ee);
				this.Stop();
			}



		}
	}
}
