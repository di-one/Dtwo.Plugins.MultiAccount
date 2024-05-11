﻿using Dtwo.API.Hybride.Network.Messages;
using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
	public static class MultiAccountManager
	{
        public static IReadOnlyDictionary<string, MultiAccountController> Accounts => m_accounts;
        private static Dictionary<string, MultiAccountController> m_accounts = new();
        //public static Object AccountsLock { get; private set; } = new object();

        public static OptionsSettings Options { get; private set; }
        public static List<CharacterPreferences> CharacterPreferences { get; private set; }

        public static Action? OnReOrdered;
        public static Action? OnSelect;
        public static Action? OnStop;

        public static Action<MultiAccountController>? OnRemoveMember;

        public static MultiAccountController? Leader { get; private set; }
        public static MultiAccountController? SelectedAccount { get; private set; }


        public static bool IsStarted { get; private set; }

        public static bool IsOnMoveAll { get; private set; }
        public static bool IsFighting { get; private set; }

        public static bool IsOnPreventInputs { get; private set; }


        private static KeyListener m_keysListener;
        private static WindowEventListener? m_windowEventListener;
        private static List<DofusWindow>? m_queueSelectedCharacter = new();

        public static Action OnAddAccount;
        public static Action OnRemoveAccount;

        public static void Init(OptionsSettings options, List<CharacterPreferences> characterPreferences)
		{
            Options = options;
            CharacterPreferences = characterPreferences;

            SelectCharacterUpdate();
            
            DofusWindow.OnDofusWindowStarted += OnDofusWindowStarted;
            DofusWindow.OnDofusWindowStoped += OnDofusWindowStoped;
        }
        

        public static void Start(bool inviteMembers)
        {
            try
            {
                if (IsStarted)
                {
                    Stop();
                }

                IsStarted = true;

                m_windowEventListener = new WindowEventListener();
                m_windowEventListener.Listen(OnFocusWindow);
                MultiClickListener.Start();

                ListenKeys();

                if (inviteMembers)
                {
                    InviteMembers();
                }

                ReOrder();
            }
            catch (Exception ex)
            {
                LogManager.LogError("Une erreur est survenue pendant le démarrage du mod MultiAccountManager", 1);
                Stop();
                return;
            }
        }

        private static void ListenKeys()
        {
            List<API.InputKey> keys = new List<InputKey>();

            if (Options.Inputs.MultiClickKey != null && Options.Inputs.MultiClickKey.KeyId > 0)
            {
                keys.Add(Options.Inputs.MultiClickKey);
            }
            else
            {
                LogManager.LogWarning("La touche pour le clic multiple n'a pas été définie !", 1);
            }

            if (Options.Inputs.NextKey != null && Options.Inputs.NextKey.KeyId > 0)
            {
                keys.Add(Options.Inputs.NextKey);
            }
            else
            {
                LogManager.LogWarning("La touche pour passer à la fenêtre suivante n'a pas été définie !", 1);
            }

            if (Options.Inputs.PrevKey != null && Options.Inputs.PrevKey.KeyId > 0)
            {
                keys.Add(Options.Inputs.PrevKey); ;
            }
            else
            {
                LogManager.LogWarning("La touche pour passer à la fenêtre précédente n'a pas été définie !", 1);
            }

            if (Options.Inputs.EnableMultiClickRight)
            {
                keys.Add(new InputKey() { KeyId = 0x02, KeyString = "RightClick" });
            }
            else
            {
                LogManager.LogWarning("Le multi clic droit n'est pas activé !", 1);
            }


            var hwnds = GetHwnds();
            m_keysListener = new KeyListener(hwnds);
            m_keysListener.Listen(keys, OnKeyPressed);
        }

        private static void OnKeyPressed(API.InputKey key)
        {
            if (IsOnPreventInputs)
            {
                return;
            }

            try
            {
                if (key == Options.Inputs.MultiClickKey)
                {
                    MultiClickListener.OnMultiClick();
                }
                else if (key.KeyId == 0x02) // right click
                {
                    MultiClickListener.OnRightClick();
                }
                else if (key == Options.Inputs.PrevKey)
                {
                    SelectPrevWindow();
                }
                else if (key == Options.Inputs.NextKey)
                {
                    SelectNextWindow();
                }
                else if (key == Options.Inputs.PassTurnKey && IsFighting)
                {
                    SelectNextLeaving();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex.ToString(), 1);
            }
        }

        public static void Stop()
		{
            try
            {
                IsStarted = false;

                MultiClickListener.Stop();
                m_windowEventListener?.Stop();
                m_windowEventListener = null;

                m_keysListener?.Stop();
                m_keysListener?.Dispose();
                m_keysListener = null;

                m_queueSelectedCharacter = new();
                m_accounts = new();

                Leader = null;
                SelectedAccount = null;

                IsFighting = false;
                IsOnMoveAll = false;

                OnStop?.Invoke();
            }
            catch(Exception ex)
            {
                LogManager.LogError(
                            $"{nameof(MultiAccountManager)}.{nameof(Stop)}", 
                            "error on stop : " + ex.ToString());
            }
        }

        public static void PreventInput(bool b)
        {
            if (IsOnPreventInputs == b)
            {
                LogManager.LogWarning(
                            $"{nameof(MultiAccountManager)}.{nameof(PreventInput)}", 
                            $"Prevent input already {b}");
                return;
            }
        }

        public static CharacterPreferences GetCharacterPreferences(MultiAccountController controller)
        {
            var founded = CharacterPreferences.Find(x => x.CharacterName == controller.DofusWindow.Character.Name);
            if (founded == null)
            {
                founded = new CharacterPreferences()
                {
                    CharacterName = controller.DofusWindow.Character.Name
                };

                controller.SetCharacterPreferences(founded);
                CharacterPreferences.Add(founded);
            }

            return founded;
        }

        // Todo : global ?
        private static void OnDofusWindowStarted(DofusWindow dofusWindow)
        {
            m_queueSelectedCharacter?.Add(dofusWindow);
        }

        private static void OnDofusWindowStoped(DofusWindow dofusWindow)
        {
            RemoveWindow(dofusWindow);
            ReOrder();
        }


        // todo : global ?
        private static void SelectCharacterUpdate()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (m_queueSelectedCharacter?.Count == 0) continue;

                    var window = m_queueSelectedCharacter[0];

                    Thread.Sleep(API.Random.Range(Options.Delays.DelayCharacterSelectionMin, Options.Delays.DelayCharacterSelectionMax));
                    InputEvents.InputManager.KeyboardKeyUpAndDown(window, 0x0D, 50).Wait();

                    m_queueSelectedCharacter.RemoveAt(0);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public static void AddWindow(DofusWindow dofusWindow)
        {
            var controller = GetController(dofusWindow);
            if (controller != null) return;

            controller = new MultiAccountController(dofusWindow, Accounts.Count == 0);

            if (Accounts.Count == 0)
			{
                SelectedAccount = controller;
                Leader = controller;
			}

            m_accounts.Add(dofusWindow.Character.Name, controller);
            OnAddAccount?.Invoke();
        }

        public static void RemoveWindow(DofusWindow dofusWindow)
        {
            var controller = GetController(dofusWindow);
            if (controller == null) return;

            RemoveController(controller);
        }

        public static void RemoveController(MultiAccountController controller)
        {
            m_accounts.Remove(controller.DofusWindow.Character.Name);

            if (IsStarted)
            {
                ReOrder();
                OnRemoveMember?.Invoke(controller);
            }

            OnRemoveAccount?.Invoke();
        }

        // Sended to member joined
        public static void OnPartyJoin(DofusWindow dofusWindow, PartyJoinMessage message)
        {
            bool isOwner = message.Members.Length == 1;
            //var member = message.members.FirstOrDefault(x => x.name == Name);

            foreach (var member in message.Members)
			{
                if (Accounts.ContainsKey(member.name) == false) continue;

                Accounts[member.name].UpdateInitiative((int)member.initiative);
			}

            ReOrder();
        }

        // When stats change (sended for all members)
        public static void OnPartyUpdate(DofusWindow dofusWindow, PartyUpdateMessage message)
        {
            // Not update during fight
            if (IsFighting == false)
            {

                var ownerController = GetController(dofusWindow);
                if (ownerController == null || ownerController.IsLeader == false)
				{
                    return;
				}

                if (Accounts.ContainsKey(message.MemberInformations.name) == false)
                {
                    return;
                }

                MultiAccountController controller = Accounts[message.MemberInformations.name];
                controller.UpdateInitiative((int)message.MemberInformations.initiative);

                ReOrder();
            }
        }

        public static void OnFightStartMessage(DofusWindow dofusWindow, GameFightStartMessage message)
        {
            var controller = GetController(dofusWindow);

            if (controller == null)
            {
                return;
            }

            controller.IsFighting = true;

			if (controller != null && controller.IsLeader)
			{
				IsFighting = true;
				//SelectWindow(controller);
			}
        }

        public static void OnGameFightStartTurnMessage(DofusWindow dofusWindow, GameFightTurnStartMessage message)
        {
            var controller = GetController(dofusWindow);
            if (controller != null && controller.IsLeader)
            {
                for (int i = 0; i < Accounts.Count; i++)
                {
                    var account = Accounts.ElementAt(i).Value;
                    if (account.DofusWindow.Character.Id != message.Id) continue;

                    CharacterPreferences prefs = GetCharacterPreferences(account);
                    if (prefs.IsMule)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(API.Random.Range(850, 1850));
                            SendPassTurn(account);
                        });
                    }

                    break;
                }
            }
        }

        public static void OnGameFightEndTurnMessage(DofusWindow dofusWindow, GameFightTurnEndMessage message)
        {
            try
            {
                if (Options.AutoSelectTurn == false)
                    return;

                var controller = GetController(dofusWindow);
                if (controller != null && controller.IsLeader)
                {
                    for (int i = 0; i < Accounts.Count; i++)
                    {
                        var account = Accounts.ElementAt(i).Value;

                        if (account.DofusWindow.Character.Id != message.Id) continue;

                        CharacterPreferences prefs = GetCharacterPreferences(account);
                        if (prefs.IsMule == false)
                        {
                            SelectNextLeaving();
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void OnFightEndMessage(DofusWindow dofusWindow, GameFightEndMessage message)
        {
            var controller = GetController(dofusWindow);
            if (controller != null && controller.IsLeader)
            {
                for (int i = 0; i < Accounts.Count; i++)
                {
                    Accounts.ElementAt(i).Value.IsFightDead = false;
                    Accounts.ElementAt(i).Value.IsFighting = false;
                }

                IsFighting = false;
            }
        }

        public static void OnGameActionFightDeathMessage(DofusWindow dofusWindow, GameActionFightDeathMessage message)
		{
            var controller = GetController(dofusWindow);
            if (controller?.IsLeader == false) return;

            //lock (AccountsLock)
            //{
                for (int i = 0; i < Accounts.Count; i++)
                {
                    var account = Accounts.ElementAt(i);

                    if (account.Value.DofusWindow.Character.Id == message.TargetId)
                    {
                        account.Value.IsFightDead = true;
                        break;
                    }
                }
            //}
        }

        public static MultiAccountController? GetController(DofusWindow dofuswindow)
		{
            //lock (AccountsLock)
            //{
                for (int i = 0; i < Accounts.Count; i++)
                {
                    var account = Accounts.ElementAt(i);

                    if (account.Value.DofusWindow == dofuswindow)
                    {
                        return account.Value;
                    }
                }
            //}

            return null;
		}

        public static void SelectWindow(MultiAccountController account)
		{
            SelectedAccount = account;
            PInvoke.FocusProcess(account.DofusWindow.WindowProcess);

            OnSelect?.Invoke();
		}

        private static void InviteMembers()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    // Click to chat (send space)
                    InputEvents.InputManager.KeyboardKeyUpAndDown(Leader?.DofusWindow, 0x20, 1).Wait(); // space
                    Thread.Sleep(1500);

                    // Send /invite .... to chat for each accounts
                    for (int i = 0; i < Accounts.Count; i++)
                    {
                        var account = Accounts.ElementAt(i).Value;

                        if (account == Leader) continue;

                        SendInvite(Leader.DofusWindow, account.DofusWindow.Character.Name);
                        Thread.Sleep(API.Random.Range(125, 250));
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogWarning(
                            $"{nameof(MultiAccountManager)}.{nameof(InviteMembers)}", 
                            "Invite members error : " + ex.Message, 1);
                }
            });
        }

        private static void SendInvite(DofusWindow window, string playerName)
        {
            playerName = "/invite " + playerName;
            InputEvents.InputManager.KeyboardKeyUpAndDown(Leader.DofusWindow, 0x08, 500).Wait(); // backspace

            // send inputs character
            for (int i = 0; i < playerName.Length; i++)
            {
                //InputEvents.InputManager.KeyboardKeyDown(window, playerName[i]);
                //InputEvents.InputManager.KeyboardKeyUp(window, playerName[i]);

                InputEvents.InputManager.KeyboardKey(window, playerName[i]);

                Thread.Sleep(API.Random.Range(Options.Delays.DelayChatCharacterMin, Options.Delays.DelayChatCharacterMax));
            }

            Thread.Sleep(API.Random.Range(25, 50));

            // send enter
            InputEvents.InputManager.KeyboardKeyUpAndDown(window, 0x0D, 1).Wait(); // enter
        }

        private static void OnFocusWindow(IntPtr hwnd)
        {
            if (Accounts == null) return;

            //lock (AccountsLock)
            //{
                for (int i = 0; i < Accounts.Count; i++)
                {
                    var account = Accounts.ElementAt(i);

                    if (account.Value.DofusWindow.WindowProcess.MainWindowHandle == hwnd)
                    {
                        SelectedAccount = account.Value;
                        OnSelect?.Invoke();

                        //DofusWindow.SelectDofusWindow(account.Value.DofusWindow);
                        return;
                    }
                }
            //}
        }

        public static void SelectNextWindow()
		{
            int index = IndexOfAccount(SelectedAccount);
            if (index >= Accounts.Count - 1)
			{
                index = 0;
			}
			else
			{
                index++;
			}

            SelectWindow(Accounts.ElementAt(index).Value);
		}

        public static void SelectPrevWindow()
        {
            int index = IndexOfAccount(SelectedAccount);
            if (index == 0)
            {
                index = Accounts.Count - 1;
            }
            else
            {
                index --;
            }

            SelectWindow(Accounts.ElementAt(index).Value);
        }

        public static void SelectNextLeaving(int index = -1, int count = 0)
		{
            if (count > Accounts.Count)
			{
                return;
			}

            if (index == -1)
			{
                index = IndexOfAccount(SelectedAccount);
            }
            
            if (index >= Accounts.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }

            var account = Accounts.ElementAt(index);
            CharacterPreferences prefs = account.Value.CharacterPreferences;
            if (prefs == null)
            {
                prefs = GetCharacterPreferences(account.Value);
            }

            if ((account.Value.IsFightDead && Options.AutoSelectTurn_PassDeath) || prefs.IsMule || account.Value.IsFighting == false)
            {
                SelectNextLeaving(index, count++);
                return;
            }
            else
            {
                SelectWindow(account.Value);
            }
        }

        private static void SendPassTurn(MultiAccountController controller)
        {
            InputEvents.InputManager.KeyboardKeyUpAndDown(controller.DofusWindow, Options.Inputs.PassTurnKey.KeyId, 35).Wait();
        }

        public static void ForceReOrder(MultiAccountController controller, int newIndex)
        {
            var accountOld = m_accounts.ElementAt(newIndex).Value;
            var indexOld = -1;

            for (int i = 0; i < m_accounts.Count; i++)
            {
                var account = m_accounts.ElementAt(i).Value;
                if (account == controller)
                {
                    indexOld = i;
                }
            }
            
            if (indexOld == -1)
            {
                LogManager.LogError(
                            $"{nameof(MultiAccountManager)}.{nameof(InviteMembers)}", 
                            "Error on force reorder IndexOld == - 1");
                return;
            }

            var newAccounts = new Dictionary<string, MultiAccountController>();
            for (int i = 0; i < m_accounts.Count; i++)
            {
                var account = m_accounts.ElementAt(i).Value;
                if (i == indexOld)
                {
                    account = accountOld;
                }
                else if (i == newIndex)
                {
                    account = controller;
                }

                newAccounts.Add(account.DofusWindow.Character.Name, account);
            }

            m_accounts = newAccounts;

            //OnReOrdered?.Invoke();
        }

        public static void ReOrder()
		{
            if (Accounts.Count > 0)
            {
                if (Options.AutoUpdateInitiative)
                {
                    m_accounts = Accounts.OrderByDescending(x => x.Value.Initiative).ToDictionary(p => p.Key, p2 => p2.Value);
                }
                OnReOrdered?.Invoke();
            }
		}

        public static List<IntPtr> GetHwnds()
        {
            List<IntPtr> hwnds = new List<IntPtr>();
            for (int i = 0; i < Accounts.Count; i++)
            {
                var account = Accounts.ElementAt(i);

                hwnds.Add(account.Value.DofusWindow.WindowProcess.MainWindowHandle);
            }

            return hwnds;
        }

        public static int IndexOfAccount(MultiAccountController account)
        {
            for (int i = 0; i < Accounts.Count; i++)
            {
                var val = Accounts.ElementAt(i).Value;
                if (val == account)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOfWindow(DofusWindow dofusWindow)
        {
            for (int i = 0; i < Accounts.Count; i++)
            {
                var val = Accounts.ElementAt(i).Value;
                if (val.DofusWindow == dofusWindow)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
