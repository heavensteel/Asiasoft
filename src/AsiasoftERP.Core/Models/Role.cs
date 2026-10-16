using System.ComponentModel.DataAnnotations;

namespace AsiasoftERP.Core.Models;

public class Role
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public bool CanViewCariHesaplar { get; set; } = true;
    public bool CanEditCariHesaplar { get; set; } = false;
    public bool CanViewFaturalar { get; set; } = true;
    public bool CanEditFaturalar { get; set; } = false;
    public bool CanViewStok { get; set; } = true;
    public bool CanEditStok { get; set; } = false;
    public bool CanViewOdeme { get; set; } = true;
    public bool CanEditOdeme { get; set; } = false;
    public bool CanViewBanka { get; set; } = true;
    public bool CanEditBanka { get; set; } = false;
    public bool CanViewRapor { get; set; } = true;
    public bool CanManageUsers { get; set; } = false;
    public bool CanViewVersionHistory { get; set; } = true;
    public bool IsAdmin { get; set; } = false;

    public ICollection<User> Users { get; set; } = new List<User>();
}
