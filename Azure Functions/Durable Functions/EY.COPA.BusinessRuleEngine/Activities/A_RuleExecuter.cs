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
using EY.COPA.BusinessRuleEngine.Common;

namespace EY.COPA.BusinessRuleEngine.Activities
{
    public static class A_RuleExecuter
    {
        [FunctionName("A_RuleExecuter")]
        public static async Task<IEnumerable<ProtocolRuleResult>> RuleExecAsync(
            [ActivityTrigger] RuleExecRequest ruleExecRequest,
            ILogger log
            )
        {
           

            if (ruleExecRequest.Rules == null || ruleExecRequest.Rules.Count() <= 0)
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION, ruleExecRequest.InstanceId, nameof(A_RuleExecuter), "Invalid Request");
                throw new Exception("Invalid Request");
            }
            var returnValue = new List<ProtocolRuleResult>();

            
            string protocolProc = string.Format(Constants.RE_PROTOCOL_SPROC, ruleExecRequest.ProtocolRoute);
            string protocolTableRecId = string.Format(Constants.RE_PROTOCOL_TBLRECID, ruleExecRequest.ProtocolRoute);

            
            DataSet dataSet = null;
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
                        cmd.CommandText = protocolProc;
                        var paramFileId = new SqlParameter(Constants.RE_PROTOCOL_SPROC_INPARAM_FILEID, SqlDbType.Int);
                        paramFileId.SqlValue = ruleExecRequest.FileRef;
                        var paramClientId = new SqlParameter(Constants.RE_PROTOCOL_SPROC_INPARAM_CLIENTID, SqlDbType.Int);
                        paramClientId.SqlValue = ruleExecRequest.TenantRef;
                        var paramPrototcol = new SqlParameter(Constants.RE_PROTOCOL_SPROC_INPARAM_PRID, SqlDbType.Int);
                        paramPrototcol.SqlValue = ruleExecRequest.ProtocolId;
                        cmd.Parameters.Add(paramClientId);
                        cmd.Parameters.Add(paramPrototcol);
                        cmd.Parameters.Add(paramFileId);
                        dataSet = new DataSet();
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(dataSet);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(Constants.LOG_MSG_EXCEPTION, ruleExecRequest.InstanceId, nameof(A_RuleExecuter), ex.ToString());
                throw ex;

            }
            if (dataSet.Tables == null || dataSet.Tables.Count != 2)
            {
                log.LogCritical(Constants.LOG_MSG_PROCESS, ruleExecRequest.InstanceId, nameof(A_RuleExecuter), "Wrong Data Load");
                throw new Exception("Wrong Data Load");
            }
            var universeData = dataSet.Tables[0].AsEnumerable().Select(dr => new Protocol(protocolTableRecId, dr,
                 dataSet.Tables[1].ToDictionary(Constants.RE_PROTOCOL_UDU_DF_NM
                 , Constants.RE_PROTOCOL_UDU_DF_VAL
                 , string.Format(Constants.RE_PROTOCOL_UDU_FILTER, dr[protocolTableRecId].CastToValue<int>()))));

        


            foreach (DataModels.Rule rule in ruleExecRequest.Rules)
            {
               
                try
                {
                    Func<Protocol, bool> ruleExpression;
                    if (!string.IsNullOrEmpty(rule.RULE_LAMBDA_ARGS))
                        ruleExpression = System.Linq.Dynamic.DynamicExpression.ParseLambda<Protocol, bool>(rule.RULE_LAMBDA, rule.RULE_LAMBDA_ARGS).Compile();
                    else
                        ruleExpression = System.Linq.Dynamic.DynamicExpression.ParseLambda<Protocol, bool>(rule.RULE_LAMBDA).Compile();
                    var validData = universeData.Where(ruleExpression);
                    var invalidData = universeData.Where(u => validData.FirstOrDefault(v => v.UNVFIELD[protocolTableRecId] == u.UNVFIELD[protocolTableRecId]) == default(Protocol));


                    if (invalidData != null && invalidData.Count() > 0)
                    {
                        var result = invalidData.Select(x => new ProtocolRuleResult
                        {
                            CLIENT_REF_ID = x.CLIENT_REF_ID,
                            CLIENT_SRC_REF_ID = x.CLIENT_SRC_REF_ID,
                            SRC_FILE_ID = x.SRC_FILE_ID,
                            PRR_TABLE_REC_ID = x.UNV_EY_ID,
                            PDRCR_ID = rule.PDRCR_ID,
                            PDRR_ID = rule.PDRR_ID,
                            PROTOCOL_RD_ID = rule.PROTOCOL_RD_ID,
                            PROTOCOL_REF_ID = rule.PROTOCOL_REF_ID,
                            PRR_ACTIVE_FLAG = rule.RULE_ACTIVE_FLAG,
                            PRR_REMARKS = rule.RULE_MSG,
                            IsValid = false,
                            PRD_DF_NM = universeData.FirstOrDefault(y => y.UNV_EY_ID == x.UNV_EY_ID).UNVFIELD.FirstOrDefault(z => z.Key == rule.PRD_DF_NM).Value,
                            PLN_DECISN_DT = universeData.FirstOrDefault(y => y.UNV_EY_ID == x.UNV_EY_ID)?.UNVFIELD.FirstOrDefault(z => z.Key == "PLN_DECISN_DT").Value
                        }).ToList();
                        returnValue.AddRange(result);
                    }

                    //Adding valid data to disable existing issues for such data
                    if (validData != null && validData.Any())
                    {
                        var result = validData.Select(x => new ProtocolRuleResult
                        {
                            CLIENT_REF_ID = x.CLIENT_REF_ID,
                            CLIENT_SRC_REF_ID = x.CLIENT_SRC_REF_ID,
                            SRC_FILE_ID = x.SRC_FILE_ID,
                            PRR_TABLE_REC_ID = x.UNV_EY_ID,
                            PDRCR_ID = rule.PDRCR_ID,
                            PDRR_ID = rule.PDRR_ID,
                            PROTOCOL_RD_ID = rule.PROTOCOL_RD_ID,
                            PROTOCOL_REF_ID = rule.PROTOCOL_REF_ID,
                            PRR_ACTIVE_FLAG = rule.RULE_ACTIVE_FLAG,
                            PRR_REMARKS = rule.RULE_MSG,
                            IsValid = true,
                            PRD_DF_NM = universeData.FirstOrDefault(y => y.UNV_EY_ID == x.UNV_EY_ID)?.UNVFIELD.FirstOrDefault(z => z.Key == rule.PRD_DF_NM).Value,
                            PLN_DECISN_DT = universeData.FirstOrDefault(y => y.UNV_EY_ID == x.UNV_EY_ID)?.UNVFIELD.FirstOrDefault(z => z.Key == "PLN_DECISN_DT").Value
                        }).ToList();
                        returnValue.AddRange(result);
                    }

                }
                catch (Exception ex)
                {
                    log.LogCritical(Constants.LOG_MSG_EXCEPTION, ruleExecRequest.InstanceId, nameof(A_RuleExecuter), $"Protocol Dictionary Rule ID:{rule.PDRR_ID}, Info:{ex.ToString()}");
                    throw;
                }


            }



            return returnValue;

        }

        
    }
}
