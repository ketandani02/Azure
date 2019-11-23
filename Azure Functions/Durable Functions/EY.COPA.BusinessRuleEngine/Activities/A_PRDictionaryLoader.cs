using EY.COPA.BusinessRuleEngine.DataContracts;
using EY.COPA.BusinessRuleEngine.DataModels;

using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using EY.COPA.BusinessRuleEngine.Utility;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using EY.COPA.BusinessRuleEngine.Common;

namespace EY.COPA.BusinessRuleEngine.Activities
{
   

    public static class A_PRDictionaryLoader
    {
        [FunctionName("A_PRDictionaryLoader")]
        public static async Task<List<ProtocolDictionary>> LoadRulesAsync(
            [ActivityTrigger] PRDLoadRequest prdLoadReq,
            ILogger log
            )
        {

            if (prdLoadReq == null)
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION, prdLoadReq.MasterOrcId, nameof(A_PRDictionaryLoader), "Invalid Request");
                throw new Exception("Invalid Request");
            }

            List<ProtocolDictionary> returnValue = null;
          

            DataTable dicTable = null;

            try
            {

                using (SqlConnection conn = new SqlConnection(ConfigurationReader.DbConn))
                {
                    conn.AccessToken = await AuthHelper.GetDatabaseToken();

                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Connection = conn;
                        cmd.CommandText = "[dbo].[usp_Get_Protocol_Ref_Dictionary_Base_ByProrocol_Ref_Id]";
                        var paramPrototcol = new SqlParameter("@protocol_Ref_Id", SqlDbType.Int);
                        paramPrototcol.SqlValue = prdLoadReq.ProtocolId;
                        cmd.Parameters.Add(paramPrototcol);
                        dicTable = new DataTable();
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(dicTable);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(Constants.LOG_MSG_EXCEPTION, prdLoadReq.MasterOrcId, nameof(A_PRDictionaryLoader), ex.ToString());
                throw ex;

            }
            if (dicTable == null || dicTable.Rows.Count <= 0)
            {
                log.LogCritical(Constants.LOG_MSG_PROCESS, prdLoadReq.MasterOrcId, nameof(A_PRDictionaryLoader), "No dictionary values found");
                throw new Exception("No dictionary values found");
            }


            returnValue = dicTable.AsEnumerable().Select(dr => new ProtocolDictionary
            {
                PROTOCOL_RD_ID = dr["PROTOCOL_RD_ID"].CastToValue<int>(),
                PROTOCOL_REF_ID = dr["PROTOCOL_REF_ID"].CastToValue<int>(),
                PDBIZNM_REF_CODE = dr["PDBIZNM_REF_CODE"].CastToValue<string>(),
                PRD_DF_SYSTEM = dr["PRD_DF_SYSTEM"].CastToValue<string>(),
                PRD_DF_NM = dr["PRD_DF_NM"].CastToValue<string>(),
                PRD_DF_COL_ID = dr["PRD_DF_COL_ID"].CastToValue<string>(),
                PRD_DF_BUSINESS_NA = dr["PRD_DF_BUSINESS_NA"].CastToValue<string>(),
                PRD_DF_DESC = dr["PRD_DF_DESC"].CastToValue<string>(),
                PRD_DF_REFERENCE_TABLE = dr["PRD_DF_REFERENCE_TABLE"].CastToValue<string>(),
                PRD_DF_DATA_TYPE = dr["PRD_DF_DATA_TYPE"].CastToValue<string>(),
                PRD_DF_FORMAT = dr["PRD_DF_FORMAT"].CastToValue<string>(),
                PRD_DF_USE_OF_NA = dr["PRD_DF_USE_OF_NA"].CastToValue<string>(),
                PRD_DF_ALWAYS_REQ = dr["PRD_DF_ALWAYS_REQ"].CastToValue<string>(),
                PRD_DF_MIN_ALLOWED_LEN = dr["PRD_DF_MIN_ALLOWED_LEN"].CastToValue<int>(),
                PRD_DF_MAX_ALLOWED_LEN = dr["PRD_DF_MAX_ALLOWED_LEN"].CastToValue<int>(),
                PRD_DF_ONLY_ALLOWED_VALS = dr["PRD_DF_ONLY_ALLOWED_VALS"].CastToValue<string>(),
                PRD_DF_NOT_ALLOWED_VALS = dr["PRD_DF_NOT_ALLOWED_VALS"].CastToValue<string>(),
                PRD_DF_DT_TM_FORMAT = dr["PRD_DF_DT_TM_FORMAT"].CastToValue<DateTime>(),
                PRD_DF_ORDER_INPUT_FILE = dr["PRD_DF_ORDER_INPUT_FILE"].CastToValue<int>(),
                PRD_DF_ORDER_OUTPUT_FILE = dr["PRD_DF_ORDER_OUTPUT_FILE"].CastToValue<int>(),
            }).ToList();





            return returnValue;
        }

    }
}
