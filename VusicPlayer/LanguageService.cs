using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;

namespace VusicPlayer
{
    public static class Strings
    {
        private static readonly ResourceLoader _loader = new ResourceLoader();

        // These properties act as "Shortcuts" to your IDs
        public static string VersionType => Get("sc0x001");
        public static string VersionText => Get("sc0x002");
        public static string BuildText => Get("sc0x003");

        /// <summary>
        /// Helper to fetch strings. Automatically appends "/Text" 
        /// since you used .Text in your Resources.resw
        /// </summary>
        private static string Get(string key)
        {
            // We append "/Text" here so you don't have to repeat it for every property
            return _loader.GetString($"{key}/Text");
        }
    }
}
