using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using EY.COPA.BusinessRuleEngine.Utility;
using EY.COPA.BusinessRuleEngine.DataContracts;
using EY.COPA.BusinessRuleEngine.Common;

namespace EY.COPA.BusinessRuleEngine.Activities
{
    public static class A_ProtocolRouter
    {
        [FunctionName("A_ProtocolRouter")]
        public static async Task<string> GetRouteAsync(
            [ActivityTrigger] RouteRequest request,
            ILogger log
            )
        {
            object protocol = null;
            if (request == null )
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION, request.InstanceId, nameof(A_ProtocolRouter), "Invalid Request");
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
                        cmd.CommandText = "[dbo].[usp_Get_ProtocolById]";
                        var paramPrototcol = new SqlParameter("@protocol_Ref_Id", SqlDbType.Int);
                        paramPrototcol.SqlValue = request.ProtocolId;
                        cmd.Parameters.Add(paramPrototcol);
                        protocol=cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(Constants.LOG_MSG_EXCEPTION, request.InstanceId, nameof(A_ProtocolRouter), ex.ToString());
                throw ex;

            }
            string returnValue = string.Empty;
            if (protocol == null || string.IsNullOrEmpty(returnValue = protocol.CastToValue<string>()))
            {
                log.LogCritical(Constants.LOG_MSG_PROCESS, request.InstanceId, nameof(A_ProtocolRouter), "Invalid Protocol");
                throw new Exception("Invalid Protocol");
            }

            return returnValue;

        }
    }
}
