using System.Runtime.Serialization;

namespace Dtwo.Plugins.MultiAccount
{
    [DataContract]
    public class OptionsDelays
    {
        [DataMember]
        public int DelayMultiClickMin { get; set; } = 450;
        [DataMember]
        public int DelayMultiClickMax { get; set; } = 1150;

        [DataMember]
        public int DelayChatCharacterMin { get; set; } = 125;
        [DataMember]
        public int DelayChatCharacterMax { get; set; } = 250;

        [DataMember]
        public int DelayCharacterSelectionMin { get; set; } = 1000;
        [DataMember]
        public int DelayCharacterSelectionMax { get; set; } = 1500;
    }
}
