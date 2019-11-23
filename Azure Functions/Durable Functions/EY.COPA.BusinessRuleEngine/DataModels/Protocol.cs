using EY.COPA.BusinessRuleEngine.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace EY.COPA.BusinessRuleEngine.DataModels
{
    public class Protocol
    {
      
        public Protocol(string unvEyIDField, DataRow fieldSet, Dictionary<string, string> customFields=null)
        {
            if (fieldSet == null) return;
            this.UNV_EY_ID = fieldSet[unvEyIDField].CastToValue<int>();
            this.CLIENT_REF_ID = fieldSet["CLIENT_REF_ID"].CastToValue<int>();
            this.CLIENT_SRC_REF_ID = fieldSet["CLIENT_SRC_REF_ID"].CastToValue<int>();
            this.SRC_FILE_ID = fieldSet["SRC_FILE_ID"].CastToValue<int>();
            this.PROTOCOL_REF_ID = fieldSet["PROTOCOL_REF_ID"].CastToValue<int>();
            var sFields = new Dictionary<string, string>();
            foreach (DataColumn col in fieldSet.Table.Columns)
                sFields.Add(col.ColumnName, fieldSet[col.ColumnName].ToString());

            if (customFields != null)
                this.UNVFIELD = sFields.Concat(customFields).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
            else
                this.UNVFIELD = sFields;
        }

        public int UNV_EY_ID { get; private set; }


        public int CLIENT_REF_ID { get; private set; }


        public int CLIENT_SRC_REF_ID { get; private set; }


        public int SRC_FILE_ID { get; private set; }


        public int PROTOCOL_REF_ID { get; private set; }
        public Dictionary<string, string> UNVFIELD { get; private set; }

    }
}
