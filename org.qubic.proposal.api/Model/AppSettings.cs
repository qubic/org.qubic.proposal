using org.qubic.common.caching;

namespace org.qubic.proposal.api.Model
{
    public class AppSettings
    {
        public string MongoConnectionstring { get; set; }
        public string MongoDb { get; set; }

        public RedisConfigurationDefaultImpl Redis { get; set; }

        public double MinNetworkSyncInterval { get; set; } = 30; // default sync every 30 second
        public string RpcBaseUrl { get; set; } = "https://rpc.qubic.org";
    }
}
