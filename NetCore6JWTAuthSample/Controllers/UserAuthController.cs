using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NetCore6JWTAuthSample.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NetCore6JWTAuthSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        //1. Yeni bir controller ekledik ve kullanıcıları yönetmek için Microsoft.AspNetCore.Identity tarafından sağlanan UserManager'ı ekledik.
        private readonly UserManager<IdentityUser> _userManager;

        //2. Ayrıca, appsettings.json içindeki JWT ayarlarını okumak için IConfiguration interface'ini ekledik.
        private readonly IConfiguration _configuration;
        
        public UserAuthController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                authClaims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signIn);

            return token;
        }

        //3. Sonra iki adet endpoint oluşturduk, biri kullanıcıların kayıt olabileceği(api/UserAuth/Register), diğeri ise kullanıcıların giriş yapabileceği(api/UserAuth/Login).
        //Register kısmı kullanıcının gönderdiği verileri alacak ve doğrulama yapacak. Eğer kayıt varsa kaydetmeyecek, kayıt yoksa kayıt gerçekleşecek.
        //Login kısmı ise kullanıcının login olup olmadığı kontrol edecek. Eğer kayıt varsa login olacak, yoksa hata mesaşı dönecektir.
        #region Endpoints
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin userLogin)
        {
            var user = await _userManager.FindByNameAsync(userLogin.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, userLogin.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistration userRegistration)
        {
            var userExists = await _userManager.FindByNameAsync(userRegistration.Username);
            
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, "User with this username already exists!");

            IdentityUser user = new()
            {
                Email = userRegistration.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = userRegistration.Username
            };

            var result = await _userManager.CreateAsync(user, userRegistration.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create user, please try again.");

            return Ok("User created successfully.");
        }
        #endregion
    }
}
