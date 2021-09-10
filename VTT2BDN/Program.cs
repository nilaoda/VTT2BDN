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
            BDNHelper.ConvertToBDN
                (
                    vttPath: input,
                    resW: 1920,
                    resH: 1080,
                    frameRate: 23.976,
                    paddingBottom: 0,
                    paddingSide: 0,
                    generateSup: true
                );
        }
    }
}
