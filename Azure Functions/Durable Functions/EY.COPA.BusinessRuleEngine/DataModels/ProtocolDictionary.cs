using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EY.COPA.BusinessRuleEngine.DataModels
{
    public  class ProtocolDictionary
    {

        [DataMember]
        public int PROTOCOL_RD_ID { get; set; }

        [DataMember]
        public int PROTOCOL_REF_ID { get; set; }

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
        public int PRD_DF_MIN_ALLOWED_LEN { get; set; }

        [DataMember]
        public int PRD_DF_MAX_ALLOWED_LEN { get; set; }

        [DataMember]
        public string PRD_DF_ONLY_ALLOWED_VALS { get; set; }

        [DataMember]
        public string PRD_DF_NOT_ALLOWED_VALS { get; set; }

        [DataMember]
        public DateTime PRD_DF_DT_TM_FORMAT { get; set; }

        [DataMember]
        public int PRD_DF_ORDER_INPUT_FILE { get; set; }

        [DataMember]
        public int PRD_DF_ORDER_OUTPUT_FILE { get; set; }

       


    }
}
