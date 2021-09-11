/*
 * Copyright 2009 Volker Oth (0xdeadbeef)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * NOTE: Converted to C# and modified by Nikse.dk@gmail.com
 * NOTE: For more info see http://blog.thescorpius.com/index.php/2017/07/15/presentation-graphic-stream-sup-files-bluray-subtitle-format/
 */

using Nikse.SubtitleEdit.Core.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTT2BDN;

namespace Nikse.SubtitleEdit.Core.BluRaySup
{
    class BluRaySupPicture
    {
        private static List<Color> GetBitmapPalette(NikseBitmap bitmap)
        {
            var pal = new List<Color>();
            var lookup = new HashSet<int>();
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    if (c != Color.Transparent)
                    {
                        if (lookup.Contains(c.ToArgb()))
                        {
                            // exact color already exists
                        }
                        else if (pal.Count < 100)
                        {
                            if (!HasCloseColor(c, pal, 1))
                            {
                                pal.Add(c);
                                lookup.Add(c.ToArgb());
                            }
                        }
                        else if (pal.Count < 240)
                        {
                            if (!HasCloseColor(c, pal, 5))
                            {
                                pal.Add(c);
                                lookup.Add(c.ToArgb());
                            }
                        }
                        else if (pal.Count < 254 && !HasCloseColor(c, pal, 25))
                        {
                            pal.Add(c);
                            lookup.Add(c.ToArgb());
                        }
                    }
                }
            }

