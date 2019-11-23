
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

    public static class A_RuleLoader
    {


        [FunctionName("A_RuleLoader")]
        public static async Task<List<RuleDetails>> LoadRulesAsync(
            [ActivityTrigger] RuleLoadRequest ruleLoadReq,
            ILogger log
            )
        {

            if (ruleLoadReq == null)
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION,  ruleLoadReq.MasterOrcId, nameof(A_RuleLoader), "Invalid Request");
                throw new Exception("Invalid Request");
            }

            List<RuleDetails> returnValue = null;
          
            DataTable ruleTable = null;
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
                        cmd.CommandText = "[dbo].[usp_Get_All_Business_Rules]";
                        var paramPrototcol = new SqlParameter("@protocol_Id", SqlDbType.Int);//{ ParameterName = "@protocol_Id",,SqlDbType=SqlDbType.Int, Direction = ParameterDirection.Input, Value = ruleLoadReq.ProtocolId });
                        paramPrototcol.SqlValue = ruleLoadReq.ProtocolId;
                        cmd.Parameters.Add(paramPrototcol);
                        ruleTable = new DataTable();
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(ruleTable);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(Constants.LOG_MSG_EXCEPTION, ruleLoadReq.MasterOrcId, nameof(A_RuleLoader), ex.ToString());
                throw ex;

            }
            if (ruleTable == null || ruleTable.Rows.Count <= 0)
            {
                log.LogCritical(Constants.LOG_MSG_PROCESS, ruleLoadReq.MasterOrcId, nameof(A_RuleLoader),"No rules found");
                throw new Exception("No rules found");
            }

            returnValue = ruleTable.AsEnumerable().Select(dr => new RuleDetails
            {
                PROTOCOL_REF_ID = dr["PROTOCOL_REF_ID"].CastToValue<int>(),
                PROTOCOL_RD_ID = dr["PROTOCOL_RD_ID"].CastToValue<int>(),
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
                PRD_DF_MIN_ALLOWED_LEN = dr["PRD_DF_MIN_ALLOWED_LEN"].CastToValue<short?>(),
                PRD_DF_MAX_ALLOWED_LEN = dr["PRD_DF_MAX_ALLOWED_LEN"].CastToValue<short?>(),
                PRD_DF_ONLY_ALLOWED_VALS = dr["PRD_DF_ONLY_ALLOWED_VALS"].CastToValue<string>(),
                PRD_DF_NOT_ALLOWED_VALS = dr["PRD_DF_NOT_ALLOWED_VALS"].CastToValue<string>(),
                PRD_DF_DT_TM_FORMAT = dr["PRD_DF_DT_TM_FORMAT"].CastToValue<string>(),
                PRD_DF_ORDER_INPUT_FILE = dr["PRD_DF_ORDER_INPUT_FILE"].CastToValue<short?>(),
                PRD_DF_ORDER_OUTPUT_FILE = dr["PRD_DF_ORDER_OUTPUT_FILE"].CastToValue<short?>(),
                PRD_EFF_BEGIN_DT = dr["PRD_EFF_BEGIN_DT"].CastToValue<DateTime?>(),
                PRD_EFF_END_DT = dr["PRD_EFF_END_DT"].CastToValue<DateTime?>(),
                PRD_ACTIVE_FLAG = dr["PRD_ACTIVE_FLAG"].CastToValue<string>(),
                RULE_TYPE = dr["RULE_TYPE"].CastToValue<string>(),
                RULE_CODE = dr["RULE_CODE"].CastToValue<string>(),
                RULE_SORT_ORDER = dr["RULE_SORT_ORDER"].CastToValue<int?>(),
                RULE_PRIORITY = dr["RULE_PRIORITY"].CastToValue<int?>(),
                RULE_ACTIVE_FLAG = dr["RULE_ACTIVE_FLAG"].CastToValue<string>(),
                GRR_ID = dr["GRR_ID"].CastToValue<int?>(),
                RULE_SUB_TYPE = dr["GRR_SUB_TYPE"].CastToValue<string>(),
                RULE_MSG = dr["GRR_MSG"].CastToValue<string>(),
                PDRCR_ID = dr["PDRCR_ID"].CastToValue<int>(),
                PDRR_ID = dr["PDRR_ID"].CastToValue<int>(),
                PDBIZNM_REF_CODE = dr["PDBIZNM_REF_CODE"].CastToValue<string>()
            }).ToList();





            return returnValue;
        }

    }
}
