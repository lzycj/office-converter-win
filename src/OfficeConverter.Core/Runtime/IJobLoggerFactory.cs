using Microsoft.Extensions.Logging;
using System;

namespace OfficeConverter.Core.Runtime;

public interface IJobLoggerFactory
{
    (ILogger Logger, string LogPath) Create(Guid jobId);
}
