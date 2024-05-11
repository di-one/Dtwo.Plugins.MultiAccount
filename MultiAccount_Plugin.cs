using System.Reflection;
using Dtwo.API.Hybride.Network.Messages;
using Dtwo.API;
using System.Diagnostics;

namespace Dtwo.Plugins.MultiAccount
{
	public class MultiAccount_Plugin : Plugin
	{
		public static MultiAccount_Plugin Instance { get; private set; }
		public MultiAccount_Plugin(PluginInfos infos, Assembly assembly) : base(infos, assembly)
		{
			Instance = this;

			OptionsSettings settings = LoadFile<OptionsSettings>("options");
			if (settings == null) settings = new OptionsSettings();

			List<CharacterPreferences> characterPreferences = LoadFile<List<CharacterPreferences>>("characters_preferences");
			if (characterPreferences == null) characterPreferences = new List<CharacterPreferences>();

			MultiAccountManager.Init(settings, characterPreferences);
		}

		[DofusEvent]
		public void OnPartyJoinMessage(DofusWindow dofusWindow, PartyJoinMessage message)
		{
			MultiAccountManager.OnPartyJoin(dofusWindow, message);
		}

		[DofusEvent]
		public void OnPartyUpdateMessage(DofusWindow dofusWindow, PartyUpdateMessage message)
		{
			MultiAccountManager.OnPartyUpdate(dofusWindow, message);
		}

        [DofusEvent]
        public void OnGameFightStartMessage(DofusWindow dofusWindow, GameFightStartMessage message)
        {
			MultiAccountManager.OnFightStartMessage(dofusWindow, message);
        }

		[DofusEvent]
		public void OnGameFightTurnStartMessage(DofusWindow dofusWindow, GameFightTurnStartMessage message)
		{
			MultiAccountManager.OnGameFightStartTurnMessage(dofusWindow, message);
		}

		[DofusEvent]
		public void OnGameFightTurnEndMessage(DofusWindow dofusWindow, GameFightTurnEndMessage message)
		{
			MultiAccountManager.OnGameFightEndTurnMessage(dofusWindow, message);
		}

		[DofusEvent]
		public void OnGameFightEndMessage(DofusWindow dofusWindow, GameFightEndMessage message)
		{
			MultiAccountManager.OnFightEndMessage(dofusWindow, message);
		}

		[DofusEvent]
		public static void OnGameActionFightDeathMessage(DofusWindow dofusWindow, GameActionFightDeathMessage message)
		{
			MultiAccountManager.OnGameActionFightDeathMessage(dofusWindow, message);
		}
	}
}