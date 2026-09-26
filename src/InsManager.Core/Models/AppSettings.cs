namespace InsManager.Core.Models;

public sealed record AppSettings(
    string SimBriefPilotId = "",
    string Theme = "Dark",
    string Aircraft = "FSS Boeing 727");
