namespace UniGet
{
    internal class MdbTool
    {
        public static void ConvertPdbToMdb(string dll)
        {
            RunPdb2Mdb(dll);
        }

        private static void RunPdb2Mdb(params string[] args)
        {
            var entryPoint = typeof(Pdb2Mdb.Converter).Assembly.EntryPoint;
            entryPoint.Invoke(null, new object[] { args });
        }
    }
}
