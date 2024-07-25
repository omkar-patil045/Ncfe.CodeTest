using Ncfe.CodeTest.src.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncfe.CodeTest.src.Services.Interfaces
{
    public interface IConfigurationManager
    {
        IAppSettings AppSettings { get; }
    }
}
