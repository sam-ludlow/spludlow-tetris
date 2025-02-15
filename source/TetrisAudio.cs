using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using SharpDX.XAudio2;      //	SharpDX.XAudio2.dll
using SharpDX.Multimedia;
using System.Threading;

namespace Spludlow.Tetris
{
	/// <summary>
	/// using SharpDX.XAudio2 to load WAVs and play them
	/// Picked at the code from here: https://github.com/sharpdx/SharpDX-Samples/blob/master/Desktop/XAudio2/PlaySound/Program.cs
	/// Constructor loads all WAVs in specified directory and creates a Dictionary of required XAudio2 objects with the filename as the key
	/// The Play() method will allow you  to specify weather sounds will queue or not
	/// If you are queing and over 32 buffers are queued then nothing will hapern
	/// If you are not queing and there are queued buffers then the queue is flushed and sound is submitted
	/// When FlushSourceBuffers() is called you need to wait until the BufferEnd event has fired, otherwise the next SubmitSourceBuffer() may overflow (64 max) if for example many threads are submitting like mad
	/// </summary>
	public class TetrisAudio : IDisposable
	{
		public class TetrisSound
		{
			public AudioBuffer AudioBuffer;
			public SourceVoice SourceVoice;
			public uint[] DecodedPacketsInfo;
			public AutoResetEvent AutoResetEvent;
		}

		public Dictionary<string, TetrisSound> TetrisSounds = new Dictionary<string, TetrisSound>();

		public XAudio2 XAudio2 = null;
		public MasteringVoice MasteringVoice = null;

		public TetrisAudio(string soundsDirectory)
		{
			this.XAudio2 = new XAudio2();

			this.MasteringVoice = new MasteringVoice(this.XAudio2);

			foreach (string filename in Directory.GetFiles(soundsDirectory))
			{
				TetrisSound sound = new TetrisSound();

				string name = Path.GetFileNameWithoutExtension(filename);

				if (name.StartsWith("_") == true)	//	Turn sounds off
					continue;

				using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					using (SoundStream soundStream = new SoundStream(fileStream))
					{
						sound.AudioBuffer = new AudioBuffer();
						sound.AudioBuffer.Stream = soundStream.ToDataStream();
						sound.AudioBuffer.AudioBytes = (int)soundStream.Length;
						sound.AudioBuffer.Flags = BufferFlags.EndOfStream;

						sound.SourceVoice = new SourceVoice(this.XAudio2, soundStream.Format, true);
						sound.SourceVoice.BufferEnd += (context) => SourceVoice_BufferEnd(name);

						sound.DecodedPacketsInfo = soundStream.DecodedPacketsInfo;

						sound.AutoResetEvent = new AutoResetEvent(false);

						this.TetrisSounds.Add(name, sound);
					}
				}
			}

			this.Volume(1.0F);
		}

		private void SourceVoice_BufferEnd(string name)
		{
			this.TetrisSounds[name].AutoResetEvent.Set();
		}

		public void Volume(float volume)
		{
			foreach (string key in this.TetrisSounds.Keys)
			{
				float equalize = 1.0F;
				switch (key)
				{
					case "TICK":
						equalize = 0.5F;
						break;
					case "DROP":
						equalize = 0.3F;
						break;
					case "ROTATE":
						equalize = 0.3F;
						break;
					//case "LINE":
					//	equalize = 0.5F;
					//	break;
				}

				this.TetrisSounds[key].SourceVoice.SetVolume(volume * equalize);
			}
		}

		public void Play(string soundName, bool queue)
		{
			if (this.TetrisSounds.ContainsKey(soundName) == false)
				return;

			TetrisSound sound = this.TetrisSounds[soundName];

			SourceVoice sourceVoice = sound.SourceVoice;

			if (queue == true && sourceVoice.State.BuffersQueued > 32)
				return;

			if (queue == false && sourceVoice.State.BuffersQueued > 0)
			{
				sourceVoice.Stop();
				sourceVoice.FlushSourceBuffers();

				sound.AutoResetEvent.WaitOne();
			}

			sourceVoice.SubmitSourceBuffer(sound.AudioBuffer, sound.DecodedPacketsInfo);

			sourceVoice.Start();
		}

		public void Dispose()
		{
			foreach (string key in this.TetrisSounds.Keys)
			{
				TetrisSound sound = this.TetrisSounds[key];

				sound.SourceVoice.DestroyVoice();
				sound.SourceVoice.Dispose();
			}

			if (this.MasteringVoice != null)
				this.MasteringVoice.Dispose();

			if (this.XAudio2 != null)
				this.XAudio2.Dispose();
		}


		/// <summary>
		/// Play sounds as fast as posible using specified number of threads on a single TetrisAudio object
		/// No clean exit so you need to kill process in Task Manager to finish
		/// </summary>
		public static void Test(string soundsDirectory, int threadCount)
		{
			Spludlow.Tetris.TetrisAudio audio = new Spludlow.Tetris.TetrisAudio(soundsDirectory);

			string[] soundNames = audio.TetrisSounds.Keys.ToArray();

			Task[] tasks = new Task[threadCount];

			for (int id = 0; id < threadCount; ++id)
			{
				int threadId = id;

				tasks[threadId] = new Task(() => TestWorker(threadId, soundNames, audio));
				tasks[threadId].Start();
			}

			int index = Task.WaitAny(tasks);
			
			throw new ApplicationException("Test Tetris Audio Finished with errors", tasks[index].Exception);
		}

		private static void TestWorker(int threadId, string[] soundNames, Spludlow.Tetris.TetrisAudio audio)
		{
			Random random = new Random();

			while (true)
			{
				int index = random.Next(soundNames.Length);

				string soundName = soundNames[index];
				bool queue = (random.Next(2) == 0 ? false : true);    //	(index % 2)

				try
				{
					audio.Play(soundName, queue);
				}
				catch (Exception ee)
				{
					throw new ApplicationException("Test Tetris Audio: " + threadId + ", " + soundName + ", " + queue + ", " + ee.Message, ee);
				}
			}
		}
	}
}
