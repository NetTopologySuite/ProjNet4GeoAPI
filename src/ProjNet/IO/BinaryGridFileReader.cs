
namespace ProjNet.IO
{
    using ProjNet.NTv2;
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Reads a binary grid file (NTv2).
    /// </summary>
    class BinaryGridFileReader
    {
        const int EXPECTED_OREC = 11;
        const int EXPECTED_SREC = 11;
        const int NAME_LEN = 8;

        bool reverse = false;
        bool padding = false;

        /// <summary>
        /// Reads a binary grid file (.gsb).
        /// </summary>
        /// <param name="file">The binary grid file.</param>
        /// <returns>The grid file.</returns>
        public GridFile Read(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                return Read(stream);
            }
        }

        /// <summary>
        /// Reads a grid file from given stream.
        /// </summary>
        /// <param name="stream">The stream containing the binary grid data.</param>
        /// <returns>The grid file.</returns>
        public GridFile Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var header = ReadHeader(reader);

                var g = new GridFile(header);

                for (int i = 0; i < header.NUM_FILE; i++)
                {
                    var s = ReadGridHeader(reader);

                    g.grids.Add(ReadGrid(reader, s));
                }

                return g;
            }
        }

        private GridFileHeader ReadHeader(BinaryReader reader)
        {
            int k;

            var header = new GridFileHeader();

            byte[] buffer = new byte[NAME_LEN];

            // --- NUM_OREC

            reader.Read(buffer, 0, NAME_LEN); // string
            reader.Read(buffer, 0, 4);
            k = BitConverter.ToInt32(buffer, 0);

            // Determine if byte-swapping is needed.

            if (k != EXPECTED_OREC)
            {
                Array.Reverse(buffer, 0, 4);
                k = BitConverter.ToInt32(buffer, 0);

                if (k != EXPECTED_OREC)
                {
                    throw new FormatException("Invalid grid header (expected NUM_OREC = 11).");
                }

                reverse = true;
            }

            header.NUM_OREC = k;

            // Determine if pad-bytes are present.

            reader.Read(buffer, 0, 4);
            k = BitConverter.ToInt32(buffer, 0);

            if (k == 0)
            {
                padding = true;
            }
            else
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
            }

            // --- NUM_SREC

            reader.Read(buffer, 0, NAME_LEN); // string
            header.NUM_SREC = reader.ReadInt32(buffer, reverse);

            if (header.NUM_SREC != EXPECTED_SREC)
            {
                throw new FormatException("Invalid grid header (expected NUM_SREC = 11).");
            }

            if (padding) reader.Read(buffer, 0, 4);

            // --- NUM_FILE

            reader.Read(buffer, 0, NAME_LEN); // string
            header.NUM_FILE = reader.ReadInt32(buffer, reverse);

            if (padding) reader.Read(buffer, 0, 4);

            // --- GS_TYPE

            reader.Read(buffer, 0, NAME_LEN); // string
            header.GS_TYPE = reader.ReadString(buffer, NAME_LEN);

            // --- VERSION

            reader.Read(buffer, 0, NAME_LEN); // string
            header.VERSION = reader.ReadString(buffer, NAME_LEN);

            // --- SYSTEM_F

            reader.Read(buffer, 0, NAME_LEN); // string
            header.SYSTEM_F = reader.ReadString(buffer, NAME_LEN);

            // --- SYSTEM_T

            reader.Read(buffer, 0, NAME_LEN); // string
            header.SYSTEM_T = reader.ReadString(buffer, NAME_LEN);

            // --- MAJOR_F

            reader.Read(buffer, 0, NAME_LEN); // string
            header.MAJOR_F = reader.ReadDouble(buffer, reverse);

            // --- MINOR_F

            reader.Read(buffer, 0, NAME_LEN); // string
            header.MINOR_F = reader.ReadDouble(buffer, reverse);

            // --- MAJOR_T

            reader.Read(buffer, 0, NAME_LEN); // string
            header.MAJOR_T = reader.ReadDouble(buffer, reverse);

            // --- MINOR_T

            reader.Read(buffer, 0, NAME_LEN); // string
            header.MINOR_T = reader.ReadDouble(buffer, reverse);

            return header;
        }

        private GridHeader ReadGridHeader(BinaryReader reader)
        {
            var header = new GridHeader();

            byte[] buffer = new byte[NAME_LEN];

            // --- SUB_NAME

            reader.Read(buffer, 0, NAME_LEN); // string
            header.SUB_NAME = reader.ReadString(buffer, NAME_LEN);

            // --- PARENT

            reader.Read(buffer, 0, NAME_LEN); // string
            header.PARENT = reader.ReadString(buffer, NAME_LEN);

            // --- CREATED

            reader.Read(buffer, 0, NAME_LEN); // string
            header.CREATED = reader.ReadString(buffer, NAME_LEN);

            // --- UPDATED

            reader.Read(buffer, 0, NAME_LEN); // string
            header.UPDATED = reader.ReadString(buffer, NAME_LEN);

            // --- S_LAT

            reader.Read(buffer, 0, NAME_LEN); // string
            header.S_LAT = reader.ReadDouble(buffer, reverse);

            // --- N_LAT

            reader.Read(buffer, 0, NAME_LEN); // string
            header.N_LAT = reader.ReadDouble(buffer, reverse);

            // --- E_LONG

            reader.Read(buffer, 0, NAME_LEN); // string
            header.E_LONG = reader.ReadDouble(buffer, reverse);

            // --- W_LONG

            reader.Read(buffer, 0, NAME_LEN); // string
            header.W_LONG = reader.ReadDouble(buffer, reverse);

            // --- LAT_INC

            reader.Read(buffer, 0, NAME_LEN); // string
            header.LAT_INC = reader.ReadDouble(buffer, reverse);

            // --- LONG_INC

            reader.Read(buffer, 0, NAME_LEN); // string
            header.LONG_INC = reader.ReadDouble(buffer, reverse);

            // --- GS_COUNT

            reader.Read(buffer, 0, NAME_LEN); // string
            header.GS_COUNT = reader.ReadInt32(buffer, reverse);

            if (padding) reader.Read(buffer, 0, 4);

            return header;
        }

        private Grid ReadGrid(BinaryReader reader, GridHeader header, bool accuracies = false)
        {
            int count = header.GS_COUNT;

            byte[] buffer = new byte[16];

            var grid = new Grid(header, accuracies);

            ShiftRecord record;

            for (int i = 0; i < count; i++)
            {
                reader.Read(buffer, 0, 16);

                record.Item1 = BitConverter.ToSingle(buffer, 0);
                record.Item2 = BitConverter.ToSingle(buffer, 4);

                grid.shifts[i] = record;

                if (accuracies)
                {
                    record.Item1 = BitConverter.ToSingle(buffer, 8);
                    record.Item2 = BitConverter.ToSingle(buffer, 12);

                    grid.accuracies[i] = record;
                }
            }

            return grid;
        }
    }

    static class BinaryReaderExtensions
    {
        public static int ReadInt32(this BinaryReader reader, byte[] buffer, bool reverse)
        {
            reader.Read(buffer, 0, 4);

            if (reverse)
            {
                Array.Reverse(buffer, 0, 4);
            }

            return BitConverter.ToInt32(buffer, 0);
        }

        public static double ReadDouble(this BinaryReader reader, byte[] buffer, bool reverse)
        {
            reader.Read(buffer, 0, 8);

            if (reverse)
            {
                Array.Reverse(buffer, 0, 8);
            }

            return BitConverter.ToDouble(buffer, 0);
        }

        public static string ReadString(this BinaryReader reader, byte[] buffer, int length)
        {
            reader.Read(buffer, 0, length);

            int i = length - 1;

            while (i >= 0)
            {
                int c = buffer[i];

                if (c > 0x20 && c <= 0x7E)
                {
                    break;
                }

                i--;
            }

            return Encoding.ASCII.GetString(buffer, 0, i + 1);
        }
    }
}