            pal.Add(Color.Transparent); // last entry must be transparent
            return pal;
        }

        private static bool HasCloseColor(Color color, List<Color> palette, int maxDifference)
        {
            var max = palette.Count;
            for (int i = 0; i < max; i++)
            {
                var c = palette[i];
                int difference = Math.Abs(c.A - color.A) + Math.Abs(c.R - color.R) + Math.Abs(c.G - color.G) + Math.Abs(c.B - color.B);
                if (difference < maxDifference)
                {
                    return true;
                }
            }
            return false;
        }

        private static byte FindBestMatch(Color color, List<Color> palette)
        {
            int smallestDiff = 1000;
            int smallestDiffIndex = -1;
            int max = palette.Count;
            for (int i = 0; i < max; i++)
            {
                var c = palette[i];
                int diff = Math.Abs(c.A - color.A) + Math.Abs(c.R - color.R) + Math.Abs(c.G - color.G) + Math.Abs(c.B - color.B);
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    smallestDiffIndex = i;
                }
            }
            return (byte)smallestDiffIndex;
        }

        /// <summary>
        /// Create RLE buffer from bitmap
        /// </summary>
        /// <param name="bm">Bitmap to compress</param>
        /// <param name="palette">Palette used for bitmap encoding</param>
        /// <returns>RLE buffer</returns>
        private static byte[] EncodeImage(NikseBitmap bm, List<Color> palette)
        {
            var lookup = new Dictionary<int, int>();
            for (int i = 0; i < palette.Count; i++)
            {
                var color = palette[i].ToArgb();
                if (!lookup.ContainsKey(color))
                {
                    lookup.Add(color, i);
                }
            }

            var bytes = new List<byte>();
            for (int y = 0; y < bm.Height; y++)
            {
                int x;
                int len;
                for (x = 0; x < bm.Width; x += len)
                {
                    Color c = bm.GetPixel(x, y);
                    byte color;
                    if (lookup.TryGetValue(c.ToArgb(), out int intC))
                    {
                        color = (byte)intC;
                    }
                    else
                    {
                        color = FindBestMatch(c, palette);
                    }

                    for (len = 1; x + len < bm.Width; len++)
                    {
                        if (bm.GetPixel(x + len, y) != c)
                        {
                            break;
                        }
                    }

                    if (len <= 2 && color != 0)
                    {
                        // only a single occurrence -> add color
                        bytes.Add(color);
                        if (len == 2)
                        {
                            bytes.Add(color);
                        }
                    }
                    else
                    {
                        if (len > 0x3fff)
                        {
                            len = 0x3fff;
                        }

                        bytes.Add(0); // rle id
                        // commented out due to bug in SupRip
                        /*if (color == 0 && x+len == bm.Width)
                        {
                            bytes.Add(0);
                            eol = true;
                        }
                        else */
                        if (color == 0 && len < 0x40)
                        {
                            // 00 xx -> xx times 0
                            bytes.Add((byte)len);
                        }
                        else if (color == 0)
                        {
                            // 00 4x xx -> xxx zeroes
                            bytes.Add((byte)(0x40 | (len >> 8)));
                            bytes.Add((byte)len);
                        }
                        else if (len < 0x40)
                        {
                            // 00 8x cc -> x times value cc
                            bytes.Add((byte)(0x80 | len));
                            bytes.Add(color);
                        }
                        else
                        {
                            // 00 cx yy cc -> xyy times value cc
                            bytes.Add((byte)(0xc0 | (len >> 8)));
                            bytes.Add((byte)len);
                            bytes.Add(color);
                        }
                    }
                }
                if (x == bm.Width)
                {
                    bytes.Add(0); // rle id
                    bytes.Add(0);
                }
            }
            int size = bytes.Count;
            var retval = new byte[size];
            for (int i = 0; i < size; i++)
            {
                retval[i] = bytes[i];
            }

            return retval;
        }

        /// <summary>
        /// Get ID for given frame rate
        /// </summary>
        /// <param name="fps">frame rate</param>
        /// <returns>byte ID for the given frame rate</returns>
        private static int GetFpsId(double fps)
        {
            if (Math.Abs(fps - Core.Fps24Hz) < 0.01) // 24
            {
                return 0x20;
            }

            if (Math.Abs(fps - Core.FpsPal) < 0.01) // 25
            {
                return 0x30;
            }

            if (Math.Abs(fps - Core.FpsNtsc) < 0.01) // 29.97
            {
                return 0x40;
            }

            if (Math.Abs(fps - 30.0) < 0.01) // 30
            {
                return 0x50;
            }

            if (Math.Abs(fps - Core.FpsPalI) < 0.01) // 50
            {
                return 0x60;
            }

            if (Math.Abs(fps - Core.FpsNtscI) < 0.1) // 59.94
            {
                return 0x70;
            }

            return 0x10; // 23.976
        }

        private static int _lastEndTimeForWrite = -1000;

        public static byte[] CreateSupFrame(SupSub supSub, int resW, int resH, double fps, int CompositionNumber)
        {
            var bmp = supSub.Bitmap;
            var bm = new NikseBitmap(bmp);
            bm.SetTransparentTo(Color.Transparent);
            var colorPalette = GetBitmapPalette(bm);
            var pal = new BluRaySupPalette(colorPalette.Count);
            for (int i = 0; i < colorPalette.Count; i++)
            {
                pal.SetColor(i, colorPalette[i]);
            }

            byte[] rleBuf = EncodeImage(bm, colorPalette);

            // for some obscure reason, a packet can be a maximum 0xfffc bytes
            // since 13 bytes are needed for the header("PG", PTS, DTS, ID, SIZE)
            // there are only 0xffef bytes available for the packet
            // since the first ODS packet needs an additional 11 bytes for info
            // and the following ODS packets need 4 additional bytes, the
            // first package can store only 0xffe4 RLE buffer bytes and the
            // following packets can store 0xffeb RLE buffer bytes
            int numAddPackets;
            if (rleBuf.Length <= 0xffe4)
            {
                numAddPackets = 0; // no additional packets needed
            }
            else
            {
                numAddPackets = 1 + (rleBuf.Length - 0xffe4) / 0xffeb;
            }

            // a typical frame consists of 8 packets. It can be elongated by additional object frames
            int palSize = colorPalette.Count;

            var packetHeader = new byte[]
            {
                0x50, 0x47,             // 0:  "PG"
                0x00, 0x00, 0x00, 0x00, // 2:  PTS - presentation time stamp
                0x00, 0x00, 0x00, 0x00, // 6:  DTS - decoding time stamp
                0x00,                   // 10: segment_type
                0x00, 0x00              // 11: segment_length (bytes following till next PG)
            };
            var headerPcsStart = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // 0: video_width, video_height
                0x10,                   // 4: hi nibble: frame_rate (0x10=24p), lo nibble: reserved
                0x00, 0x00,             // 5: composition_number (increased by start and end header)
                0x80,                   // 7: composition_state (0x80: epoch start)
                                        //      0x00: Normal
                                        //      0x40: Acquisition Point
                                        //      0x80: Epoch Start
                0x00,                   // 8: palette_update_flag (0x80==true, 0x00==false), 7bit reserved
                0x00,                   // 9: palette_id_ref (0..7)
                0x01,                   // 10: number_of_composition_objects (0..2)
                0x00, 0x00,             // 11: 16bit object_id_ref
                0x00,                   // 13: window_id_ref (0..1)
                0x00,                   // 14: object_cropped_flag: 0x80, forced_on_flag = 0x040, 6bit reserved
                0x00, 0x00, 0x00, 0x00  // 15: composition_object_horizontal_position, composition_object_vertical_position
            };
            var headerPcsEnd = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // 0: video_width, video_height
                0x10,                   // 4: hi nibble: frame_rate (0x10=24p), lo nibble: reserved
                0x00, 0x00,             // 5: composition_number (increased by start and end header)
                0x00,                   // 7: composition_state (0x00: normal)
                0x00,                   // 8: palette_update_flag (0x80), 7bit reserved
                0x00,                   // 9: palette_id_ref (0..7)
                0x00                    // 10: number_of_composition_objects (0..2)
            };
            var headerWds = new byte[]
            {
                0x01,                   // 0 : number of windows (currently assumed 1, 0..2 is legal)
                0x00,                   // 1 : window id (0..1)
                0x00, 0x00, 0x00, 0x00, // 2 : x-ofs, y-ofs
                0x00, 0x00, 0x00, 0x00  // 6 : width, height
            };
            var headerOdsFirst = new byte[]
            {
                0x00, 0x00,             // 0: object_id
                0x00,                   // 2: object_version_number
                0xC0,                   // 3: first_in_sequence (0x80), last_in_sequence (0x40), 6bits reserved
                0x00, 0x00, 0x00,       // 4: object_data_length - full RLE buffer length (including 4 bytes size info)
                0x00, 0x00, 0x00, 0x00  // 7: object_width, object_height
            };
            var headerOdsNext = new byte[]
            {
                0x00, 0x00,             // 0: object_id
                0x00,                   // 2: object_version_number
                0x40                    // 3: first_in_sequence (0x80), last_in_sequence (0x40), 6bits reserved
            };

            int size = packetHeader.Length * (8 + numAddPackets);
            size += headerPcsStart.Length + headerPcsEnd.Length;
            size += 2 * headerWds.Length + headerOdsFirst.Length;
            size += numAddPackets * headerOdsNext.Length;
            size += (2 + palSize * 5) /* PDS */;
            size += rleBuf.Length;

            int h = resH - 2 * Core.CropOfsY;

            var buf = new byte[size];
            int index = 0;
            int fpsId = GetFpsId(fps);

            /* time (in 90kHz resolution) needed to initialize (clear) the screen buffer
                based on the composition pixel rate of 256e6 bit/s - always rounded up */
            int frameInitTime = (resW * resH * 9 + 3199) / 3200; // better use default height here
            /* time (in 90kHz resolution) needed to initialize (clear) the window area
                based on the composition pixel rate of 256e6 bit/s - always rounded up
                Note: no cropping etc. -> window size == image size */
            int windowInitTime = (bm.Width * bm.Height * 9 + 3199) / 3200;
            /* time (in 90kHz resolution) needed to decode the image
                based on the decoding pixel rate of 128e6 bit/s - always rounded up  */
            int imageDecodeTime = (bm.Width * bm.Height * 9 + 1599) / 1600;
            // write PCS start - Presentation Composition Segment (also called the Control Segment)
            packetHeader[10] = 0x16; // ID

            var pts = supSub.StartTimeForWrite;

            _lastEndTimeForWrite = supSub.EndTimeForWrite;

            int dts = pts - (frameInitTime + windowInitTime + imageDecodeTime); //int dts = pic.StartTimeForWrite - windowInitTime; ???

            ToolBox.SetDWord(packetHeader, 2, pts);                     // PTS
            ToolBox.SetDWord(packetHeader, 6, dts);                     // DTS
            ToolBox.SetWord(packetHeader, 11, headerPcsStart.Length);   // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            ToolBox.SetWord(headerPcsStart, 0, resW);
            ToolBox.SetWord(headerPcsStart, 2, h);                      // cropped height
            ToolBox.SetByte(headerPcsStart, 4, fpsId);
            ToolBox.SetWord(headerPcsStart, 5, CompositionNumber);
            headerPcsStart[14] = (byte)0;                               // IsForced 0x40 or 0
            ToolBox.SetWord(headerPcsStart, 15, supSub.X);
            ToolBox.SetWord(headerPcsStart, 17, supSub.Y);
            for (int i = 0; i < headerPcsStart.Length; i++)
            {
                buf[index++] = headerPcsStart[i];
            }

            // write WDS
            packetHeader[10] = 0x17;                                            // ID
            int timestamp = pts - windowInitTime;
            ToolBox.SetDWord(packetHeader, 2, timestamp);               // PTS (keep DTS)
            ToolBox.SetWord(packetHeader, 11, headerWds.Length);        // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            ToolBox.SetWord(headerWds, 2, supSub.X);
            ToolBox.SetWord(headerWds, 4, supSub.Y);
            ToolBox.SetWord(headerWds, 6, bm.Width);
            ToolBox.SetWord(headerWds, 8, bm.Height);
            for (int i = 0; i < headerWds.Length; i++)
            {
                buf[index++] = headerWds[i];
            }

            // write PDS - Palette Definition Segment
            packetHeader[10] = 0x14;                                            // ID
            ToolBox.SetDWord(packetHeader, 2, dts);                     // PTS (=DTS of PCS/WDS)
            ToolBox.SetDWord(packetHeader, 6, 0);                       // DTS (0)
            ToolBox.SetWord(packetHeader, 11, 2 + palSize * 5);         // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            buf[index++] = 0;
            buf[index++] = 0;
            for (int i = 0; i < palSize; i++)
            {
                buf[index++] = (byte)i;                                         // index
                buf[index++] = pal.GetY()[i];                                   // Y
                buf[index++] = pal.GetCr()[i];                                  // Cr
                buf[index++] = pal.GetCb()[i];                                  // Cb
                buf[index++] = pal.GetAlpha()[i];                               // Alpha
            }

            // write first OBJ
            int bufSize = rleBuf.Length;
            int rleIndex = 0;
            if (bufSize > 0xffe4)
            {
                bufSize = 0xffe4;
            }

            packetHeader[10] = 0x15;                                                    // ID
            timestamp = 0;
            ToolBox.SetDWord(packetHeader, 2, timestamp);                       // PTS
            ToolBox.SetDWord(packetHeader, 6, dts);                             // DTS
            ToolBox.SetWord(packetHeader, 11, headerOdsFirst.Length + bufSize); // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            int marker = (int)((numAddPackets == 0) ? 0xC0000000 : 0x80000000);
            ToolBox.SetDWord(headerOdsFirst, 3, marker | (rleBuf.Length + 4));
            ToolBox.SetWord(headerOdsFirst, 7, bm.Width);
            ToolBox.SetWord(headerOdsFirst, 9, bm.Height);
            for (int i = 0; i < headerOdsFirst.Length; i++)
            {
                buf[index++] = headerOdsFirst[i];
            }

            for (int i = 0; i < bufSize; i++)
            {
                buf[index++] = rleBuf[rleIndex++];
            }

            // write additional OBJ packets
            bufSize = rleBuf.Length - bufSize; // remaining bytes to write
            for (int p = 0; p < numAddPackets; p++)
            {
                int psize = bufSize;
                if (psize > 0xffeb)
                {
                    psize = 0xffeb;
                }

                packetHeader[10] = 0x15;                                         // ID (keep DTS & PTS)
                ToolBox.SetWord(packetHeader, 11, headerOdsNext.Length + psize); // size
                for (int i = 0; i < packetHeader.Length; i++)
                {
                    buf[index++] = packetHeader[i];
                }

                for (int i = 0; i < headerOdsNext.Length; i++)
                {
                    buf[index++] = headerOdsNext[i];
                }

                for (int i = 0; i < psize; i++)
                {
                    buf[index++] = rleBuf[rleIndex++];
                }

                bufSize -= psize;
            }

            // write END
            packetHeader[10] = 0x80;                                            // ID
            ToolBox.SetDWord(packetHeader, 6, 0);                               // DTS (0) (keep PTS of ODS)
            ToolBox.SetWord(packetHeader, 11, 0);                               // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            // write PCS end
            packetHeader[10] = 0x16;                                            // ID
            ToolBox.SetDWord(packetHeader, 2, supSub.EndTimeForWrite);          // PTS
            dts = supSub.EndTimeForWrite - 90;
            ToolBox.SetDWord(packetHeader, 6, dts);                             // DTS
            ToolBox.SetWord(packetHeader, 11, headerPcsEnd.Length);             // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            ToolBox.SetWord(headerPcsEnd, 0, resW);
            ToolBox.SetWord(headerPcsEnd, 2, h);                                // cropped height
            ToolBox.SetByte(headerPcsEnd, 4, fpsId);
            ToolBox.SetWord(headerPcsEnd, 5, CompositionNumber + 1);
            for (int i = 0; i < headerPcsEnd.Length; i++)
            {
                buf[index++] = headerPcsEnd[i];
            }

            // write WDS - Window Definition Segment
            packetHeader[10] = 0x17;                                            // ID
            timestamp = supSub.EndTimeForWrite - windowInitTime;
            ToolBox.SetDWord(packetHeader, 2, timestamp);                       // PTS
            ToolBox.SetDWord(packetHeader, 6, dts - windowInitTime);            // DTS
            ToolBox.SetWord(packetHeader, 11, headerWds.Length);                // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            ToolBox.SetWord(headerWds, 2, supSub.X);
            ToolBox.SetWord(headerWds, 4, supSub.Y);
            ToolBox.SetWord(headerWds, 6, bm.Width);
            ToolBox.SetWord(headerWds, 8, bm.Height);
            for (int i = 0; i < headerWds.Length; i++)
            {
                buf[index++] = headerWds[i];
            }

            // write END
            packetHeader[10] = 0x80;                                            // ID
            ToolBox.SetDWord(packetHeader, 2, dts);                     // PTS (DTS of end PCS)
            ToolBox.SetDWord(packetHeader, 6, 0);                       // DTS (0)
            ToolBox.SetWord(packetHeader, 11, 0);                       // size
            for (int i = 0; i < packetHeader.Length; i++)
            {
                buf[index++] = packetHeader[i];
            }

            return buf;
        }
    }
}
