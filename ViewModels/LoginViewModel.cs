using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }
    }
}