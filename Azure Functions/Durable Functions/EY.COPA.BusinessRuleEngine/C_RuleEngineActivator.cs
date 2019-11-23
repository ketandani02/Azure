
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using System.Collections.Generic;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using EY.COPA.BusinessRuleEngine.DataContracts;
using Newtonsoft.Json;
using EY.COPA.BusinessRuleEngine.Common;

namespace EY.COPA.BusinessRuleEngine
{
    public static class C_RuleEngineActivator
    {
        [FunctionName("C_RuleEngineActivator")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient activator,
            ILogger log)
        {
            log.LogInformation(Constants.LOG_MSG,"New rule processing request recieved");
            string tenantRef = null;
            string fileRef = null;
            string protocolRef=null;

            if (req.Headers.Contains("X-COPA-TENANT"))
                  tenantRef = req.Headers.GetValues("X-COPA-TENANT").FirstOrDefault();
            if (req.Headers.Contains("X-COPA-FILEID"))
                fileRef = req.Headers.GetValues("X-COPA-FILEID").FirstOrDefault();
            if (req.Headers.Contains("X-COPA-PROTOCOL"))
                protocolRef = req.Headers.GetValues("X-COPA-PROTOCOL").FirstOrDefault();


            string content = await req.Content.ReadAsStringAsync();
            dynamic jContent = JsonConvert.DeserializeObject(content);
            string re_queue_id = jContent.RequestQueueId;

            //Validate parameters
            if (fileRef == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing File reference");
            if (tenantRef == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing Tenant reference");
            if (protocolRef == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing Protocol reference");
            if (!int.TryParse(protocolRef, out int protocolId))
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid protocol Id");
            if (!int.TryParse(re_queue_id, out int reqQueueId))
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Request Id");


            
            var orchestrationId = await activator.StartNewAsync("O_MasterOrchestrator",
                new OrchestrationRequest()
                {
                    FileRef = fileRef
                ,
                    Tenant = tenantRef
                ,
                    ProtocolId = protocolId
                    ,
                    RequestQueueId = reqQueueId
                });

            log.LogInformation(Constants.LOG_MSG, $"Successfully activated rule processing. Instance ID: {orchestrationId}");
            return activator.CreateCheckStatusResponse(req, orchestrationId);
        }

       
    }



}
