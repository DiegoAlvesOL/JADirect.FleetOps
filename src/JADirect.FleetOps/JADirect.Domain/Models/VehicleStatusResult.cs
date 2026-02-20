namespace JADirect.Domain.Models;


/// <summary>
/// Representa o estado de conformidade processado pela Fleet Intelligence.
/// </summary>
public class VehicleStatusResult
{
    public DateTime? LastWalkaroundDate { get; set; }
    public int  DaysSinceLastCheck  { get; set; }
    public string StatusColor { get; set; }
    public string Message {  get; set; }
    public bool IsActionRequired { get; set; }
}