using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using org.qubic.proposal.api.Qubic;
using li.qubic.lib;
using System.Text;
using li.qubic.lib.Helper;

namespace org.qubic.proposal.api.Model
{

    public class ProposalResultModel
    {
      
        // todo: add result tick (for which tick is this result valid)

        public static ProposalResultModel From(ProposalSummarizedVotingDataV1 resultSummary)
        {
            var result = new ProposalResultModel()
            {
                ProposalIndex = resultSummary.proposalIndex,
                OptionCount = resultSummary.optionCount,
                ProposalTick = resultSummary.proposalTick,
                AuthorizedVoters = resultSummary.authorizedVoters,
                TotalVotes = resultSummary.totalVotes,
                OptionVoteCount = resultSummary.optionVoteCount,
            };
            return result;
        }


        public ushort ProposalIndex { get; set; }
        public ushort OptionCount { get; set; }

        public uint ProposalTick { get; set; }

        public uint AuthorizedVoters { get; set; }

        public uint TotalVotes { get; set; }

        public uint[] OptionVoteCount { get; set; }

    }

}
