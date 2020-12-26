using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
// ReSharper disable IdentifierTypo

namespace SpriteRotate
{
    class Program
    {
        private static byte[] memdmp = new byte[65536];

        private static readonly Color[] colorsY = new Color[]
        {
            Color.AntiqueWhite,
            Color.FromArgb(255, 120, 120, 120),
            Color.FromArgb(255, 180, 180, 180),
            Color.Black
        };

        private static readonly Color[] colorsB = new Color[]
        {
            Color.PowderBlue,
            Color.FromArgb(255, 120, 120, 120),
            Color.FromArgb(255, 180, 180, 180),
            Color.Black
        };

        static void Main(string[] args)
        {
            byte[] savdmp = File.ReadAllBytes("memdmp.bin");
            Array.Copy(savdmp, 0, memdmp, 0, 65536);

            PrepareFontProto();
            PrepareTilesets();

            using (var file = new StreamWriter("DESOLD.MAC"))
            {
                PrepareRooms(file);

                PrepareCreditsMargins(file);

                file.WriteLine(";");
                file.WriteLine("; List of encoded room addresses, for 72 rooms");
                file.WriteLine("LDE97:");
                PrepareWordRefArrayDump(file, 0xDE97, 0xDF27 - 0xDE97);

                file.WriteLine("LDF27:\t\t; Room description address table, for 72 rooms");
                PrepareWordRefArrayDump(file, 0xDF27, 0xDFB7 - 0xDF27);

                file.WriteLine("; Encoded screen for Small message popup, in Tileset #2");
                file.WriteLine("LEB27:");
                PrepareByteArrayDump(file, 0xEB27, 18);

                file.WriteLine("; Encoded screen for Inventory/Info popup, in Tileset #2");
                file.WriteLine("LF329:");
                PrepareByteArrayDump(file, 0xF329, 38);

                file.WriteLine("; Encoded screen: Data cartridge reader screen, in Tileset #2");
                file.WriteLine("LF42F:");
                PrepareByteArrayDump(file, 0xF42F, 57);

                file.WriteLine("; Encoded screen: Door Lock panel popup, in Tileset #2");
                file.WriteLine("LF468:");
                PrepareByteArrayDump(file, 0xF468, 0xF4B5 - 0xF468);

                file.WriteLine("; Main menu screen, 96 tiles in Tileset #2");
                file.WriteLine("LF4B5:");
                PrepareByteArrayDump(file, 0xF4B5, 0xF515 - 0xF4B5);

                file.WriteLine("; Main menu screen moving background, 96 tiles");
                file.WriteLine("LF515:");
                PrepareByteArrayDump(file, 0xF515, 96);

                file.WriteLine("; Data for flipping a byte");
                file.WriteLine("FLIPAR:");
                PrepareByteFlipArray(file);

                file.WriteLine("; Data for multiplying by 24 for 0..137");
                file.WriteLine("MUL24D:");
                PrepareMultiply24Array(file);

                file.Flush();
                Console.WriteLine("DESOLD.MAC saved");
            }
        }

        static void PrepareRooms(StreamWriter file)
        {
            file.WriteLine("; Encoded rooms");
            for (int r = 0; r < 72; r++)
            {
                int aaddr = 0xDE97 + r * 2;
                int addr = memdmp[aaddr] + memdmp[aaddr + 1] * 256;
                int length = GetRoomEncodedLength(addr, 12 * 8);
                file.WriteLine($"L{addr:X4}:\t\t; Room #{r}");
                PrepareByteArrayDump(file, addr, length);
            }
            file.WriteLine("; Room descriptions, RLE encoded, for 72 rooms, 49 decoded bytes per description");
            for (int r = 0; r < 72; r++)
            {
                int aaddr = 0xDF27 + r * 2;
                int addr = memdmp[aaddr] + memdmp[aaddr + 1] * 256;
                int length = GetRoomEncodedLength(addr, 49);
                file.WriteLine($"L{addr:X4}:\t\t; Room #{r} description");
                PrepareByteArrayDump(file, addr, length);
            }
        }

        static void PrepareFontProto()
        {
            using (var writer = new StreamWriter("DESOLF.MAC"))
            {
                Bitmap bmp = new Bitmap(@"..\fontproto.png");

                writer.WriteLine("FONTPR::");
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 16; col++)
                    {
                        int x = col * 13;
                        int y = 2 + row * 15;
                        var octets = new byte[11];

                        for (int i = 0; i < 11; i++)
                        {
                            int val = 0;
                            for (int b = 0; b < 8; b++)
                            {
                                Color c = bmp.GetPixel(x + b, y + i);
                                int v = (c.GetBrightness() > 0.5f) ? 0 : 1;
                                val |= (v << b);
                            }

                            octets[i] = (byte) val;
                        }

                        bool lowered = octets[10] != 0;
                        byte mask = 0;
                        for (int i = 0; i < 11; i++)
                            mask |= octets[i];
                        if (mask == 0)
                            continue; // Skip empty symbol

                        int width = 0;
                        for (int b = 0; b < 8; b++)
                        {
                            if (((mask >> b) & 1) == 1)
                                width = b + 1;
                        }

                        byte descbyte = (byte) ((lowered ? 128 : 0) + width);

                        writer.Write($"\t.BYTE {EncodeOctalString(descbyte)}, ");
                        var start = lowered ? 1 : 0;
                        for (int i = start; i < start + 10; i++)
                        {
                            writer.Write(EncodeOctalString(octets[i]));
                            if (i < start + 9) writer.Write(",");
                        }

                        var ch = (char) (' ' + col + row * 16);
                        writer.Write($"  ; {ch}");
                        writer.WriteLine();
                    }
                }
                writer.WriteLine();

