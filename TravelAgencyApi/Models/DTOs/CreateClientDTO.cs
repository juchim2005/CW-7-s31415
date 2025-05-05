using System.ComponentModel.DataAnnotations;

namespace TravelAgencyApi.Models.DTOs;

public class CreateClientDTO
{
    [Required]
    [Length(3,40)]
    public string FirstName { get; set; }
    
    [Required]
    [Length(3,40)]
    public string LastName { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(50)]
    public string Email { get; set; }
    
    [Required]
    [Phone]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "Nr. telefonu powinien mieć 9 liczb.")]
    public string Telephone { get; set; }
    
    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Pesel ma mieć 11 liczb.")]
    public string Pesel { get; set; }
}