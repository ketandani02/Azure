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


    public static class O_ProtocolRuleOrchestrator
    {
        [FunctionName("O_ProtocolRuleOrchestrator")]
        public static async Task<IEnumerable<ProtocolRuleResult>> RunPROrchestrationAsync(
        [OrchestrationTrigger]  DurableOrchestrationContext protocolrulectx , ILogger log)
        {
             var request= protocolrulectx.GetInput<PROrchestrationRequest>();

            string instanceId = $"{request.MasterOrcId}_{protocolrulectx.InstanceId}";

            var rules = request.RuleDetailList;

            var prdSet = request.ProtocolDictionarySet;

            
            var  prepRules = await protocolrulectx.CallActivityAsync<IEnumerable<Rule>>("A_RulePrep" , new RulePrepRequest { Rules=rules,PRDSet=prdSet, InstanceId=instanceId });
            log.LogInformation(Constants.LOG_MSG_PROCESS, instanceId, nameof(O_ProtocolRuleOrchestrator), "Rule Prepared");

           
            var protocolRuleRoute = await protocolrulectx.CallActivityAsync<string>("A_ProtocolRouter",new RouteRequest { InstanceId = instanceId, ProtocolId = request.ProtocolId });
            log.LogInformation(Constants.LOG_MSG_PROCESS, instanceId, nameof(O_ProtocolRuleOrchestrator), "Finalised rule execution route");

           
            var protocolRuleResults = await protocolrulectx.CallActivityAsync<IEnumerable<ProtocolRuleResult>>("A_RuleExecuter",
                new RuleExecRequest {
                    InstanceId=instanceId,
                    FileRef =request.FileRef,
                    TenantRef=request.TenantRef,
                    ProtocolId=request.ProtocolId,
                    Rules = prepRules.ToList(),
                    ProtocolRoute = protocolRuleRoute
                   
                });
            log.LogInformation(Constants.LOG_MSG_PROCESS, instanceId, nameof(O_ProtocolRuleOrchestrator), "Applied rules & results recieved");

           
            return protocolRuleResults;
        }

    }
}
