using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class PRDLoadRequest
    {
        [DataMember]
        public string MasterOrcId { get; set; }

        [DataMember]
        public int ProtocolId { get; set; }
    }
}
