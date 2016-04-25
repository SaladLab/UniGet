using System.IO;

namespace UniGet
{
    internal class MdbTool
    {
        public static void ConvertPdbToMdb(string dll)
        {
            var pdbFile = Path.ChangeExtension(dll, ".pdb");
            if (File.Exists(pdbFile))
                Pdb2Mdb.Converter.Convert(dll);
        }
    }
}
