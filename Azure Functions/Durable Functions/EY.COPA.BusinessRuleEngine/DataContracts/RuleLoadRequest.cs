using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class RuleLoadRequest
    {
        [DataMember]
        public string MasterOrcId { get; set; }

        [DataMember]
        public int ProtocolId { get; set; }
    }
}
