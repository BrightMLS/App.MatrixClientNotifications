using System.Collections.Generic;

namespace BrightMls.Enterprise.MatrixClientNotifications.Models
{
    public class MlsInformation
    {
        public int ListingKeyNumeric { get; set; }
        public int MlsNumber { get; set; }
        public string Address { get; set; }
        public IList<ListingNote> Notes { get; set; }
    }
}
