using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using org.qubic.proposal.api.Qubic;
using li.qubic.lib;
using System.Text;
using li.qubic.lib.Helper;

namespace org.qubic.proposal.api.Model
{

    public class ProposalModel
    {
        private static int GetUrlLength(byte[] byteArray)
        {
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] == 0)
                {
                    return i;
                }
            }

            return byteArray.Length;
        }

        public static ProposalModel From(short contractIndex, int index, GetProposal_output_YesNo output)
        {
            var helper = new QubicHelper();
            var proposal = new ProposalModel()
            {
                ContractIndex = contractIndex,
                Epoch = output.proposal.epoch,
                Index = index,
                ProposerId = helper.GetIdentity(output.proposerPublicKey),
                Tick = output.proposal.tick,
                Url = Encoding.ASCII.GetString(output.proposal.url, 0, GetUrlLength(output.proposal.url)),
                NumberOfOptions = ProposalTypes.OptionCount(output.proposal.type),
                Type = (ProposalTypes.ClassTypes)ProposalTypes.ClassOfProposalType(output.proposal.type),
            };
            switch (proposal.Type)
            {
                case ProposalTypes.ClassTypes.GeneralOptions:
                    break;
                case ProposalTypes.ClassTypes.Transfer:
                    proposal.TransferData = TransferDataModel.From(output.proposal.transfer);
                    break;
                case ProposalTypes.ClassTypes.Variable:
                    proposal.VariableOptions = output.proposal.variableOptions;
                    break;
            }
            return proposal;
        }

        public short ContractIndex { get; set; }

        public ProposalTypes.ClassTypes Type { get; set; }

        public int Index { get; set; }

        public string ProposerId { get; set; }

        public int Epoch { get; set; }

        public uint Tick { get; set; }

        public string Url { get; set; }

        public int NumberOfOptions { get; set; }

        /// <summary>
        /// this is filled if the proposal is a transfer proposal
        /// </summary>
        public TransferDataModel? TransferData { get; set; }
        /// <summary>
        /// this is filled if it is a variable options proposal
        /// </summary>
        public VariableOptionsData? VariableOptions { get; set; }

        public ProposalResultModel Result { get; set; }

    }

}
