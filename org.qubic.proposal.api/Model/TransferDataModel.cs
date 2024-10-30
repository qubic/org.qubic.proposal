using li.qubic.lib;
using org.qubic.proposal.api.Qubic;

namespace org.qubic.proposal.api.Model
{
    public class TransferDataModel
    {
        public static TransferDataModel From(TransferData data)
        {
            return new TransferDataModel()
            {
                Amount = data.amount,
                DestinationId = new QubicHelper().GetIdentity(data.destination)
            };
    }

        public long Amount { get; set; }
        public string DestinationId { get; set; }
    }
}
