
using EY.COPA.BusinessRuleEngine.Common;
using EY.COPA.BusinessRuleEngine.DataContracts;
using EY.COPA.BusinessRuleEngine.Utility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.Activities
{
    public static class A_DataStore
    {
        [FunctionName("A_DataStore")]
        public static async Task<bool> PersistPRResultAsync(
           [ActivityTrigger]  PersistDataRequest request,
           ILogger log)
        {



            if (request.RuleExecutionResults == null || request.RuleExecutionResults.Count() <= 0)
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION, request.MasterOrcId, nameof(A_DataStore), "Invalid Request");
                throw new Exception("Invalid Request");
            }


            try
            {
                var validData = request.RuleExecutionResults.Where(r => r.IsValid);
                var inValidData = request.RuleExecutionResults.Where(r => !r.IsValid);

                if (inValidData != null && inValidData.Any())
                {
                    using (SqlConnection conn = new SqlConnection(ConfigurationReader.DbConn))
                    {
                        conn.AccessToken = await AuthHelper.GetDatabaseToken();
                        conn.Open();

                        var persistTask = new List<Task<int>>();

                        foreach (var result in inValidData)
                        {
                            using (SqlCommand cmd = new SqlCommand())
                            {

                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Connection = conn;
                                cmd.CommandText = "[dbo].[usp_Add_Protocol_Rules_Result]";
                                var p_client_Ref_Id = new SqlParameter("@client_Ref_Id", SqlDbType.Int);
                                p_client_Ref_Id.SqlValue = result.CLIENT_REF_ID;
                                var p_client_Src_Ref_Id = new SqlParameter("@client_Src_Ref_Id", SqlDbType.Int);
                                p_client_Src_Ref_Id.SqlValue = result.CLIENT_SRC_REF_ID;
                                var p_src_File_Id = new SqlParameter("@src_File_Id", SqlDbType.Int);
                                p_src_File_Id.SqlValue = result.SRC_FILE_ID;
                                var p_protocol_Ref_Id = new SqlParameter("@protocol_Ref_Id", SqlDbType.Int);
                                p_protocol_Ref_Id.SqlValue = result.PROTOCOL_REF_ID;
                                var p_protocol_Ref_Dictnry_Id = new SqlParameter("@protocol_Ref_Dictnry_Id", SqlDbType.Int);
                                p_protocol_Ref_Dictnry_Id.SqlValue = result.PROTOCOL_RD_ID;
                                var p_protocol_Dictnry_Rule_Ref_Id = new SqlParameter("@protocol_Dictnry_Rule_Ref_Id", SqlDbType.Int);
                                p_protocol_Dictnry_Rule_Ref_Id.SqlValue = result.PDRR_ID;

                                var p_prr_Table_Rec_Id = new SqlParameter("@prr_Table_Rec_Id", SqlDbType.Int);
                                p_prr_Table_Rec_Id.SqlValue = result.PRR_TABLE_REC_ID;
                                var p_prr_Remarks = new SqlParameter("@prr_Remarks", SqlDbType.VarChar);
                                p_prr_Remarks.SqlValue = result.PRR_REMARKS;
                                var p_prr_Active_Flag = new SqlParameter("@prr_Active_Flag", SqlDbType.Char);
                                p_prr_Active_Flag.SqlValue = result.PRR_ACTIVE_FLAG.ConvertToChar();
                                var p_prr_Created_By_User = new SqlParameter("@prr_Created_By_User", SqlDbType.VarChar);
                                p_prr_Created_By_User.SqlValue = "EYCOPA.PBI@eymsprod.onmicrosoft.com";

                                var p_rule_request_Id = new SqlParameter("@rule_Engine_Id", SqlDbType.Int);
                                p_rule_request_Id.SqlValue = request.RequestQueueId;
                                var p_pr_df_nm= new SqlParameter("@err_value", SqlDbType.VarChar);
                                p_pr_df_nm.SqlValue = result.PRD_DF_NM;
                                var p_pln_decisn_dt = new SqlParameter("@pln_decisn_dt", SqlDbType.VarChar);
                                p_pln_decisn_dt.SqlValue = result.PLN_DECISN_DT;

                                cmd.Parameters.Add(p_client_Ref_Id);
                                cmd.Parameters.Add(p_client_Src_Ref_Id);
                                cmd.Parameters.Add(p_src_File_Id);
                                cmd.Parameters.Add(p_protocol_Ref_Id);
                                cmd.Parameters.Add(p_protocol_Ref_Dictnry_Id);
                                cmd.Parameters.Add(p_protocol_Dictnry_Rule_Ref_Id);

                                cmd.Parameters.Add(p_prr_Table_Rec_Id);
                                cmd.Parameters.Add(p_prr_Remarks);
                                cmd.Parameters.Add(p_prr_Active_Flag);
                                cmd.Parameters.Add(p_prr_Created_By_User);
                                cmd.Parameters.Add(p_rule_request_Id);
                                cmd.Parameters.Add(p_pr_df_nm);
                                cmd.Parameters.Add(p_pln_decisn_dt);
                                await cmd.ExecuteNonQueryAsync();
                            }

                        }

                    }
                }

                //code to update de-activate the issues for valid data
                if (validData != null && validData.Any())
                {
                    using (SqlConnection conn = new SqlConnection(ConfigurationReader.DbConn))
                    {
                        conn.AccessToken = await AuthHelper.GetDatabaseToken();
                        conn.Open();
                        var dtValidData = new DataTable();
                        dtValidData.Columns.Add("CLIENT_REF_ID", typeof(Int32));
                        dtValidData.Columns.Add("CLIENT_SRC_REF_ID", typeof(Int32));
                        dtValidData.Columns.Add("SRC_FILE_ID", typeof(Int32));
                        dtValidData.Columns.Add("PROTOCOL_REF_ID", typeof(Int32));
                        dtValidData.Columns.Add("PROTOCOL_RD_ID", typeof(Int32));
                        dtValidData.Columns.Add("PDRR_ID", typeof(Int32));
                        dtValidData.Columns.Add("PRR_TABLE_REC_ID", typeof(Int32));
                        dtValidData.Columns.Add("CREATED_BY_USERNM", typeof(String));
                        dtValidData.Columns.Add("RequestQueueId", typeof(Int32));
                        dtValidData.Columns.Add("ERR_VALUE", typeof(String));
                        dtValidData.Columns.Add("PLN_DECISN_DT", typeof(String));

                        foreach (var result in validData)
                        {
                            var dr = dtValidData.NewRow();
                            dr["CLIENT_REF_ID"] = result.CLIENT_REF_ID;
                            dr["CLIENT_SRC_REF_ID"] = result.CLIENT_SRC_REF_ID;
                            dr["SRC_FILE_ID"] = result.SRC_FILE_ID;
                            dr["PROTOCOL_REF_ID"] = result.PROTOCOL_REF_ID;
                            dr["PROTOCOL_RD_ID"] = result.PROTOCOL_RD_ID;
                            dr["PDRR_ID"] = result.PDRR_ID;
                            dr["PRR_TABLE_REC_ID"] = result.PRR_TABLE_REC_ID;
                            dr["CREATED_BY_USERNM"] = "EYCOPA.PBI@eymsprod.onmicrosoft.com";
                            dr["RequestQueueId"] = request.RequestQueueId;
                            dr["ERR_VALUE"] = result.PRD_DF_NM;
                            dr["PLN_DECISN_DT"] = result.PLN_DECISN_DT;
                            dtValidData.Rows.Add(dr);
                        }

                        using (SqlCommand cmd = new SqlCommand())
                        {

                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Connection = conn;
                            cmd.CommandText = "[dbo].[usp_Update_Valid_Protocol_Rules_Result]";

                            var p_valid_rule_results = cmd.Parameters.AddWithValue("@valid_protocol_rules_results", dtValidData);
                            p_valid_rule_results.SqlDbType = SqlDbType.Structured;

                            await cmd.ExecuteNonQueryAsync();
                        }


                    }

                }
            }
            catch (Exception ex)
            {
                log.LogCritical(Constants.LOG_MSG_EXCEPTION, request.MasterOrcId, nameof(A_DataStore), ex.ToString());
                throw;

            }

            return true;

        }

    }
}
