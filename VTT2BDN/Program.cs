using System;
using System.IO;

namespace VTT2BDN
{
    class Program
    {
        static void Main(string[] args)
        {
            //now will ignore vtt style
            var vtt = File.ReadAllText("demo.vtt");
            var xml = BDNHelper.GetBDN(vtt, 1920, 1080, 23.976);
            File.WriteAllText("test.xml", xml, new System.Text.UTF8Encoding(false));
        }
    }
}
