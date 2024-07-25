using Ncfe.CodeTest.src.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ncfe.CodeTest.src.Configuration
{
    public class ConfigurationManager : IConfigurationManager
    {
        public IAppSettings AppSettings { get; }

        public ConfigurationManager()
        {
            AppSettings = new AppSettings();
        }
    }

    public class AppSettings : IAppSettings
    {
        public string this[string key]
        {
            get => System.Configuration.ConfigurationManager.AppSettings[key];
            set => throw new NotSupportedException();
        }
    }
}
