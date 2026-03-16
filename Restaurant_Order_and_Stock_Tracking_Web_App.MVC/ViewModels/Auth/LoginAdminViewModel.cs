using System.ComponentModel.DataAnnotations;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.ViewModels.Auth
{
    public class LoginAdminViewModel
    {
        // ── Kısa Kullanıcı Adı — prefix'siz ─────────────────────────────────
        // Kullanıcı "ahmet" girer; controller arka planda "burger-palace-a1b2c3d4_ahmet"
        // olarak birleştirir ve Identity'de tam username olarak arar.
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
