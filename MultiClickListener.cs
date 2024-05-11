using static Dtwo.Plugins.InputEvents.PInvoke;
using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
	public static class MultiClickListener
	{
		private class ClickInfo
        {
			public readonly bool RightClick;
			public readonly POINT Point;
			public readonly RECT Rect;

			public ClickInfo(bool rightClick, POINT point, RECT rect)
            {
				RightClick = rightClick;
				Point = point;
				Rect = rect;
			}
        }

		private static List<ClickInfo> m_lastClicks = new List<ClickInfo> (); 

		private static bool m_isStarted;

		public static void Start()
		{
			if (m_isStarted)
            {
				LogManager.LogWarning(
                            $"{nameof(MultiClickListener)}.{nameof(Start)}", 
							"MultiClick Listener already started", 1);
				return;
            }

            UpdateClicks();

			m_isStarted = true;
		}

		public static void Stop()
		{
			m_isStarted = false;
			m_lastClicks = new List<ClickInfo>();
		}

		public static void OnMultiClick()
		{
			OnClick(false);
		}


		public static void OnRightClick()
        {
			OnClick(true);
        }


		private static void OnClick(bool isRight)
        {
			try
			{
				POINT pos = new POINT();
				RECT rect = new RECT(); // Todo GetRect if windows are note same size
				InputEvents.PInvoke.GetCursorPos(out pos);
				PInvoke.ScreenToClient(PInvoke.GetForegroundWindow(), ref pos);
				m_lastClicks.Add(new ClickInfo(isRight, pos, rect));

			}
			catch (Exception ex)
            {
				LogManager.LogError(ex.ToString(), 1);
            }
		}

		// Todo : Utiliser le temps rééls entre 2 clics, les délais arbitraires doivent être seulement entre le scomptes
		// L'idée devrait peut-être d'avoir un comportement plus naturel : 3 click fenêtre principal => 1 click suivante, etc ....
		// Plutot que : 3 click fenêtre principal => 3 click suivante, etc ...
		private static void UpdateClicks()
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					while (m_isStarted)
					{
						while (m_lastClicks.Count > 0)
						{
							var click = m_lastClicks[0];

							if (click == null)
							{
								continue;
							}

							if (MultiAccountManager.Accounts == null)
							{
								continue;
							}

							//lock (MultiAccountManager.AccountsLock)
							//{
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


								if (click.RightClick)
								{
									InputEvents.InputManager.MouseRightClickUpAndDown(click.Point.X, click.Point.Y, 50, account.DofusWindow).Wait();
								}
								else
								{
									InputEvents.InputManager.MouseClickUpAndDown(click.Point.X, click.Point.Y, 50, account.DofusWindow).Wait();
								}

								Thread.Sleep(API.Random.Range(MultiAccountManager.Options.Delays.DelayMultiClickMin, MultiAccountManager.Options.Delays.DelayMultiClickMax));
							}

							m_lastClicks.RemoveAt(0);
						}

					}
					//}
				}
				catch (Exception ex)
				{
					LogManager.LogError(ex.ToString(), 1);
					// todo : stop ?
				}
			}, TaskCreationOptions.LongRunning);
		}
	}
}
