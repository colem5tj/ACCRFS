using System;

namespace ACC_Demo.Pages.Member
{
    public class HelpRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PrimarySkill { get; set; }
        public int EstimatedHours { get; set; }
        public DateTime DesiredStartDate { get; set; }
        public TimeSpan TimeSlot { get; set; }
        public bool IsUrgent { get; set; }
        public string RequestedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}