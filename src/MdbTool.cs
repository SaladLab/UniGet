namespace UniGet
{
    internal class MdbTool
    {
        public static void ConvertPdbToMdb(string dll)
        {
            Pdb2Mdb.Converter.Convert(dll);
        }
    }
}
