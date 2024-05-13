using Dtwo.API;
using Dtwo.API.Inputs;
using Dtwo.API.Inputs.Extensions;
using System.Diagnostics;

namespace Dtwo.Plugins.MultiAccount
{
	public static class MultiClickManager
	{
		private static bool m_isStarted;
		private static bool m_onMultiClick;

		public static void Start()
		{
			if (m_isStarted)
			{
				LogManager.LogWarning(
							$"{nameof(MultiClickManager)}.{nameof(Start)}",
							"MultiClick Listener already started", 1);
				return;
			}

			InputKeyListener.Instance.KeyUp += OnKeyUp;

			InputKeyListener.Instance.AddKey(MultiAccountManager.Options.Inputs.MultiClickKey.KeyId);
			InputKeyListener.Instance.AddKey(0x02); // Right click

			m_isStarted = true;
		}

		private static void OnKeyUp(int key)
		{
			Debug.WriteLine("On key up multiclick " + key);


			if (SystemWindowInfos.FocusedDofusWindow == null)
			{
				Debug.WriteLine("FocusedDofusWindow is null");
				return;
			}

			if (m_onMultiClick)
			{
				Debug.WriteLine("m_onMultiClick is true");
				return;
			}

			if (key == MultiAccountManager.Options.Inputs.MultiClickKey.KeyId)
			{
				OnMultiClick();
			}
			else if (key == 0x02) // Right click
			{
				OnRightClick();
			}
		}

		public static void Stop()
		{
			m_isStarted = false;

			InputKeyListener.Instance.RemoveKey(MultiAccountManager.Options.Inputs.MultiClickKey.KeyId);
			InputKeyListener.Instance.RemoveKey(0x02); // Right click
		}

        private static void OnMultiClick()
		{
			OnClick(false);
		}


		private static void OnRightClick()
		{
			OnClick(true);
		}


		private static void OnClick(bool isRight)
		{
			try
			{
				Task.Factory.StartNew(async () =>
				{
					m_onMultiClick = true;

					PInvoke.POINT pos = new PInvoke.POINT();
					//PInvoke.RECT rect = new PInvoke.RECT(); // Todo GetRect if windows are note same size
					PInvoke.GetCursorPos(out pos);
					PInvoke.ScreenToClient(PInvoke.GetForegroundWindow(), ref pos);

					if (MultiAccountManager.Accounts == null)
					{
						return;
					}

					for (int i = 0; i < MultiAccountManager.Accounts.Count; i++)
					{
						var account = MultiAccountManager.Accounts.ElementAt(i).Value;

						if (account == null)
						{
							continue;
						}

						if (account.DofusWindow == null || account.DofusWindow.Process == null)
						{
							continue;
						}

						if (MultiAccountManager.Options == null)
						{
							continue;
						}

						ClickInfo clickInfo = new ClickInfo(pos.X, pos.Y, isRight);

						await account.DofusWindow.SendClick(clickInfo);


						// Wait time between each windows
						Thread.Sleep(API.Random.Range(MultiAccountManager.Options.Delays.DelayMultiClickMin, MultiAccountManager.Options.Delays.DelayMultiClickMax));
					}


					m_onMultiClick = false;
				});
			}

			catch (Exception ex)
			{
				LogManager.LogError(ex.ToString(), 1);
				m_onMultiClick = false;
			}
		}
	}
}
