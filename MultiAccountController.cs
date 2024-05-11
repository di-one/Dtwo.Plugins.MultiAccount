using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
	public class MultiAccountController
	{
		public readonly DofusWindow DofusWindow;
		public readonly bool IsLeader;
		public CharacterPreferences CharacterPreferences { get; private set; }

		public bool IsFightDead;
		public bool IsFighting;
		
		public int Initiative { get; private set; }

		public MultiAccountController(DofusWindow dofusWindow, bool isLeader)
		{
			DofusWindow = dofusWindow;
			IsLeader = isLeader;
		}

		// Todo : Revoir globalement MAM pour que l'initialisation se fasse quand touts les persos sont connectés
		// et ainsi avoir les données sur le character
		public void SetCharacterPreferences(CharacterPreferences preferences)
        {
			CharacterPreferences = preferences;

		}

		public void UpdateInitiative(int val)
		{
			Initiative = val;
		}
	}
}
