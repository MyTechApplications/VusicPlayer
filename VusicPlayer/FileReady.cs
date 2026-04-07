using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class FileReady
    {
        public static bool IsFileReady(string path)
        {
            try
            {
                // Try to open the file with Exclusive access
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false; // File is locked by another process
            }
        }

    }
}
