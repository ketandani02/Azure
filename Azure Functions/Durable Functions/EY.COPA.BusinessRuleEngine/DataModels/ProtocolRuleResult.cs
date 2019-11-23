using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataModels
{
    [DataContract]
    public class ProtocolRuleResult
    {
        [DataMember]
        public int PRR_EY_ID { get; set; }

        [DataMember]
        public int CLIENT_REF_ID { get; set; }

        [DataMember]
        public int CLIENT_SRC_REF_ID { get; set; }

        [DataMember]
        public int SRC_FILE_ID { get; set; }

        [DataMember]
        public int PROTOCOL_REF_ID { get; set; }

        [DataMember]
        public int PROTOCOL_RD_ID { get; set; }

        [DataMember]
        public int PDRR_ID { get; set; }

        [DataMember]
        public int PDRCR_ID { get; set; }

        [DataMember]
        public int PRR_TABLE_REC_ID { get; set; }

        [DataMember]
        public string PRR_REMARKS { get; set; }

        [DataMember]
        public string PRR_ACTIVE_FLAG { get; set; }


        [DataMember]
        public int RequestQueueId { get; set; }

        [DataMember]
        public bool IsValid { get; set; }
        [DataMember]
        public string PRD_DF_NM { get; set; }
        [DataMember]
        public string PLN_DECISN_DT { get; set; }
    }
}
