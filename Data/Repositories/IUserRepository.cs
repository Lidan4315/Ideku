using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(long id);
    }
}