using EY.COPA.BusinessRuleEngine.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{

    [DataContract]
    public class PROrchestrationRequest
    {
        [DataMember]
        public string MasterOrcId { get; set; }

        [DataMember]
        public string FileRef { get; set; }

        [DataMember]
        public string TenantRef { get; set; }

        [DataMember]
        public int ProtocolId { get; set; }

        [DataMember]
        public List<RuleDetails> RuleDetailList{ get; set; }

        [DataMember]
        public List<ProtocolDictionary> ProtocolDictionarySet { get; set; }

    }
}
