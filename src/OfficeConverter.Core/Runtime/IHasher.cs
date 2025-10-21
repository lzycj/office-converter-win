using System.Threading;
using System.Threading.Tasks;

namespace OfficeConverter.Core.Runtime;

public interface IHasher
{
    Task<string> ComputeHashAsync(string filePath, CancellationToken ct);
}
