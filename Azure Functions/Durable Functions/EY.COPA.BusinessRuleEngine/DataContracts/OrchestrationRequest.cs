
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class OrchestrationRequest 
    {
        [DataMember]
        public string FileRef { get; set; }

        [DataMember]
        public string Tenant { get; set; }

        [DataMember]
        public int ProtocolId { get; set; }

        [DataMember]
        public int RequestQueueId { get; set; }

    }
}
