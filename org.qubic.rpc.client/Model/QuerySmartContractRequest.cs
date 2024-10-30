using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace org.qubic.rpc.client.Model
{
    public class QuerySmartContractResponse
    {

        [JsonPropertyName("responseData")]
        public byte[] ResponseData { get; set; }
    }
}
