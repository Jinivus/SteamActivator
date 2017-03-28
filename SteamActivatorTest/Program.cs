using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamActivator;

namespace SteamActivatorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            String key = Console.ReadLine();
            SteamClient sa = new SteamClient();
            var res = sa.ActivateKey(key);
            Console.WriteLine(res);
        }
    }
}
