using Shared.Services.Authentication;

namespace Shared.Interfaces.AuthServices
{
    public interface IModuleService
    {
        /// <summary>
        /// Đồng bộ module 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SyncModulesAsync(CancellationToken cancellationToken = default);
    }
}
