using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace VTT2BDN
{
    class Program
    {
        class MyOption
        {
            public string VttPath { get; set; }
            public string FrameRate { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int PaddingBottom { get; set; }
            public int PaddingSide { get; set; }
            public bool GenerateSup { get; set; }
        }

        static int Main(string[] args)
        {
            var rootCommand = new RootCommand
                {
                    new Argument<string>(
                        "vttPath",
                        description: "Your vtt file path"),
                    new Option<string>(
                        new string[]{ "--frame-rate"},
                        getDefaultValue: () => "29.97",
                        "Frame rate. Supported: 23.976;24;25;29.97;30;50;59.94"),
                    new Option<int>(
                        new string[]{ "--width"},
                        getDefaultValue: () => 1920,
                        "Set video width"),
                    new Option<int>(
                        new string[]{ "--height"},
                        getDefaultValue: () => 1080,
                        "Set video height"),
                    new Option<int>(
                        new string[]{ "--padding-bottom"},
                        getDefaultValue: () => 0,
                        "Padding of bottom"),
                    new Option<int>(
                        new string[]{ "--padding-side"},
                        getDefaultValue: () => 0,
                        "Padding of left and right"),
                    new Option<bool>(
                        new string[]{ "--generate-sup"},
                        getDefaultValue: () => true,
                        "Generate sup after xml produced"),
                };


            rootCommand.Description = "Convert vtt with png to BDN xml";
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            rootCommand.Handler = CommandHandler.Create<string, string, int, int, int, int, bool>
                ((vttPath, frameRate, width, height, paddingBottom, paddingSide, generateSup) =>
            {
                DoWork(vttPath, frameRate, width, height, paddingBottom, paddingSide, generateSup);
            });

            return rootCommand.Invoke(args);
        }

        private static void DoWork(string vttPath, string frameRate, int width, int height, int paddingBottom, int paddingSide, bool generateSup)
        {
            try
            {

                if (!new List<string>("23.976;24;25;29.97;30;50;59.94".Split(';')).Contains(frameRate))
                    throw new Exception("Frame Rate Not Suppotted: " + frameRate);

                Console.WriteLine($"FPS: {frameRate}; Resolution: {width}x{height}; Padding: {paddingBottom},{paddingSide}; GenerateSup: {generateSup}");

                //now will ignore vtt style
                if (!File.Exists(vttPath))
                    throw new Exception("File not exists!");
                //BDNHelper.PreProcess(input);
                //return;
                BDNHelper.ConvertToBDN
                    (
                        vttPath: vttPath,
                        resW: width,
                        resH: height,
                        frameRate: Convert.ToDouble(frameRate),
                        paddingBottom: paddingBottom,
                        paddingSide: paddingSide,
                        generateSup: generateSup
                    );
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(ex.Message);
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }
}
