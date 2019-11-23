using System.Runtime.Serialization;

namespace EY.COPA.BusinessRuleEngine.DataModels
{
    public class Rule
    {

        [DataMember]
        public int PROTOCOL_REF_ID { get; set; }

        [DataMember]
        public int PROTOCOL_RD_ID { get; set; }

        [DataMember]
        public int PDRR_ID { get; set; }

        [DataMember]
        public int PDRCR_ID { get; set; }

        [DataMember]
        public int? GRR_ID { get; set; }

        [DataMember]
        public string RULE_ACTIVE_FLAG { get; set; }

        [DataMember]
        public string RULE_LAMBDA { get; set; }

        [DataMember]
        public string RULE_LAMBDA_ARGS { get; set; }


        [DataMember]
        public int? RULE_SORT_ORDER { get; set; }

        [DataMember]
        public int? RULE_PRIORITY { get; set; }

        [DataMember]
        public string RULE_MSG { get; set; }

        [DataMember]
        public string PRD_DF_NM { get; set; }
    }
}
