namespace APK2NSP
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using static System.Console;

    internal struct PfsCtor
    {
        internal uint Magic;
        internal uint NumOfFiles;
        internal uint StrTableSize;
        internal uint Padding;
        internal FileEntryTable[] Entries;
        internal string[] StringTable;
    }

    internal struct FileEntryTable
    {
        internal ulong Offset;
        internal ulong Size;
        internal uint StrTableOffset;
        internal uint Padding;
    }

    internal class Program
    {
        internal static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                WriteLine("\nUsage: APK2NSP.exe <Input APK> <Output NSP>");
                Environment.Exit(0);
            }

            var zip = ZipFile.OpenRead(args[0]);

            var len = zip.Entries.Count;

            var strTable = new string[len];

            var entryTable = new FileEntryTable[len];

            ulong fileOfs = 0;
            uint strOfs = 0;

            for (int i = 0; i < len; i++)
            {
                strTable[i] = zip.Entries[i].Name;

                entryTable[i] = new FileEntryTable()
                {
                    Offset = fileOfs,
                    Size = (ulong)len,
                    StrTableOffset = strOfs,
                    Padding = 0
                };

                fileOfs += (ulong)len;
                strOfs += (uint)strTable[i].Length;
            }

            var pfs = new PfsCtor()
            {
                Magic = 0x30534650,
                NumOfFiles = (uint)len,
                StrTableSize = strOfs,
                Padding = 0,
                Entries = entryTable,
                StringTable = strTable
            };

            using (var output = File.OpenWrite(args[1]))
            using (var buf = new BufferedStream(output, 0x4000))
            using (var writer = new BinaryWriter(buf))
            {
                WriteLine("Writing header to PFS...");

                writer.Write(pfs.Magic);
                writer.Write(pfs.NumOfFiles);
                writer.Write(pfs.StrTableSize);
                writer.Write(pfs.Padding);

                WriteLine("Writing entries to PFS...");

                foreach (var entry in pfs.Entries)
                {
                    writer.Write(entry.Offset);
                    writer.Write(entry.Size);
                    writer.Write(entry.StrTableOffset);
                    writer.Write(entry.Padding);
                }

                WriteLine("Writing string table to PFS...\n");

                for (int i = 0; i < pfs.StringTable.Length; i++)
                {
                    writer.Write(Encoding.ASCII.GetBytes(pfs.StringTable[i]));
                }

                for (int i = 0; i < len; i++)
                {
                    WriteLine("Adding {0} to PFS...", pfs.StringTable[i].Trim('\0'));
                    using (var read = zip.Entries[i].Open())
                    using (var buf2 = new BufferedStream(read))
                    {
                        buf2.CopyTo(buf);
                    }
                }

                WriteLine("\nSuccessfully packed APK into an NSP!");
            }
        }
    }
}
