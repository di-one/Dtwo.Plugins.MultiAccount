using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
    public class MultiKeyListener
    {
        private readonly char[] m_availableKeys = new char[] { '&', 'é', '"', '\'', '(' };

        public bool IsStarted { get; private set; }

        private API.InputKey m_firstKey;
        private bool m_waitUp;
        private char m_waitUpChar = '\0';

        private List<char> m_inputKeys = new List<char>();

        public void Start(API.InputKey firstKey)
        {
            if (IsStarted)
            {
                LogManager.LogError("MultiKeyListener already started", 1);
                return;
            }

            m_firstKey = firstKey;

            IsStarted = true;

            Update();
        }

        public void Stop()
        {
            IsStarted = false;
        }

        private void Update()
        {
            UpdateRead();
            UpdateInput();
        }

        private void UpdateRead()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (IsStarted)
                    {
                        if (InputEvents.PInvoke.KeyIsDown(m_firstKey.KeyId))
                        {
                            foreach (var c in m_availableKeys)
                            {
                                if (InputEvents.PInvoke.KeyIsDown(c))
                                {
                                    m_waitUp = true;
                                    m_waitUpChar = c;
                                }
                                else if (m_waitUp && m_waitUpChar == c) // up
                                {
                                    m_waitUp = false;
                                    m_waitUpChar = '\0';

                                    m_inputKeys.Add(c);
                                }
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

        private void UpdateInput()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (IsStarted)
                    {
                        while (m_inputKeys.Count > 0)
                        {
                            var inputKey = m_inputKeys[0];

                            //lock (MultiAccountManager.AccountsLock)
                            //{
                            for (int i = 0; i < MultiAccountManager.Accounts.Count; i++)
                            {
                                var account = MultiAccountManager.Accounts.ElementAt(i);

                                InputEvents.InputManager.KeyboardKeyUpAndDown(account.Value.DofusWindow, inputKey, 25).Wait();

                                Thread.Sleep(API.Random.Range(10, 25)); // between clicks
                            }
                            //}

                            Thread.Sleep(API.Random.Range(150, 250)); // between account
                            m_inputKeys.RemoveAt(0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex.ToString(), 1);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
