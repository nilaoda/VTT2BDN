﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VTT2BDN
{
    class ImageSub
    {
        public string FileName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    class VttSub
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Payload { get; set; }
        public string Style { get; set; }
    }

    class BDNHelper
    {
        static string XML_STRACTURE = @"<?xml version=""1.0"" encoding=""utf-8""?>
<BDN Version=""0.93"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:noNamespaceSchemaLocation=""BD-03-006-0093b BDN File Format.xsd"">
  <Description>
    <Name Title=""subtitle_from_vtt"" Content="""" />
    <Language Code=""eng"" />
    <Format VideoFormat=""${VideoFormat}"" FrameRate=""${FrameRate}"" DropFrame=""False"" />
    <Events Type=""Graphic"" FirstEventInTC=""${FirstEventInTC}"" LastEventOutTC=""${LastEventOutTC}"" NumberofEvents=""${NumberofEvents}"" />
  </Description>
  <Events>
${Events}
  </Events>
</BDN>";

        public static string GetBDN(string vttContent, int resW, int resH, double frameRate, int paddingBottom = 20)
        {
            var subs = ParseImageSubFromVtts(ParseVttsFromString(vttContent), resW, resH, frameRate, paddingBottom);
            StringBuilder sb = new StringBuilder();
            foreach (var s in subs)
            {
                sb.AppendLine($"    <Event InTC=\"{s.StartTime}\" OutTC=\"{s.EndTime}\" Forced=\"false\">");
                sb.AppendLine($"      <Graphic Width=\"{s.Width}\" Height=\"{s.Height}\" X=\"{s.X}\" Y=\"{s.Y}\">{s.FileName}</Graphic>");
                sb.AppendLine($"    </Event>");
            }
            return XML_STRACTURE
                .Replace("${VideoFormat}", $"{resW}x{resH}")
                .Replace("${FrameRate}", frameRate.ToString())
                .Replace("${FirstEventInTC}", subs.First().StartTime)
                .Replace("${LastEventOutTC}", subs.Last().EndTime)
                .Replace("${NumberofEvents}", subs.Count().ToString())
                .Replace("${Events}", sb.ToString().TrimEnd());
        }

        private static List<ImageSub> ParseImageSubFromVtts(IEnumerable<VttSub> subs, int resW, int resH, double frameRate, int paddingBottom)
        {
            var list = new List<ImageSub>();
            foreach (var s in subs)
            {
                var startFrame = (int)Math.Round(s.StartTime.Milliseconds / (1000.0 / frameRate));
                var endFrame = (int)Math.Round(s.EndTime.Milliseconds / (1000.0 / frameRate));
                var imgW = 0;
                var imgH = 0;
                (imgW, imgH) = GetImgResolution(s.Payload);
                var newSub = new ImageSub();
                newSub.Width = imgW;
                newSub.Height = imgH;
                newSub.StartTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", s.StartTime.Hours, s.StartTime.Minutes, s.StartTime.Seconds, startFrame);
                newSub.EndTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", s.EndTime.Hours, s.EndTime.Minutes, s.EndTime.Seconds, endFrame);
                newSub.X = (int)((resW / 2.0) - (imgW / 2.0));
                newSub.Y = resH - imgH - paddingBottom;
                newSub.FileName = s.Payload;
                list.Add(newSub);
            }
            return list;
        }

        private static (int, int) GetImgResolution(string path)
        {
            Bitmap bitmap = new Bitmap(path);
            return (bitmap.Width, bitmap.Height);
        }

        private static List<VttSub> ParseVttsFromString(string vttContent)
        {
            if (!vttContent.Trim().StartsWith("WEBVTT"))
                throw new Exception("Bad vtt!");
            var vtts = new List<VttSub>();
            var needPayload = false;
            var timeLine = "";
            var index = 0;
            foreach (var line in vttContent.Split('\n'))
            {
                if (string.IsNullOrEmpty(line)) continue;

                if (!needPayload && line.Contains(" --> "))
                {
                    needPayload = true;
                    timeLine = line.Trim();
                    continue;
                }

                if (needPayload)
                {
                    var payload = line;
                    if (payload.StartsWith("http"))
                    {
                        var filename = index++.ToString("0000") + ".png";
                        Console.WriteLine($"Downloading: {payload} ==> {filename}");
                        new WebClient().DownloadFile(payload, filename);
                        payload = filename;
                    }
                    var arr = Regex.Split(timeLine.Replace("-->", ""), "\\s").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var startTime = ConvertToTS(arr[0]);
                    var endTime = ConvertToTS(arr[1]);
                    var style = arr.Count > 2 ? string.Join(" ", arr.Skip(2)) : "";
                    vtts.Add(new VttSub()
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        Payload = payload,
                        Style = style
                    });
                    needPayload = false;
                }
            }
            return vtts;
        }

        private static TimeSpan ConvertToTS(string str)
        {
            var ms = Convert.ToInt32(str.Split('.').Last());
            var o = str.Split('.').First();
            var t = o.Split(':').Reverse().ToList();
            var time = 0L + ms;
            for (int i = 0; i < t.Count(); i++)
            {
                time += (int)Math.Pow(60, i) * Convert.ToInt32(t[i]) * 1000;
            }
            return TimeSpan.FromMilliseconds(time);
        }
    }
}
