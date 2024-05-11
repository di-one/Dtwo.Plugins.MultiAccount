using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
	public class KeyListener : IDisposable
	{
		private bool m_isListening;
		private bool disposedValue;
		private Task? m_task;
		private bool m_waitUp;
		private API.InputKey m_waitUpInputKey;

		private List<IntPtr> m_availableWindows = new List<IntPtr>();

		public KeyListener(List<IntPtr> availableWindows)
        {
			m_availableWindows = availableWindows;
        }

		public void Listen(List<API.InputKey> inputKeys, Action<API.InputKey> listennedCallback)
		{
			if (m_isListening) return;

			m_isListening = true;

			m_task = Task.Factory.StartNew(() =>
			{
				try
				{
					while (m_isListening)
					{
						if (IsDesiredForegroundWindow() == false)
						{
							continue;
						}

						for (int i = 0; i < inputKeys.Count; i++)
						{
							var inputKey = inputKeys[i];

							if (m_waitUp && inputKey != m_waitUpInputKey)
							{
								continue;
							}

							if (InputEvents.PInvoke.KeyIsDown(inputKey.KeyId))
							{
								if (m_waitUp)
								{
									continue;
								}

								m_waitUp = true;
								m_waitUpInputKey = inputKey;
								break;
							}
							else if (m_waitUp)
							{
								listennedCallback(m_waitUpInputKey);
								m_waitUp = false;
							}
						}

					}
				}
				catch (Exception ex)
                {
					LogManager.LogError(ex.ToString(), 1);
				}

			}, TaskCreationOptions.LongRunning);
		}

		private bool IsDesiredForegroundWindow()
        {
			for (int i = 0; i < m_availableWindows.Count; i++)
            {
				IntPtr ptr = m_availableWindows[i];
				if (WindowEventListener.LastForegroundWindow == ptr)
                {
					return true;
                }
            }

			return false;
        }

		public void Stop()
		{
			if (m_isListening)
			{
				m_isListening = false;
				m_task = null;
				m_waitUp = false;
				m_availableWindows.Clear();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{

				}

				Stop();

				disposedValue = true;
			}
		}

		// TODO: substituer le finaliseur uniquement si 'Dispose(bool disposing)' a du code pour libérer les ressources non managées
		~KeyListener()
		{
			// Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
