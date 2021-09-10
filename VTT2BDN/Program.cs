using System;
using System.IO;

namespace VTT2BDN
{
    class Program
    {
        static void Main(string[] args)
        {

            //now will ignore vtt style
            var input = @"D:\VSProjects\VTT2BDN\VTT2BDN\bin\Debug\net5.0\demo\test_pre.vtt";
            //BDNHelper.PreProcess(input);
            //return;
            var folder = Path.GetDirectoryName(Path.GetFullPath(input));
            var name = Path.GetFileNameWithoutExtension(input);
            var vtt = File.ReadAllText(input);
            var xml = BDNHelper.GetBDN(vtt, 1920, 1080, 23.976, 0, 0, folder);
            var output = Path.Combine(folder, $"{name}.xml");
            File.WriteAllText(output, xml, new System.Text.UTF8Encoding(false));
            Console.WriteLine("Done.." + output);
        }
    }
}
