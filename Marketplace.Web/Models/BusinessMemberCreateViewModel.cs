using Marketplace.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class BusinessMemberCreateViewModel
    {
        [Required]
        public Guid BusinessId { get; set; }

        [Required(ErrorMessage = "Оберіть користувача")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Оберіть бажану роль")]
        public BusinessMemberRole Role { get; set; }   
    }
}
