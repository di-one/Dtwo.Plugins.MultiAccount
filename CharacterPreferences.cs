using System.Runtime.Serialization;

namespace Dtwo.Plugins.MultiAccount
{
    [DataContract]
    public class CharacterPreferences
    {
        [DataMember]
        public string CharacterName { get; set; }

        [DataMember]
        public bool IsMule { get; set; }
    }
}
