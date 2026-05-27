namespace OutdoorsShop.Core.DTOs.Orders;

public class OrderReceiptDto
{
    public int OrderID { get; set; }
    public bool ReceiptAvailable { get; set; }
    public string? DownloadUrl { get; set; }
}
