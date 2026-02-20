using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public interface IUpdateableMusicPage
    {
        // This says: "Any page using this must have this specific method"
        void UpdateCurrentListhere(string path);
        void UpdateCurrentState(string currentstate);
    }
}
