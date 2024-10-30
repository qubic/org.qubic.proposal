using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace org.qubic.rpc.client.Model
{
    public class QuerySmartContractRequest
    {
        public QuerySmartContractRequest(short contractIndex, short inputType, byte[]? input)
        {
            if(input == null)
            {
                Input = new byte[0];
            }else
            {
                Input = input;
            }
            ContractIndex = contractIndex;
            InputType = inputType;
            InputSize = Input.Length;
        }

        public short ContractIndex { get; set; }
        public short InputType { get; set; }
        public int InputSize { get; set; }

        [JsonPropertyName("requestData")]
        public byte[] Input { get; set; }
    }
}