                Console.WriteLine("DESOLF.MAC saved");
            }
        }

        static void PrepareTilesets()
        {
            const string tilesFileName = "DESOLT.MAC";

            using (var bmp = new Bitmap(@"..\tiles.png"))
            using (var writer = new StreamWriter(tilesFileName))
            {
                writer.WriteLine("\t.EVEN");
                writer.WriteLine(";");
                writer.WriteLine("; Tileset 1, 122 tiles 16x16 no mask");
                writer.WriteLine("TILES1:");
                PrepareTilesetImpl(bmp, 8, 122, writer);

                writer.WriteLine("\t.EVEN");
                writer.WriteLine(";");
                writer.WriteLine("; Sprites, 36 tiles 16x8 with mask");
                writer.WriteLine("SPRITE:");
                PrepareSpritesImpl(bmp, 168, 36, writer);

                writer.WriteLine("\t.EVEN");
                writer.WriteLine(";");
                writer.WriteLine("; Tileset 2, 126 tiles 16x8 with mask");
                writer.WriteLine("TILES2:");
                PrepareSpritesImpl(bmp, 228, 126, writer);

                writer.WriteLine(";");

                PrepareTileset3(bmp, writer);
            }
            Console.WriteLine($"{tilesFileName} saved");
        }

        static void PrepareTilesetImpl(Bitmap bmp, int x0, int tilescount, StreamWriter writer)
        {
            for (int tile = 0; tile < tilescount; tile++)
            {
                var words = new int[16];
                int x = x0 + (tile / 16) * 20;
                int y = 8 + (tile % 16) * 20;
                for (int i = 0; i < 16; i++)
                {
                    int val = 0;
                    for (int b = 0; b < 16; b++)
                    {
                        Color c = bmp.GetPixel(x + b, y + i);
                        int v = (c.GetBrightness() > 0.2f) ? 0 : 1;
                        val |= (v << b);
                    }
                    words[i] = val;
                }

                writer.Write("\t.WORD\t");
                for (int i = 0; i < 16; i++)
                {
                    writer.Write($"{EncodeOctalString2(words[i])}");
                    if (i == 7)
                    {
                        writer.WriteLine();  writer.Write("\t.WORD\t");
                    }
                    else if (i < 15)
                        writer.Write(",");
                }

                writer.WriteLine();
            }
        }

        static void PrepareSpritesImpl(Bitmap bmp, int x0, int tilescount, StreamWriter writer)
        {
            for (int tile = 0; tile < tilescount; tile++)
            {
                var words = new int[16];
                var masks = new int[16];
                int x = x0 + (tile / 16) * 20;
                int y = 8 + (tile % 16) * 20;
                for (int i = 0; i < 16; i++)
                {
                    int val = 0;
                    int valm = 0;
                    for (int b = 0; b < 16; b++)
                    {
                        Color c = bmp.GetPixel(x + b, y + i);
                        int v = (c.GetBrightness() > 0.2f) ? 0 : 1;
                        val |= (v << b);
                        int vm = (c.R == 120 && c.G == 120 && c.B == 120) ? 1 : 0;
                        valm |= (vm << b);
                    }

                    words[i] = val;
                    masks[i] = valm ^ 0xFFFF;
                }

                writer.Write("\t.WORD\t");
                for (int i = 0; i < 16; i++)
                {
                    writer.Write($"{EncodeOctalString2(masks[i])},{EncodeOctalString2(words[i])}");
                    if (i == 3 || i == 7 || i == 11)
                    {
                        writer.WriteLine();  writer.Write("\t.WORD\t");
                    }
                    else if (i < 15)
                        writer.Write(",");
                }

                writer.WriteLine();
            }
        }

        static void PrepareTileset3(Bitmap bmp, StreamWriter writer)
        {
            writer.WriteLine("; Tiles inventory items, 14 tiles 16x16");
            writer.WriteLine("TILES3:");
            for (int tile = 0; tile < 16; tile++)
            {
                var words = new int[16];
                int x = 392;
                int y = 8 + tile * 20;
                for (int i = 0; i < 16; i++)
                {
                    int val = 0;
                    for (int b = 0; b < 16; b++)
                    {
                        Color c = bmp.GetPixel(x + b, y + i);
                        int v = (c.GetBrightness() > 0.5f) ? 0 : 1;
                        val |= (v << b);
                    }

                    words[i] = val;
                }

                writer.Write("\t.WORD\t");
                for (int i = 0; i < 16; i++)
                {
                    writer.Write($"{EncodeOctalString2(words[i])}");
                    if (i == 7)
                    {
                        writer.WriteLine(); writer.Write("\t.WORD\t");
                    }
                    else if (i < 15)
                        writer.Write(",");
                }

                writer.WriteLine();
            }
        }

        static void PrepareCreditsMargins(StreamWriter file)
        {
            file.WriteLine("LDDF2:\t\t\t; Table of left margins for Credits strings");
            int address = 0xDDF2, length = 0xDE47 - 0xDDF2;
            for (int addr = address; addr < address + length; addr++)
            {
                if ((addr - address) % 16 == 0)
                    file.Write("\t.BYTE\t");
                var value = memdmp[addr] * 2;
                file.Write($"{EncodeOctalString((byte)value)}");
                if ((addr - address) % 16 == 15)
                    file.WriteLine();
                else
                    file.Write(",");
            }
            file.WriteLine();
        }

        static void PrepareByteArrayDump(StreamWriter file, int address, int length)
        {
            for (int addr = address; addr < address + length; addr++)
            {
                if ((addr - address) % 16 == 0)
                    file.Write("\t.BYTE\t");
                var value = memdmp[addr];
                file.Write($"{EncodeOctalString((byte)value)}");
                if ((addr - address) % 16 == 15)
                    file.WriteLine();
                else
                    file.Write(",");
            }
            file.WriteLine();
        }

        static void PrepareWordArrayDump(StreamWriter file, int address, int length)
        {
            for (int addr = address; addr < address + length; addr += 2)
            {
                if ((addr - address) % 16 == 0)
                    file.Write("\t.WORD\t");
                var value = memdmp[addr] + memdmp[addr + 1] * 256;
                file.Write($"{EncodeOctalString2(value)}");
                if ((addr - address) % 16 == 14)
                    file.WriteLine();
                else
                    file.Write(",");
            }
            file.WriteLine();
        }

        static void PrepareWordRefArrayDump(StreamWriter file, int address, int length)
        {
            for (int addr = address; addr < address + length; addr += 2)
            {
                if ((addr - address) % 16 == 0)
                    file.Write("\t.WORD\t");
                var value = memdmp[addr] + memdmp[addr + 1] * 256;
                file.Write($"L{value:X4}");
                if ((addr - address) % 16 == 14)
                    file.WriteLine();
                else
                    file.Write(",");
            }
            file.WriteLine();
        }

        static void PrepareByteFlipArray(StreamWriter file)
        {
            for (int b = 0; b < 256; b++)
            {
                if (b % 16 == 0)
                    file.Write("\t.BYTE\t");
                int r = 0;
                for (int i = 0; i < 8; i++)
                {
                    if ((b & (1 << i)) != 0)
                        r |= (1 << (7 - i));
                }
                file.Write($"{EncodeOctalString((byte)r)}");
                if (b % 16 == 15)
                    file.WriteLine();
                else
                    file.Write(",");
            }
        }

        static void PrepareMultiply24Array(StreamWriter file)
        {
            for (int b = 0; b < 138; b++)
            {
                if (b % 16 == 0)
                    file.Write("\t.WORD\t");
                file.Write($"{EncodeOctalString2(b * 24)}");
                if (b % 16 == 15)
                    file.WriteLine();
                else
                    file.Write(",");

            }

        }

        static int GetRoomEncodedLength(int addr, int count)
        {
            int startAddr = addr;

            byte[] room = new byte[count];
            int roomi = 0;
            while (roomi < count)
            {
                byte v = memdmp[addr++];
                if (v != 0xff)
                    room[roomi++] = v;
                else
                {
                    int c = memdmp[addr++];
                    v = memdmp[addr++];
                    for (int i = 0; i < c; i++)
                    {
                        room[roomi++] = v;
                    }
                }
            }

            return addr - startAddr;
        }

        static string EncodeOctalString(byte value)
        {
            //convert to int, for cleaner syntax below. 
            int x = (int)value;

            return string.Format(
                @"{0}{1}{2}",
                ((x >> 6) & 7),
                ((x >> 3) & 7),
                (x & 7)
            );
        }

        static string EncodeOctalString2(int x)
        {
            return string.Format(
                @"{0}{1}{2}{3}{4}{5}",
                ((x >> 15) & 7),
                ((x >> 12) & 7),
                ((x >> 9) & 7),
                ((x >> 6) & 7),
                ((x >> 3) & 7),
                (x & 7)
            );
        }
    }
}
