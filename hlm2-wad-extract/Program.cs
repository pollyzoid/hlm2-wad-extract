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
            Extract(filename, "."); // Set the current directory as the output directory
        }

        static void Extract(string filename, string outputdir)
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
                    var name = outputdir + Path.DirectorySeparatorChar + Path.GetDirectoryName(file.Name) + Path.DirectorySeparatorChar + Path.GetFileName(file.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(name));

                    using (var of = new BinaryWriter(File.Open(name, FileMode.Create)))
                    {
                        Console.WriteLine(file.Name);
                        of.Write(wad.ReadBytes((int)file.FileLength));
                    }
                }
            }
        }
        static Int64 currentoffset = 0;
        static List<FileHeader> WalkDirectory(DirectoryInfo dir, DirectoryInfo basedir = null)
        {
            // basedir is set to the value of dir on the first iteration, but stays the same on the following iterations
            if (basedir == null)
                basedir = dir;

            var files = new List<FileHeader>();
            
            foreach (var file in dir.GetFiles())
            {
                var fhdr = new FileHeader();
                // Remove the base directory from the path, leaving a short path relative to the input directory specified by the user
                fhdr.Name = file.FullName.Remove(0, basedir.FullName.Length + 1);
                fhdr.NameLength = fhdr.Name.Length;
                fhdr.FileLength = file.Length;
                fhdr.FileOffset = currentoffset;

                currentoffset += file.Length;

                files.Add(fhdr);
                
            }

            foreach (var subdir in dir.GetDirectories())
            {
                files.AddRange(WalkDirectory(subdir, basedir));
            }

            return files;
        }

        static void Create(string filename)
        {
            Create(filename, "."); // Set the current directory as the output directory
        }
        static void Create(string filename, string foldername)
        {
            var dirinfo = new DirectoryInfo(foldername);
            var files = WalkDirectory(dirinfo);
            var relativepaths = new List<string>();

            using(var wad = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                wad.Write((Int32)files.Count);

                foreach (var file in files)
                {
                    wad.Write((Int32)file.NameLength);
                    wad.Write(Encoding.ASCII.GetBytes(file.Name.Replace('\\', '/')));
                    wad.Write((Int64)file.FileLength);
                    wad.Write((Int64)file.FileOffset);
                }

                foreach (var file in files)
                {
                    var reader = new BinaryReader(File.Open(foldername + Path.DirectorySeparatorChar + file.Name, FileMode.Open));
                    wad.Write(reader.ReadBytes((int)file.FileLength));
                    
                }
                
            }
        }

        static int Main(string[] args)
        {
            if (args.Length == 0 || args.Length == 1)
            {
                Console.WriteLine("Usage: {0} [-e|-c] [FILE] [DIRECTORY]\n" + 
                                  "If omitted, the output directory defaults to the current directory\n", AppDomain.CurrentDomain.FriendlyName);
                return 1;
            }

            //TODO: add error checking for the command line args, add more option(e.g. -o for output)

            switch (args[0])
            {
                case "-c":
                case "--create":
                    Create(args[1], args[2]);
                    break;
                case "-e":
                case "--extract":
                    Extract(args[1], args[2]);
                    break;
                default:
                    return 1;
            }

            return 0;
        }
    }
}
