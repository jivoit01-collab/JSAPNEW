using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(UserDto user);
        bool ValidateToken(string token);
    }
}
