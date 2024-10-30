using org.qubic.rpc.client.Model;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace org.qubic.rpc.client
{
    public class QubicRpcClient
    {
        #region Api Endpoints
        private static string _ep_querySmartContract = "/v1/querySmartContract";

        #endregion

        private string _baseUrl = string.Empty;

        public QubicRpcClient(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException("baseUrl must not be null or empty");
            }

            _baseUrl = baseUrl;
        }


        private string GetApiUrl(string ep)
        {
            return _baseUrl + ep;
        }

        /// <summary>
        /// call a SC function
        /// </summary>
        /// <returns></returns>
        public async Task<QuerySmartContractResponse?> QuerySmartContract(QuerySmartContractRequest request)
        {
            using (var httpClient = new HttpClient())
            {
                var body = JsonContent.Create(request, typeof(QuerySmartContractRequest), new MediaTypeHeaderValue("application/json"));

                var responseMessage = await httpClient.PostAsync(GetApiUrl(_ep_querySmartContract), body);
                if (responseMessage == null)
                {
                    return null;
                }
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if(responseMessage.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var badRequestBody = await responseMessage.Content.ReadAsStringAsync();
                        throw new Exception("Bad Request: " + badRequestBody);
                    }

                    throw new Exception("Received non OK status code: " + responseMessage.StatusCode);
                }

                var responseBody = await responseMessage.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseBody))
                {
                    throw new Exception("Received empty response body");
                }

                try
                {
                    var response = JsonSerializer.Deserialize<QuerySmartContractResponse>(responseBody);
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception("Deserialization failed (" + responseBody + ")", ex);
                }

            }
        }

    }
}
