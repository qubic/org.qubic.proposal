using li.qubic.lib.Helper;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace org.qubic.proposal.api.Qubic
{
    // translated by chat gpt
    // modified and optimized by joetom

    /// <summary>
    /// mapped Qubic constants
    /// </summary>
    public static class ProposalConstants
    {
        public const ushort INVALID_PROPOSAL_INDEX = 0xffff;
        public const uint INVALID_VOTER_INDEX = 0xffffffff;
        public const long NO_VOTE_VALUE = unchecked((long)0x8000000000000000);
    }

    // Single vote for all types of proposals defined in August 2024.
    // Input data for contract procedure call
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProposalSingleVoteDataV1
    {
        public ushort proposalIndex;
        public ushort proposalType;
        public uint proposalTick;
        public long voteValue;

        public ProposalSingleVoteDataV1() { }

        public ProposalSingleVoteDataV1(ushort proposalIndex, ushort proposalType, uint proposalTick, long voteValue)
        {
            this.proposalIndex = proposalIndex;
            this.proposalType = proposalType;
            this.proposalTick = proposalTick;
            this.voteValue = voteValue;
        }
    }

    // Voting result summary for all types of proposals defined in August 2024.
    // Output data for contract function call for getting voting results.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProposalSummarizedVotingDataV1
    {
        public ushort proposalIndex; // 2
        public ushort optionCount; // 2
        public uint proposalTick; // 4
        public uint authorizedVoters; // 4
        public uint totalVotes; // 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] // 32
        public byte[] unionData;

       
        public uint[] optionVoteCount { get {
                uint[] uintArray = new uint[8];
                for (int i = 0; i < 8; i++)
                {
                    uintArray[i] = BitConverter.ToUInt32(unionData, i * 4);
                }
                return uintArray;
            } set
            {
                if (value.Length != 8)
                    throw new Exception("length of optionVoteCount must be 8");
                unionData = new byte[32];
                for (int i = 0; i < 8; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(value[i]);
                    Array.Copy(bytes, 0, unionData, i * 4, 4);
                }
            }
        }

        public long scalarVotingResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ProposalSummarizedVotingDataV1() {
            this.optionVoteCount = new uint[8];
        }

        public ProposalSummarizedVotingDataV1(ushort proposalIndex, ushort optionCount, uint proposalTick, uint authorizedVoters, uint totalVotes)
        {
            this.proposalIndex = proposalIndex;
            this.optionCount = optionCount;
            this.proposalTick = proposalTick;
            this.authorizedVoters = authorizedVoters;
            this.totalVotes = totalVotes;
            this.optionVoteCount = new uint[8];
            this.scalarVotingResult = 0;
        }
    }

    // Proposal type constants and functions.
    public static class ProposalTypes
    {
        public enum ClassTypes
        {
            GeneralOptions = 0,
            Transfer = 256,
            Variable = 512
        }

        public static class Class
        {
            public const ushort GeneralOptions = 0;
            public const ushort Transfer = 0x100;
            public const ushort Variable = 0x200;
        }

        public const ushort YesNo = Class.GeneralOptions | 2;
        public const ushort ThreeOptions = Class.GeneralOptions | 3;
        public const ushort FourOptions = Class.GeneralOptions | 4;

        public const ushort TransferYesNo = Class.Transfer | 2;
        public const ushort TransferTwoAmounts = Class.Transfer | 3;
        public const ushort TransferThreeAmounts = Class.Transfer | 4;
        public const ushort TransferFourAmounts = Class.Transfer | 5;

        public const ushort VariableYesNo = Class.Variable | 2;
        public const ushort VariableTwoValues = Class.Variable | 3;
        public const ushort VariableThreeValues = Class.Variable | 4;
        public const ushort VariableFourValues = Class.Variable | 5;
        public const ushort VariableScalarMean = Class.Variable | 0;

        public static ushort Type(ushort cls, ushort options) => (ushort)(cls | options);
        public static ushort OptionCount(ushort proposalType) => (ushort)(proposalType & 0x00ff);
        public static ushort ClassOfProposalType(ushort proposalType) => (ushort)(proposalType & 0xff00);
    }

    // Proposal data struct for all types of proposals defined in August 2024.
    // You have to choose whether to support scalar votes next to option votes.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProposalDataV1
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] url;
        public ushort epoch;
        public ushort type;
        public uint tick;


        /*
        // Proposal payload data(for all except types with class GeneralProposal)
		union
		{
			// Used if type class is Transfer
			struct Transfer
        {
            id destination;
            array<sint64, 4> amounts;   // N first amounts are the proposed options (non-negative, sorted without duplicates), rest zero
        }
        transfer;

			// Used if type class is Variable and type is not VariableScalarMean
			struct VariableOptions
        {
            uint64 variable;            // For identifying variable (interpreted by contract only)
            array<sint64, 4> values;    // N first amounts are proposed options sorted without duplicates, rest zero
        }
        variableOptions;

			// Used if type is VariableScalarMean
			struct VariableScalar
        {
            uint64 variable;            // For identifying variable (interpreted by contract only)
            sint64 minValue;            // Minimum value allowed in proposedValue and votes, must be > NO_VOTE_VALUE
            sint64 maxValue;            // Maximum value allowed in proposedValue and votes, must be >= minValue
            sint64 proposedValue;       // Needs to be in range between minValue and maxValue

            static constexpr sint64 minSupportedValue = 0x8000000000000001;
				static constexpr sint64 maxSupportedValue = 0x7fffffffffffffff;
			}
        variableScalar;
		};
         */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] unionData;

        public TransferData transfer { get; set; }
        public VariableOptionsData variableOptions { get; set; }
        public VariableScalarData variableScalar { get; set; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TransferData
        {
            public Guid destination;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public long[] amounts;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VariableOptionsData
        {
            public ulong variable;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public long[] values;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VariableScalarData
        {
            public ulong variable;
            public long minValue;
            public long maxValue;
            public long proposedValue;

            public const long minSupportedValue = unchecked((long)0x8000000000000001);
            public const long maxSupportedValue = 0x7fffffffffffffff;
        }

        public bool CheckValidity()
        {
            bool isValid = false;
            ushort cls = ProposalTypes.ClassOfProposalType(type);
            ushort options = ProposalTypes.OptionCount(type);

            if (cls == ProposalTypes.Class.GeneralOptions)
            {
                isValid = options >= 2 && options <= 8;
            }
            else if (cls == ProposalTypes.Class.Transfer)
            {
                isValid = options >= 2 && options <= 5;
                for (int i = 0; i < options - 1; i++)
                {
                    if (transfer.amounts[i] < 0)
                    {
                        isValid = false;
                        break;
                    }
                }
            }
            else if (cls == ProposalTypes.Class.Variable)
            {
                if (options >= 2 && options <= 5)
                {
                    isValid = true; // Placeholder logic for checking sorted values
                }
                else if (options == 0)
                {
                    isValid = variableScalar.minValue <= variableScalar.proposedValue &&
                              variableScalar.proposedValue <= variableScalar.maxValue &&
                              variableScalar.minValue > ProposalConstants.NO_VOTE_VALUE;
                }
            }

            return isValid;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransferData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] destination;
        public long amount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VariableOptionsData
    {
        public ulong variable;
        public long value;
    }

    // Proposal data struct for 2-option proposals (requires less storage space).
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProposalDataYesNo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] url;
        public ushort epoch;
        public ushort type;
        public uint tick;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] unionData;

        public TransferData transfer { get => Marshalling.Deserialize<TransferData>(unionData); set => unionData = Marshalling.Serialize(value); }
        public VariableOptionsData variableOptions { get => Marshalling.Deserialize<VariableOptionsData>(unionData); set => unionData = Marshalling.Serialize(value); }

        

        public bool CheckValidity()
        {
            bool isValid = false;
            ushort cls = ProposalTypes.ClassOfProposalType(type);
            ushort options = ProposalTypes.OptionCount(type);

            if (cls == ProposalTypes.Class.GeneralOptions)
            {
                isValid = options >= 2 && options <= 3;
            }
            else if (cls == ProposalTypes.Class.Transfer)
            {
                isValid = options == 2 && transfer.destination.Length == 32 && transfer.amount >= 0;
            }
            else if (cls == ProposalTypes.Class.Variable)
            {
                isValid = options == 2;
            }

            return isValid;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetProposalIndices_input
    {
        [MarshalAs(UnmanagedType.I1)] // bool is typically marshaled as 1 byte in unmanaged code
        public bool activeProposals;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] _padding;

        public int prevProposalIndex;
        
        public GetProposalIndices_input(bool activeProposals, int offset = -1)
        {
            this.activeProposals = activeProposals;
            this.prevProposalIndex = offset; // Initialize to -1 as per the logic
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetProposalIndices_output
    {
        public ushort numOfIndices;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public ushort[] indices;


        public GetProposalIndices_output() { }
        public GetProposalIndices_output(ushort numOfIndices)
        {
            this.numOfIndices = numOfIndices;
            this.indices = new ushort[64]; // Initialize the indices array
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetProposal_input
    {
        public ushort proposalIndex;

        public GetProposal_input(ushort proposalIndex)
        {
            this.proposalIndex = proposalIndex;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetProposal_output_YesNo
    {
        [MarshalAs(UnmanagedType.I1)] // Assuming 'bit' is a boolean flag (1 byte)
        public bool okay;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] // Padding to ensure alignment
        public byte[] _padding0;

        // Assuming id is a fixed-length byte array (e.g., public key)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] // Size of the public key can vary, adjust accordingly
        public byte[] proposerPublicKey;

        public ProposalDataYesNo proposal;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetProposal_output_ProposalDataV1
    {
        [MarshalAs(UnmanagedType.I1)] // Assuming 'bit' is a boolean flag (1 byte)
        public bool okay;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] // Padding to ensure alignment
        public byte[] _padding0;

        // Assuming id is a fixed-length byte array (e.g., public key)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] // Size of the public key can vary, adjust accordingly
        public byte[] proposerPublicKey;

        public ProposalDataV1 proposal;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetVotingResults_input
    {
        public ushort proposalIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GetVotingResults_output
    {
        [MarshalAs(UnmanagedType.I1)] // Assuming 'bit' is a boolean flag (1 byte)
        public bool okay;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)] // Size of the public key can vary, adjust accordingly
        private byte[] _padding;
        public ProposalSummarizedVotingDataV1 results;
    }


}
