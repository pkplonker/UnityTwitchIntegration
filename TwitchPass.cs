using System;
using UnityEngine;

namespace TwitchIntegration
{
    [Serializable]
    public class TwitchPass
    {
        public string password;

        public TwitchPass(string password)
        {
            this.password = password;
        }
    }
}
