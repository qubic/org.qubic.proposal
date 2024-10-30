using li.qubic.lib.Helper;
using org.qubic.proposal.api.Qubic;

namespace org.qubic.proposal.api.test
{
    [TestClass]
    public class TestDeserialization
    {
        [TestMethod]
        public void TestVoteResultDeserialization()
        {
            var data = Convert.FromHexString("0100000000000000000002000B6C0001A4020000040000000000000004000000000000000000000000000000000000000000000000000000");

            var votingResult = Marshalling.Deserialize<GetVotingResults_output>(data);

        }
    }
}