using li.qubic.lib.Helper;
using li.qubic.lib.Network;
using org.qubic.common.caching;
using org.qubic.proposal.api.Cache;
using org.qubic.proposal.api.Helper;
using org.qubic.proposal.api.Model;
using org.qubic.proposal.api.Qubic;
using org.qubic.rpc.client;
using org.qubic.rpc.client.Model;
using System.Globalization;
using System.Text.Json;

namespace org.qubic.proposal.api.HostedServices
{
    public class NetworkSyncService : IHostedService, IDisposable
    {
        private readonly ILogger<NetworkSyncService> _logger;
        private Timer? _timer = null;
        private bool _isRunning = false;
        private RedisService _redisService;
        private AppSettings _settings;

        public NetworkSyncService(ILogger<NetworkSyncService> logger,
            RedisService redisService,
            AppSettings settings
            )
        {
            _settings = settings;
            _logger = logger;
            _redisService = redisService;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NetworkSyncService Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(_settings.MinNetworkSyncInterval));

            return Task.CompletedTask;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractIndex">6 = GeneralComputorProposal; 8 = CCF</param>
        private QuerySmartContractResponse? GetActiveProposals(short contractIndex, int offset = -1)
        {
            var client = new QubicRpcClient(_settings.RpcBaseUrl);

            var input = new GetProposalIndices_input(true, offset);

            var binaryInput = Marshalling.Serialize(input);

            var activeProposalsRequest = new QuerySmartContractRequest(contractIndex, 1, binaryInput);

            return client.QuerySmartContract(activeProposalsRequest).GetAwaiter().GetResult();
        }

        /// <summary>
        /// get one proposal
        /// </summary>
        /// <param name="contractIndex"></param>
        /// <param name="proposalIndex"></param>
        /// <returns></returns>
        private QuerySmartContractResponse? GetProposal(short contractIndex, ushort proposalIndex)
        {
            var client = new QubicRpcClient(_settings.RpcBaseUrl);
            var input = new GetProposal_input(proposalIndex);

            var binaryInput = Marshalling.Serialize(input);
            var activeProposalsRequest = new QuerySmartContractRequest(contractIndex, 2, binaryInput);

            return client.QuerySmartContract(activeProposalsRequest).GetAwaiter().GetResult();
        }


        private List<ushort> UpdateGeneralQuorumProposals()
        {
            var activeProposalsRaw = GetActiveProposals(ContractHelper.QuorumProposalContractIndex);

            // todo: save Quorum proposals
            return new List<ushort>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>list of active indices</returns>
        private List<ushort> UpdateCcfProposals()
        {
            // todo: manage when > 64 indices
            var activeProposalsRaw = GetActiveProposals(ContractHelper.CcfContractIndex);
            var o = Marshalling.Deserialize<GetProposalIndices_output>(activeProposalsRaw.ResponseData);

            var activeIndices = new List<ushort>();

            for(ushort i = 0; i < o.numOfIndices; i++)
            {
                activeIndices.Add(o.indices[i]);
                var proposalRaw = GetProposal(ContractHelper.CcfContractIndex, o.indices[i]);
                var parsedProposal = Marshalling.Deserialize<GetProposal_output_YesNo>(proposalRaw.ResponseData);

                currentEpoch = parsedProposal.proposal.epoch;

                // store the proposal in cache
                var storeKey = ProposalCacheKey.CcfEpochProposal(currentEpoch, i);

                var cachedProposal = ProposalModel.From(ContractHelper.CcfContractIndex, i, parsedProposal);

                _redisService.Add(storeKey, cachedProposal, ProposalCacheKey.CcfEpochIndex(cachedProposal.Epoch));

            }

            return activeIndices;

            // todo: remove cleared propopsals

        }

        private void UpdateCcfResults(List<ushort> indices)
        {
            var client = new QubicRpcClient(_settings.RpcBaseUrl);

            foreach(ushort i in indices)
            {
                var input = new GetVotingResults_input()
                {
                    proposalIndex = i
                };

                var binaryInput = Marshalling.Serialize(input);

                var proposalResultRequest = new QuerySmartContractRequest(ContractHelper.CcfContractIndex, 4, binaryInput);

                var result = client.QuerySmartContract(proposalResultRequest).GetAwaiter().GetResult();

                if(result?.ResponseData != null)
                {
                    if (result.ResponseData[0] == 1) { 
                        var votingResultSummary = Marshalling.Deserialize<GetVotingResults_output>(result.ResponseData);
                        var resultModel = ProposalResultModel.From(votingResultSummary.results);


                        var storeKey = ProposalCacheKey.CcfEpochProposal(currentEpoch, i);

                        // todo: error handling
                        var existingEntry = _redisService.Get<ProposalModel>(storeKey).GetAwaiter().GetResult();

                        if(existingEntry != null)
                        {
                            existingEntry.Result = resultModel;
                            _redisService.AddOrUpdate(storeKey, existingEntry).GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        // todo: error handling: node returned NOK
                    }
                }
            }

        }

        // todo: load current epoch from rpc
        private int currentEpoch = 0;

        private void DoWork(object? state)
        {

            if (_isRunning)
                return;
            try
            {
                _isRunning = true;

                // getting lock cache key
                var lockKey = ProposalCacheKey.LockNetworkSync;

                if (RedisLockHelper.TryAcquireLockAsync(_redisService.Database, lockKey,
                // set expire to 5 seconds to avoid longer locks
                TimeSpan.FromSeconds(5)).GetAwaiter().GetResult())
                {
                    // lock aquired do the work
                    try
                    {
                        // todo: think about passive locking

                        var activeQuorumIndices = UpdateGeneralQuorumProposals();
                        var activeCcfIndices = UpdateCcfProposals();

                        UpdateCcfResults(activeCcfIndices);
                        
                        // just a test
                        _redisService.Database.StringSet("test", DateTime.UtcNow.ToString());
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e, "Error in Network Sync");
                    }
                    finally
                    {
                        // always release lock
                        _ = RedisLockHelper.ReleaseLockAsync(_redisService.Database, lockKey);
                    }
                }
                else
                {
                    return;
                }

                _logger.LogInformation(
                    "NetworkSyncService done");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
            }
            finally
            {
                _isRunning = false;
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NetworkSyncService Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
