using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BrightMls.Enterprise.MatrixClientNotifications.Models
{
    public class PortalListing
    {
        public int ContactKeyNumeric { get; set; }
        public int ListingKeyNumeric { get; set; }
        public string ResourceName { get; set; }
        public string ClassName { get; set; }
        public DateTime? ListingSentTimestamp { get; set; }
        public DateTime? PortalLastVisitedTimestamp { get; set; }
        public string ContactListingPreference { get; set; }
        public DateTime? ModificationTimestamp { get; set; }

        [JsonProperty("DirectEmailYN")]
        public bool DirectEmailYn { get; set; }
        public string LastContactNoteTimestamp { get; set; }
        public string LastAgentNoteTimestamp { get; set; }
        [JsonProperty("ListingViewedYN")]
        public bool ListingViewedYn { get; set; }
        [JsonProperty("AgentNotesUnreadYN")]
        public bool AgentNotesUnreadYn { get; set; }
        [JsonProperty("ContactNotesUnreadYN")]
        public bool ContactNotesUnreadYn { get; set; }
        public List<ListingNote> ListingNotes { get; set; }
    }
}
