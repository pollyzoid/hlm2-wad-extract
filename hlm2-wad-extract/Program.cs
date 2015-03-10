using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace hlm2_wad_extract
{
    struct Header
    {
        public Int32 FileCount;
    }

    struct FileHeader
    {
        public Int32 NameLength;
        public String Name;
        public Int64 FileLength;
        public Int64 FileOffset;
    }

    class Program
    {
        static void Extract(string filename)
        {
            using (var wad = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                var hdr = new Header();

                // header
                hdr.FileCount = wad.ReadInt32();

                var files = new List<FileHeader>(hdr.FileCount);

                // file header list
                for (uint i = 0; i < hdr.FileCount; ++i)
                {
                    var fhdr = new FileHeader();

                    fhdr.NameLength = wad.ReadInt32();
                    fhdr.Name = Encoding.UTF8.GetString(wad.ReadBytes(fhdr.NameLength));
                    fhdr.FileLength = wad.ReadInt64();
                    fhdr.FileOffset = wad.ReadInt64();

                    files.Add(fhdr);
                }

                // file data
                foreach (var file in files)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file.Name));

                    using (var of = new BinaryWriter(File.Open(file.Name, FileMode.Create)))
                    {
                        of.Write(wad.ReadBytes((int)file.FileLength));
                    }
                }
            }
        }

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: {0} [FILE]", AppDomain.CurrentDomain.FriendlyName);
                return 1;
            }

            Extract(args[0]);
            return 0;
        }
    }
}
