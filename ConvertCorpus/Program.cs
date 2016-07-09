using System.IO;

namespace ConvertCorpus
{
    class Program
    {
        public static void ConvertFormat(string strInputFile, string strOutputFile)
        {
            StreamReader sr = new StreamReader(strInputFile);
            StreamWriter sw = new StreamWriter(strOutputFile);
            string strLine = null;

            while ((strLine = sr.ReadLine()) != null)
            {
                strLine = strLine.Trim();

                string[] items = strLine.Split();
                foreach (string item in items)
                {
                    int pos = item.LastIndexOf('[');
                    string strTerm = item.Substring(0, pos);
                    string strTag = item.Substring(pos + 1, item.Length - pos - 2);

                    sw.WriteLine("{0}\t{1}", strTerm, strTag);
                }
                sw.WriteLine();
            }

            sr.Close();
            sw.Close();
        }

        static void Main(string[] args)
        {
            ConvertFormat(args[0], args[1]);
        }
    }
}
