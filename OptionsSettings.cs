using System.Runtime.Serialization;

namespace Dtwo.Plugins.MultiAccount
{
    [DataContract]
    public class OptionsSettings
    {
        [DataMember]
        public OptionsInputs Inputs { get; set; } = new OptionsInputs();

        [DataMember]
        public OptionsDelays Delays { get; set; } = new OptionsDelays();


        [DataMember]
        public bool AutoSelectTurn { get; set; } = true;

        [DataMember]
        public bool AutoSelectTurn_PassDeath { get; set; } = true;

        [DataMember]
        public bool AutoUpdateInitiative { get; set; } = true;
    }
}
