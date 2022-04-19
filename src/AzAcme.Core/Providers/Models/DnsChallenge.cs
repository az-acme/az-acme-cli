namespace AzAcme.Core.Providers.Models
{
    public class DnsChallenge
    {
        public enum DnsChallengeStatus { Pending, Validated, Failed }

        public DnsChallenge(string identifier, string txtValue)
        {
            Identitifer = identifier;
            TxtValue = txtValue;
        }

        public void SetRecordName(string record)
        {
            TxtRecord = record;
        }

        public void SetStatus(DnsChallengeStatus status)
        {
            Status = status;
        }

        public DnsChallengeStatus Status { get; private set; } = DnsChallengeStatus.Pending;

        public string Identitifer { get; }
        public string TxtValue { get; }

        public string? TxtRecord { get; private set; }
    }
}
