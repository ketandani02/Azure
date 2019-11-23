using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using System.Collections.Generic;
using SendGrid.Helpers.Mail;
using System.Text;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Data;

namespace AF_ValidateCSVFile
{
    public static class EmailNotifications
    {
        [FunctionName("EmailNotifications")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Triggered Email Queue execution.");
            string eqid = req.Query["eqid"];
            try
            {


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                eqid = eqid ?? data?.name;

                bool retval = await ExecuteEmailQueueRequest(int.Parse(eqid));
                var result = new
                {
                    Status = "Success",
                    ErrorMsg = "",
                    MQ_ID = eqid,

                };
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogInformation("Exception: " + ex);
                var result = new
                {
                    Status = "fail",
                    ErrorMsg = ex.Message,
                    PrqHearderID = eqid,

                };
                return new OkObjectResult(result);
            }

        }



        #region "Send Email Notification"


        public static async Task<bool> ExecuteEmailQueueRequest(int eqid)
        {
            string sqlConstr = Environment.GetEnvironmentVariable("sqldb_connection");
            string strUpdatedBy = "Azure Function";


            try
            {
                // Step 1: Check is there any request exists with Pending status in PROCESS_REQUEST_QUEUE table for Specified MQ_ID.
                // Steps 1.1: Incase MQ_ID is  Zero will return all records with status pending .
                // Step 2: If not Pending Request Exists then Exit task.
                // Step 3: Get details from Result From,To,Subject,Status,Flag
                // Step 4: Update MQ_ID Status to InProgress and Send email.
                // Step 5: Update PRQ table Status, Updateby Columns.

                // Step 1: Check is there any request exists with Pending status in PROCESS_REQUEST_QUEUE table for Specified MQ_ID.

                using (SqlConnection connObj = new SqlConnection(sqlConstr))
                {
                    connObj.AccessToken = await GetDatabaseToken();
                    SqlDataAdapter myCommand = new SqlDataAdapter("usp_Get_Mail_Queue_Request", connObj);
                    myCommand.SelectCommand.Parameters.Add(new SqlParameter("@MQ_ID", SqlDbType.Int));
                    myCommand.SelectCommand.Parameters["@MQ_ID"].Value = eqid;
                    myCommand.SelectCommand.CommandType = CommandType.StoredProcedure;
                    DataSet ds = new DataSet();
                    myCommand.Fill(ds);

                    // Step 2: If not Pending Request Exists then Exit task.
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataTable dt = ds.Tables[0];
                        foreach (DataRow row in dt.Rows)
                        {
                            // Step 3: Get details from Result From,To,Subject,Status,Flag
                            SendEmailNotification.eqID = int.Parse(row[0].ToString());
                            SendEmailNotification.From = row[1].ToString();
                            SendEmailNotification.Subject = row[2].ToString();
                            SendEmailNotification.eMailBody = row[3].ToString();
                            SendEmailNotification.To = row[4].ToString();
                            SendEmailNotification.CC = row[5].ToString();

                            // Step 4: Update PRQ Status to InProgress and Call SP as per Report type.
                            myCommand.Dispose();
                            bool retval = UpdateMQStatus(connObj, SendEmailNotification.eqID, "INPROGRESS", "", strUpdatedBy);
                          
                            bool retvalemail = await Sendemail();
                            // Step 5: Update PRQ table Status, Updateby Columns.

                            bool retvalupdate = UpdateMQStatus(connObj, SendEmailNotification.eqID, "SENT", "", strUpdatedBy);
                        }

                    }
                    // To do Else
                    else
                    {
                        return false;
                    }

                    }
                return true;

                }
            catch (Exception ex)
            {
                using (SqlConnection connObjError = new SqlConnection(sqlConstr))
                {
                    connObjError.AccessToken = await GetDatabaseToken();
                    bool retvalupdate = UpdateMQStatus(connObjError, SendEmailNotification.eqID, "SENT", ex.Message, strUpdatedBy);
                }


                throw ex;
            }
        }
        public static async Task<bool> Sendemail()
        {
            try
            {
                string mailKey = Environment.GetEnvironmentVariable("mailkey");
                var client = new SendGridClient(mailKey);
                var msg = new SendGridMessage();
                msg.SetFrom(new EmailAddress(SendEmailNotification.From, Environment.GetEnvironmentVariable("mailfromgroupname")));

                StringBuilder eMailContent = new StringBuilder();
                eMailContent.Append(SendEmailNotification.eMailBody);

                var Toemails = SendEmailNotification.To.ToString().Split(";");

                var recipients = new List<EmailAddress>();
                foreach (string email in Toemails)
                {
                  recipients.Add(new EmailAddress(email));
                }
                msg.AddTos(recipients);
                var CCemails = SendEmailNotification.CC.ToString().Split(";");
                if (SendEmailNotification.CC.Length > 0)
                { 
                    var CCrecipients = new List<EmailAddress>();
                    foreach (string ccemail in CCemails)
                    {
                       CCrecipients.Add(new EmailAddress(ccemail));
                    }

                
                    msg.AddCcs(CCrecipients);
                }

                msg.SetSubject(SendEmailNotification.Subject);
                msg.AddContent(MimeType.Html, eMailContent.ToString().Replace("copasupportteam@in.ey.com", Environment.GetEnvironmentVariable("mailfromgroupid")));
                var response = await client.SendEmailAsync(msg);
               

                if (response.StatusCode.ToString() != System.Net.HttpStatusCode.Accepted.ToString())
                {
                    throw new Exception("Send Grid Error out with code : " + response.StatusCode.ToString());
                }

            }
            catch (Exception ex)
            {
                // Update Mail Status to Error and Log Error Message here 
                throw ex;
            }




            return true;
        }

