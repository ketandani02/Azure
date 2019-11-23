using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EY.COPA.BusinessRuleEngine.DataContracts
{
    [DataContract]
    public class RouteRequest
    {

        [DataMember]
        public string InstanceId { get; set; }

        [DataMember]
        public int ProtocolId { get; set; }
    }
}
