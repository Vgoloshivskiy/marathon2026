namespace APIHandler.Models.GuestSessions;

/// <summary>Absolute new quantity for a dish line. Nullable so a missing field is rejected instead of silently removing the line.</summary>
public class SetGroupSessionItemQuantityRequest
{
    public int? Quantity { get; set; }
}
