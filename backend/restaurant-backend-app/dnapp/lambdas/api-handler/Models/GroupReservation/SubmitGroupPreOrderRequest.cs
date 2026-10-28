using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GroupReservation;

public class SubmitGroupPreOrderRequest
{
    [Required(AllowEmptyStrings = false)]
    [JsonPropertyName("reservationId")]
    public string ReservationId { get; set; } = string.Empty;
}
