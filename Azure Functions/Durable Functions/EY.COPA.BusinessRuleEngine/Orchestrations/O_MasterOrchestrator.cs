
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
using EY.COPA.BusinessRuleEngine.DataModels;
using EY.COPA.BusinessRuleEngine.Common;

namespace EY.COPA.BusinessRuleEngine.Orchestrations
{
    public static class O_MasterOrchestrator
    {

        [FunctionName("O_MasterOrchestrator")]
        public static async Task<string> RunOrchestratorAsync(
            [OrchestrationTrigger] DurableOrchestrationContext masterctx, ILogger log)
        {
          
            var request = masterctx.GetInput<OrchestrationRequest>();
            log.LogInformation(Constants.LOG_MSG_PROCESS, masterctx.InstanceId,nameof(O_MasterOrchestrator), "Request Extracted" );
           
         
            var rules = await masterctx.CallActivityAsync<List<RuleDetails>>("A_RuleLoader"
             , new RuleLoadRequest { ProtocolId = request.ProtocolId, MasterOrcId=masterctx.InstanceId });
            log.LogInformation(Constants.LOG_MSG_PROCESS, masterctx.InstanceId, nameof(O_MasterOrchestrator), "Rules Loaded");

         
            var protocolDictionarySet = await masterctx.CallActivityAsync<List<ProtocolDictionary>>("A_PRDictionaryLoader"
             , new PRDLoadRequest { ProtocolId = request.ProtocolId, MasterOrcId = masterctx.InstanceId });
            log.LogInformation(Constants.LOG_MSG_PROCESS, masterctx.InstanceId, nameof(O_MasterOrchestrator), "Protocol Dictionary Loaded");

           
            var kpis = rules.Where(r=>r.RULE_TYPE != null).GroupBy(r => r.RULE_TYPE).Select(g => g.Key).ToList<string>(); 
  
           
            var protocolOrchestrationTasks = new List<Task<List<ProtocolRuleResult>>>();
            foreach (string kpi in kpis)
            {

                 var kpiRuleList = rules.Where(r => r.RULE_TYPE == kpi).ToList(); 
                var task = masterctx.CallSubOrchestratorAsync<List<ProtocolRuleResult>>("O_ProtocolRuleOrchestrator",
                    new PROrchestrationRequest
                    {
                        FileRef = request.FileRef,
                        TenantRef = request.Tenant,
                        ProtocolId = request.ProtocolId,
                        RuleDetailList = kpiRuleList,
                        ProtocolDictionarySet= protocolDictionarySet,
                        MasterOrcId = masterctx.InstanceId
                    });
                protocolOrchestrationTasks.Add(task);
            }

            var ruleResults = await Task.WhenAll(protocolOrchestrationTasks);
            log.LogInformation(Constants.LOG_MSG_PROCESS, masterctx.InstanceId, nameof(O_MasterOrchestrator), "Results Generated");


           
            var results=ruleResults.SelectMany(x => x);


            if (results != null && results.Count() > 0)
            {
                await masterctx.CallActivityAsync<bool>("A_DataStore", new PersistDataRequest
                {
                    MasterOrcId=masterctx.InstanceId,
                    RequestQueueId = request.RequestQueueId,
                    RuleExecutionResults = results
                });

                log.LogInformation(Constants.LOG_MSG_PROCESS, masterctx.InstanceId, nameof(O_MasterOrchestrator), $"Result: {results.Count()} records commited");
            }

          
            bool complete = await masterctx.CallActivityAsync<bool>("A_ProgressReporter"
                 , new RuleEngineStatusRequest { MasterOrcId = masterctx.InstanceId, Status = "BRERAN", OrcReq = request });
            log.LogInformation(Constants.LOG_MSG_PROCESS, masterctx.InstanceId, nameof(O_MasterOrchestrator),  "Marked Completion");

            return masterctx.InstanceId;

        }


    }
}
