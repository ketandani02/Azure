using EY.COPA.BusinessRuleEngine.DataModels;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class RuleExecRequest
    {
        [DataMember]
        public string InstanceId { get; set; }

        [DataMember]
        public string FileRef { get; set; }

        [DataMember]
        public string TenantRef { get; set; }

        [DataMember]
        public int ProtocolId { get; set; }

        [DataMember]
        public List<Rule>  Rules { get; set; }


        [DataMember]
        public int RequestQueueId { get; set; }

        [DataMember]
        public string ProtocolRoute { get; set; }


    }
}
