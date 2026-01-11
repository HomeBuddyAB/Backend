
using System.ComponentModel.DataAnnotations;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.DTOs.Requests;
public class AdjustInventoryRequest
{
    [Required]
    public int Delta { get; set; }

    [Required]
    public InventoryTransactionType TransactionType { get; set; }

    public string? ReferenceId { get; set; }
}
