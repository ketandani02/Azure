using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class RuleEngineStatusRequest
    {

        [DataMember]
        public string MasterOrcId { get; set; }

        [DataMember]
        public string ProtocolRuleOrcId { get; set; }

        [DataMember]
        public string Status { get; set; }
        
        [DataMember]
        public string Source { get; set; }

        [DataMember]
        public int PRR_EY_ID { get; set; }

        [DataMember]
        public OrchestrationRequest OrcReq { get; set; }

    }
}
