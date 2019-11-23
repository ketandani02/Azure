using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataModels
{
   
    [DataContract]
    public class RuleDetails
    {

        [DataMember]
        public int PROTOCOL_REF_ID { get; set; }

        [DataMember]
        public int PROTOCOL_RD_ID { get; set; }  

        [DataMember]
        public int PDRR_ID { get; set; }

        [DataMember]
        public int PDRCR_ID { get; set; }

        /* Protocol Dictionary Data */
        [DataMember]
        public string PDBIZNM_REF_CODE { get; set; }

        [DataMember]
        public string PRD_DF_SYSTEM { get; set; }

        [DataMember]
        public string PRD_DF_NM { get; set; }

        [DataMember]
        public string PRD_DF_COL_ID { get; set; }

        [DataMember]
        public string PRD_DF_BUSINESS_NA { get; set; }

        [DataMember]
        public string PRD_DF_DESC { get; set; }

        [DataMember]
        public string PRD_DF_REFERENCE_TABLE { get; set; }

        [DataMember]
        public string PRD_DF_DATA_TYPE { get; set; }

        [DataMember]
        public string PRD_DF_FORMAT { get; set; }

        [DataMember]
        public string PRD_DF_USE_OF_NA { get; set; }

        [DataMember]
        public string PRD_DF_ALWAYS_REQ { get; set; }

        [DataMember]
        public short? PRD_DF_MIN_ALLOWED_LEN { get; set; }

        [DataMember]
        public short? PRD_DF_MAX_ALLOWED_LEN { get; set; }

        [DataMember]
        public string PRD_DF_ONLY_ALLOWED_VALS { get; set; }

        [DataMember]
        public string PRD_DF_NOT_ALLOWED_VALS { get; set; }

        [DataMember]
        public string PRD_DF_DT_TM_FORMAT { get; set; }

        [DataMember]
        public short? PRD_DF_ORDER_INPUT_FILE { get; set; }

        [DataMember]
        public short? PRD_DF_ORDER_OUTPUT_FILE { get; set; }

        [DataMember]
        public DateTime? PRD_EFF_BEGIN_DT { get; set; }

        [DataMember]
        public DateTime? PRD_EFF_END_DT { get; set; }

        [DataMember] 
        public string PRD_ACTIVE_FLAG { get; set; }


        /* Effective Rule Data */
        
        [DataMember]
        public string RULE_TYPE { get; set; }

        [DataMember]
        public string RULE_CODE { get; set; }

        
        [DataMember]
        public int? RULE_SORT_ORDER { get; set; }

        [DataMember]
        public int? RULE_PRIORITY { get; set; }

        [DataMember] 
        public string  RULE_ACTIVE_FLAG { get; set; }

        [DataMember]
        public int? GRR_ID { get; set; }

        [DataMember]
        public string RULE_MSG { get; set; }

        [DataMember]
        public string RULE_SUB_TYPE { get; set; }


    }
}
