﻿using System.Runtime.Serialization;
using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
    [DataContract]
    public class OptionsInputs
    {
        [DataMember]
        public InputKey PassTurnKey { get; set;  } = new InputKey();
        [DataMember]
        public InputKey MultiClickKey { get; set; } = new InputKey();

        [DataMember]
        public InputKey NextKey { get; set; } = new InputKey();
        [DataMember]
        public InputKey PrevKey { get; set; } = new InputKey();

        [DataMember]
        public bool EnableMultiClickRight { get; set; } = true;
    }
}
