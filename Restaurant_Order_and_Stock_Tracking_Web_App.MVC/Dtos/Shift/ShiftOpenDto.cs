namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Shift;

public class ShiftOpenDto
{

    public decimal OpeningBalance { get; set; }
    public string? Notes { get; set; }
    public decimal DifferenceThreshold { get; set; } = 100m;

}
