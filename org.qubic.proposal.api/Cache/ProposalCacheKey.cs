namespace org.qubic.proposal.api.Cache
{
    public static class ProposalCacheKey
    {
        public const string LockNetworkSync = "Lock:NetworkSync";

        //public const string ConfigTrustedPeers = "Config:TrustedPeers";

        public static string CcfEpochIndex(int epoch) => $"IDX:{epoch}:Ccf";
        public static string CcfEpochProposal(int epoch, ushort index) => $"CCF:{epoch}:Proposal:{index}";

    }
}
