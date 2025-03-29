using AiStudio4.Core.Models;
using System.Collections.Generic;

namespace AiStudio4.Core.Interfaces
{
    public interface IBuiltinToolService
    {
        List<Tool> GetBuiltinTools();
    }
}
