using System.IO;
using System.Text;
using System.IO.Compression;
using static System.Console;
using System;

namespace APK2NSP
{
    internal struct PfsCtor
    {
        public uint Magic;
        public uint NumOfFiles;
        public uint StrTableSize;
        public uint Padding;
        public FileEntryTable[] Entries;
        public string[] StringTable;
    }

    internal struct FileEntryTable
    {
        public ulong Offset;
        public ulong Size;
        public uint StrTableOffset;
        public uint Padding;
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                WriteLine("\nUsage: APK2NSP.exe <Input APK> <Output NSP>");
                Environment.Exit(0);
            }

            var Zip = ZipFile.OpenRead(args[0]);

            var Len = Zip.Entries.Count;

            var StrTable = new string[Len];
            var EntryTable = new FileEntryTable[Len];

            ulong FileOfs = 0;
            uint StrOfs = 0;

            for (int i = 0; i < Len; i++)
            {
                StrTable[i] = Zip.Entries[i].Name;

                EntryTable[i] = new FileEntryTable()
                {
                    Offset = FileOfs,
                    Size = (ulong)Len,
                    StrTableOffset = StrOfs,
                    Padding = 0
                };

                FileOfs += (ulong)Len;
                StrOfs += (uint)StrTable[i].Length;
            }

            var Pfs = new PfsCtor()
            {
                Magic = 0x30534650,
                NumOfFiles = (uint)Len,
                StrTableSize = StrOfs,
                Padding = 0,
                Entries = EntryTable,
                StringTable = StrTable
            };

            using (var Out = File.OpenWrite(args[1]))
            {
                using (var Buf = new BufferedStream(Out, 0x4000))
                {
                    using (var Writer = new BinaryWriter(Buf))
                    {
                        WriteLine("Writing header to PFS...");

                        Writer.Write(Pfs.Magic);
                        Writer.Write(Pfs.NumOfFiles);
                        Writer.Write(Pfs.StrTableSize);
                        Writer.Write(Pfs.Padding);

                        WriteLine("Writing entries to PFS...");

                        foreach (var Entry in Pfs.Entries)
                        {
                            Writer.Write(Entry.Offset);
                            Writer.Write(Entry.Size);
                            Writer.Write(Entry.StrTableOffset);
                            Writer.Write(Entry.Padding);
                        }

                        WriteLine("Writing string table to PFS...\n");

                        for (int i = 0; i < Pfs.StringTable.Length; i++)
                        {
                            Writer.Write(Encoding.ASCII.GetBytes(Pfs.StringTable[i]));
                        }

                        for (int i = 0; i < Len; i++)
                        {
                            WriteLine("Adding {0} to PFS...", Pfs.StringTable[i].Trim('\0'));
                            using (var Read = Zip.Entries[i].Open())
                            {
                                using (var Buf2 = new BufferedStream(Read))
                                {
                                    Buf2.CopyTo(Buf);
                                }
                            }
                        }

                        WriteLine("\nSuccessfully packed APK into an NSP!");
                    }
                }
            }
        }
    }
}