        /// <summary>
        /// UpdateMQStatus():Below function has been created to Update Process Request Queue table.
        /// </summary>
        /// <param name="connObj">sql connection object</param>
        /// <param name="MQ_ID">Process Request ID</param>
        /// <param name="Status">Proecess Request status </param>
        /// <param name="ErrorDesc">Error Desc</param>
        /// <param name="UpdatedBy">Updatedby User Name</param>
        /// <returns></returns>
        private static bool UpdateMQStatus(SqlConnection connObj, int MQ_ID, string Status,  string ErrorDesc,string UpdatedBy)
        {

            bool resultSet = false;
            using (SqlCommand sqlRenameCommand = new SqlCommand("usp_Update_Mail_Queue_Request", connObj))
            {

                sqlRenameCommand.CommandType = CommandType.StoredProcedure;
                sqlRenameCommand.Parameters.Add("@MQ_ID", SqlDbType.Int).Value = MQ_ID;
                sqlRenameCommand.Parameters.Add("@MQ_STATUS", SqlDbType.VarChar).Value = Status;
                sqlRenameCommand.Parameters.Add("@CR_LAST_UPDATED_BY_USERNM", SqlDbType.VarChar).Value = UpdatedBy;
                sqlRenameCommand.Parameters.Add("@ERROR_DESC", SqlDbType.VarChar).Value = ErrorDesc;
                connObj.Open();
                sqlRenameCommand.ExecuteNonQuery();
                connObj.Close();
                resultSet = true;
            }
            return resultSet;

        }

        public static async Task<bool> Sendemail1()
        {
            try
            {

                // var client = new SendGridClient("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");


                string mailKey = Environment.GetEnvironmentVariable("mailkey");
                var client = new SendGridClient(mailKey);

                var msg = new SendGridMessage();

                msg.SetFrom(new EmailAddress(SendEmailNotification.From, Environment.GetEnvironmentVariable("mailfromgroupname")));

                var Toemails = SendEmailNotification.To.ToString().Split(";");

                var recipients = new List<EmailAddress>();
                foreach (string email in Toemails)
                {
                    recipients.Add(new EmailAddress(email));
                }

                msg.AddTos(recipients);

                var CCemails = SendEmailNotification.CC.ToString().Split(";");

                var CCrecipients = new List<EmailAddress>();
                foreach (string ccemail in CCemails)
                {
                    CCrecipients.Add(new EmailAddress(ccemail));
                }

                msg.AddCcs(CCrecipients);
                msg.SetSubject(SendEmailNotification.Subject);

                msg.AddContent(MimeType.Text, "This is just a simple test message!");
                msg.AddContent(MimeType.Html, "<p>This is just a simple test message3!</p>");
                var response = await client.SendEmailAsync(msg);
            }
            catch (Exception ex)
            {

            }




            return true;
        }
        #endregion
        public static async Task<string> GetDatabaseToken()
        {
            string Clientid = Environment.GetEnvironmentVariable("DBClientid");
            string ClientSecret = Environment.GetEnvironmentVariable("DBClientSecret");
            string TenantId = Environment.GetEnvironmentVariable("DBTenantId");
            var ctx = new AuthenticationContext(string.Concat("https://login.microsoftonline.com/", TenantId));
            var result = await ctx.AcquireTokenAsync(" https://database.windows.net/", new ClientCredential(Clientid, ClientSecret));
            return result.AccessToken;
        }
    }


    public class SendEmailNotification
    {
        public static int eqID { get; set; }
        public static string From {get;set;}
        public static string Subject { get; set; }
        public static string eMailBody { get; set; }
        public static string To { get; set; }
        public static string CC { get; set; }

    }
}

   
