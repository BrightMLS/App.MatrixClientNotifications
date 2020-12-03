using System;

namespace BrightMls.Enterprise.MatrixClientNotifications.Models
{
    public class ListingNote
    {
        public DateTime NoteDateTime { get; set; }
        public string NotedBy { get; set; }
        public string NoteText { get; set; }
    }
}
