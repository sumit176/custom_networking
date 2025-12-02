namespace CustomNetworking.Core
{
    public class UnreliableChannel
    {
        private uint localSequence;

        public UnreliableChannel()
        {
            localSequence = 0;
        }

        public uint GetNextSequence()
        {
            return localSequence++;
        }

        /// <summary>
        /// Process received unreliable packet
        /// No ACK or retransmission needed
        /// </summary>
        public bool ProcessReceivedPacket(uint sequence)
        {
            // For unreliable packets, we just accept them
            // Could add duplicate detection here if needed
            return true;
        }
    }
}

