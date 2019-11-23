using System;
using System.Collections.Generic;
using System.Text;

using EY.COPA.BusinessRuleEngine.DataContracts;
using EY.COPA.BusinessRuleEngine.DataModels;


using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace EY.COPA.BusinessRuleEngine.Utility
{
    public static class Extensions
    {
        public static T CastToValue<T>(this object obj) 
        {
            return obj == null || obj==DBNull.Value ? default(T) : (T)obj;
        }

        public static string ConvertToString(this object obj)
        {
            var returnValue = default(string);
            if (obj != null && obj != DBNull.Value)
                returnValue =System.Convert.ToString(obj);
            return returnValue;
        }

        public static char ConvertToChar(this object obj)
        {
            var returnValue = default(char);
            if (obj != null && obj != DBNull.Value)
                returnValue = System.Convert.ToChar(obj);
            return returnValue;
        }


        public static Dictionary<string,string> ToDictionary(this DataTable table,string keyCol, string valueCol,string filter="")
        {
            var returnValue = default(Dictionary<string, string>);
            if (table != null && table.Rows.Count >= 0 && table.Columns.Contains(keyCol) && table.Columns.Contains(valueCol))
            {
                var rows = !string.IsNullOrEmpty(filter) ? table.Select(filter) : table.AsEnumerable().ToArray();
                    
                returnValue = new Dictionary<string, string>();
                foreach (DataRow dr in rows)
                    returnValue.Add(dr[keyCol].CastToValue<string>(), dr[valueCol].CastToValue<string>());
            }
            return returnValue;
        }
    }
}
