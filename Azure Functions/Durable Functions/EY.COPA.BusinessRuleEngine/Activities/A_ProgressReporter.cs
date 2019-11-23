using EY.COPA.BusinessRuleEngine.Common;
using EY.COPA.BusinessRuleEngine.DataContracts;
using EY.COPA.BusinessRuleEngine.Utility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.Activities
{
    public static class A_ProgressReporter
    {
        [FunctionName("A_ProgressReporter")]
        public static async Task<bool> ReportProgress(
            [ActivityTrigger] RuleEngineStatusRequest statusReq,
            ILogger log
            )
        {
          

            if (statusReq == null || statusReq.OrcReq == null)
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION, statusReq.MasterOrcId, nameof(A_ProgressReporter), "Request Extracted");
                throw new Exception("Invalid Request");
            }
         
            
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
                        cmd.CommandText = "[dbo].[usp_update_Rule_Engine_Queue]";
                        var p_re_queue_Id = new SqlParameter("@re_queue_Id", SqlDbType.Int);
                        p_re_queue_Id.SqlValue = statusReq.OrcReq.RequestQueueId;
                        
                        var p_re_status = new SqlParameter("@re_status", SqlDbType.VarChar);
                        p_re_status.SqlValue = statusReq.Status;
                        var p_re_Executed_Date_Time = new SqlParameter("@re_Executed_Date_Time", SqlDbType.DateTime);
                        p_re_Executed_Date_Time.SqlValue = DateTime.Now;
                        var p_re_Updated_By_User = new SqlParameter("@re_Updated_By_User", SqlDbType.VarChar);
                        p_re_Updated_By_User.SqlValue = "Service@eycopa";

                        cmd.Parameters.Add(p_re_queue_Id);
                        cmd.Parameters.Add(p_re_status);
                        cmd.Parameters.Add(p_re_Executed_Date_Time);
                        cmd.Parameters.Add(p_re_Updated_By_User);

                       await cmd.ExecuteNonQueryAsync();
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError(Constants.LOG_MSG_EXCEPTION, statusReq.MasterOrcId, nameof(A_ProgressReporter), ex.ToString());
                throw ex;
            }

            
            return true;
        }

    }
}
