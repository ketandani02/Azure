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
using Newtonsoft.Json;
using EY.COPA.BusinessRuleEngine.Common;

namespace EY.COPA.BusinessRuleEngine.Activities
{

    public static class A_RulePrep
    {
        [FunctionName("A_RulePrep")]
        public static async Task<IEnumerable<Rule>> RulePrepAsync(
            [ActivityTrigger] RulePrepRequest request,
            ILogger log
            )
        {
            if (request == null || request.Rules == null || request.Rules.Count() <= 0 || request.PRDSet == null || request.PRDSet.Count() <= 0)
            {
                log.LogCritical(Constants.LOG_MSG_VALIDATION, request.InstanceId, nameof(A_RulePrep), "Invalid Request");
                throw new Exception("Invalid Request");
            }

            var returnValue = new List<Rule>();

            foreach (var r in request.Rules)
                try
                {
                   returnValue.Add(r.ToRule(request.PRDSet));
                }
                catch (Exception ex)
                {
                    log.LogCritical(Constants.LOG_MSG_EXCEPTION, request.InstanceId, nameof(A_RulePrep), $"Protocol Dictionary Rule ID:{r.PDRR_ID}, Info:{ex.ToString()}");
                    throw ex;
                }
            return returnValue;
        }
    }
}
