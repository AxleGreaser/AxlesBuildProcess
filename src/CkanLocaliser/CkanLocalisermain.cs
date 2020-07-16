using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CkanLocaliser
{
    partial class CkanLocaliserClass
    {
        static int FileCount = 0;
        static int InValidCount = 0;
        static bool DoPause = false;
        /// <summary>
        /// Not used yet
        /// </summary>
        static bool CanEchoContext = true;

        public static int RecurseDepthLimit = 4;
        public static int FileCountLimit = 100000;

        static string Appname = new string("");
        static string pwd = new string("");

        static string Author = "localiser";
        static string Prefix = "lcl";

        public static Stack<string> DoingWhat = new Stack<string>();

        public static  string WhatString()
        {
            StringBuilder SB = new StringBuilder();
            foreach ( string S in DoingWhat ) { SB.Append(S); SB.Append("/"); }
            return SB.ToString();
        }


        public static string DownloadPath = "";

        static int Main(string[] args)
        {
            int ret = 0;
            try
            {
                string EV = Environment.GetEnvironmentVariable("CKANlocaliser");
                if (EV != null) {
                    string[] Bits = EV.Split('/');
                    if (Bits.Length > 0) { Author = Bits[0]; }
                    if (Bits.Length > 1) { Prefix = Bits[1]; }
                }
                if (args.Length < 1)
                {
                    Console.Error.WriteLine("I have nothing to do or live for (argv[0] == null); Goodbye Cruel World!");
                    return -1;
                }
                if (args[0].Equals("Validate"))
                {
                    ret = Validate(args);
                } else if (args[0].Equals("Localise"))
                {
                    ret = Localise(args);
                } else if (args[0].Equals("Regression"))
                {
                    ret = Regression();

                } else
                {
                    Usagesmsg();
                    return - 1;
                }
            }
            catch (CommandException ce)
            {
                Console.Error.WriteLine(ce.Cause);
                Usagesmsg();
                return -1;
            }
            string Fool = (FileCount<100) ? "" : (InValidCount == 0) ? " ... Sweet as." : " ... Bummer dude";
            Console.WriteLine($"Validated {FileCount} Ckan Files. {InValidCount} were \"invalid\" using chosen rules.{Fool}");
            return ret;
        }

        static void Usagesmsg()
        {
            Console.Error.WriteLine("===== Usages: =====");
            Console.Error.WriteLine("CkanLocaliser Validate FileName      [Options] : validate 1 specified ckan File ");
            Console.Error.WriteLine("CkanLocaliser Validate DirectoryName [Options] : validate every specified *.ckan recursively found to reasonable depth and File limit. ");
            Console.Error.WriteLine("CkanLocaliser Validate FileName [Options] : validate 1 speciified ckan File ");
            Console.Error.WriteLine(" ");
            Console.Error.WriteLine("CkanLocaliser Localise AbsPathToMod SrcFileName DstFileName [Options] : validate 1 speciified ckan File ");
            Console.Error.WriteLine(" ");
            Console.Error.WriteLine(" [options] : Currently there are no valid options ");
            Console.Error.WriteLine(" ====== Future abilities ====== ");
            Console.Error.WriteLine(" CkanLocaliser FixSHA AbsPathToMod SrcFileName DstFileName : Fixes SHA1 SHA256 and Download Size. Rest copied verbatum.9even if illegal as a ckan file) ");
            Console.Error.WriteLine(" CkanLocaliser Pattern ConfigFile : Does what the transformation Config file says to do.) ");
            Console.Error.WriteLine(" ============================== ");
        }

 
        /// <summary>
        /// This function validates the specified command line arguments either FileName or DirectoryName 
        /// </summary>
        /// <param name="args"></param>
        static public int Validate(string[] args)
        {

            if (args.Length < 2) { throw new CommandException("Validate what: Insufficient cmdline parameters for a Validation Command."); }
            // test if file or directory exists

            for (int i = 1; i < args.Length; i++)
            {
                string P = args[i];
                if (File.Exists(P))
                {
                    ValidateFile(P);
                }
                else if (Directory.Exists(P))
                {
                    ValidateDirectory(P);
                    RecurseDepthLimit++;
                }
                else
                {
                    throw new CommandException($"Path Does Not Exist: <{P}>");
                }
            }
            return InValidCount;
        }

        static public void ValidateDirectory(string P)
        {
            if (RecurseDepthLimit <= 0)
            {
                return;
            }
            RecurseDepthLimit--;
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(P, "*.ckan");
            foreach (string fileName in fileEntries)
            {
                ValidateFile(fileName);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(P);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ValidateDirectory(subdirectory);
            }
            RecurseDepthLimit++;
    }

    static public void ValidateFile(string P)
        {
            FileCountLimit--;
            FileCount++;
            CkanTokeniser CT = new CkanTokeniser(P);
            CT.AllowSlashN = true;
            TokenFile Foo = new TokenFile(CT);
            Foo.parse();
            CKanFormat bar = new CKanFormat(Foo);
            bar.AllowEOLs = true;

            if (!bar.validation())
            {
                InValidCount++;
                Console.Error.WriteLine($"Error: Parsing Failure ({InValidCount}) in File: <{P}>");
                bar.validation(true);
                pause();
            } 
        }

        static void pause()
        {
            if (DoPause)
            {
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
                Console.WriteLine("Anykey");
                Console.ReadKey();
            }
        }
    }


    public class CommandException : Exception
    {
        public string Cause { get; set; }

        public CommandException(string s) { Cause = s; }
    }

}
