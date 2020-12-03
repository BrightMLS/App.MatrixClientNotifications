using System.Collections.Generic;

namespace BrightMls.Enterprise.MatrixClientNotifications.Models
{
    public class Contact
    {
        public int ContactId { get; set; }
        public string ContactFirstName { get; set; }
        public string ContactLastName { get; set; }
        public string ContactEmailAddresseses { get; set; }
        public IList<ContactPhone> ContactPhoneNumbers { get; set; }
        public IList<MlsInformation> MlsInformation { get; set; }

    }
}
