using System;
using System.Collections.Generic;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; }
    public int CategoryId { get; set; }
    public string Format { get; set; }
    public int MaxParticipants { get; set; }
    public string TargetAudience { get; set; }
    public string Status { get; set; }
    public List<Session> Sessions { get; set; } = new List<Session>();
}