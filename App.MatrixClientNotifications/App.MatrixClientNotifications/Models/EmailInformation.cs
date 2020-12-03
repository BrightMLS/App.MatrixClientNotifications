using System.Collections.Generic;

namespace BrightMls.Enterprise.MatrixClientNotifications.Models
{
    public class EmailInformation
    {
        public int ContactId { get; set; }
        public string ContactFirstName { get; set; }
        public string ContactEmail { get; set; }
        public string AgentMemberMlsId { get; set; }
        public Agent Agent { get; set; }
        public List<MlsInformation> MlsInformation { get; set; }
    }
}
