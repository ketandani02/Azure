using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using EY.COPA.BusinessRuleEngine.Utility;

namespace EY.COPA.BusinessRuleEngine.DataModels
{
    public static class Mappers
    {
        public static Rule ToRule(this RuleDetails ruleDetails, IEnumerable<ProtocolDictionary> pdSet)
        {
            dynamic jsonRule = JsonConvert.DeserializeObject(ruleDetails.RULE_CODE);
        
          
            var codeParams = new List<string>();
            string lambda = string.Empty;
            //if codeparamsspec exists
            
            if (((Newtonsoft.Json.Linq.JObject)jsonRule).ContainsKey("codeparamsspec"))
            {
                // Get the properties of 'Protocol Dictionary' class object.
                PropertyInfo[] pdPropertyInfo = Type.GetType("EY.COPA.BusinessRuleEngine.DataModels.ProtocolDictionary").GetProperties();
                foreach (var spec in jsonRule.codeparamsspec)
                {
                    var pDic = pdSet.FirstOrDefault(r => r.PDBIZNM_REF_CODE == spec.fname.Value);
                    if (spec.cname.Value == "PRD_DF_NM")
                        codeParams.Add($"UNVFIELD[\"{pdPropertyInfo.FirstOrDefault(p => p.Name == spec.cname.Value).GetValue(pDic).ConvertToString()}\"]");
                    else
                        codeParams.Add($"{pdPropertyInfo.FirstOrDefault(p => p.Name == spec.cname.Value).GetValue(pDic).ConvertToString()}");

                }
                lambda = string.Format(jsonRule.code.Value, codeParams.ToArray());

            }
            else //code params
            {
                // Get the properties of 'RuleDetails' class object.
                PropertyInfo[] ruleDetailsPropertyInfo = Type.GetType("EY.COPA.BusinessRuleEngine.DataModels.RuleDetails").GetProperties();
                foreach (var col in jsonRule.codeparams)
                {
                    if (col.Value == "PRD_DF_NM")
                        codeParams.Add($"UNVFIELD[\"{ruleDetailsPropertyInfo.FirstOrDefault(p => p.Name == col.Value).GetValue(ruleDetails).ConvertToString()}\"]");
                    else
                        codeParams.Add($"{ruleDetailsPropertyInfo.FirstOrDefault(p => p.Name == col.Value).GetValue(ruleDetails).ConvertToString()}");
                }
                    lambda = string.Format(jsonRule.code.Value, codeParams.ToArray());
            }
            

            return new Rule {
                GRR_ID = ruleDetails.GRR_ID,
                PROTOCOL_RD_ID = ruleDetails.PROTOCOL_RD_ID,
                PROTOCOL_REF_ID = ruleDetails.PROTOCOL_REF_ID,
                RULE_MSG = ruleDetails.RULE_MSG,
                RULE_PRIORITY = ruleDetails.RULE_PRIORITY,
                RULE_SORT_ORDER = ruleDetails.RULE_SORT_ORDER,
                PDRCR_ID=ruleDetails.PDRCR_ID,
                PDRR_ID=ruleDetails.PDRR_ID,
                RULE_ACTIVE_FLAG=ruleDetails.RULE_ACTIVE_FLAG,
                RULE_LAMBDA = lambda,
                RULE_LAMBDA_ARGS = jsonRule.expargs.Value,
                PRD_DF_NM = ruleDetails.PRD_DF_NM
            };
        }
    }
}
