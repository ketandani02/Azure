using EY.COPA.BusinessRuleEngine.DataModels;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class RulePrepRequest
    {
        [DataMember]
        public string InstanceId { get; set; }

        [DataMember]
        public  IEnumerable<RuleDetails> Rules { get; set; }

        [DataMember]
        public IEnumerable<ProtocolDictionary> PRDSet { get; set; }



    }

}
