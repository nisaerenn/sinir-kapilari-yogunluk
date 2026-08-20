namespace SinirKapisiYogunluk.Models;

// POST /api/border-gates/{gateId}/observations isteğinin gövdesi (body) buna karşılık gelir.
public class CreateObservationRequest
{
    public int WaitingVehicleCount { get; set; }
}
