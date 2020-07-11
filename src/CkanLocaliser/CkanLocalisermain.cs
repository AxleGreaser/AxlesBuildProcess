using System;
using System.IO;

namespace CkanLocaliser
{
    class CkanLocaliserClass
    {
        static int FileCount = 0;
        static int InValidCount = 0;
        static bool DoPause = false;

        static string Appname = new string("");
        static string pwd = new string("");
        static int Main(string[] args)
        {
            try
            {
                Appname = Environment.GetCommandLineArgs()[0];
                //                pwd = Environment.CurrentDirectory;
                //                Console.WriteLine("Hello World!");
                //                Console.WriteLine($"From <{Appname}> In <{pwd}>");

                if (args.Length < 1)
                {
                    Console.WriteLine("I have nothign to do or live for (argv[0] == null); Goodbye Cruel World!");
                }
                if (args[0].Equals("Validate"))
                {
                    Validate(args);
                }
                else
                {
                    Usagesmsg();
                    return - 1;
                }
            }
            catch (CommandException ce)
            {
                Console.WriteLine(ce.Cause);
                Usagesmsg();
                return -1;
            }
            string Fool = (FileCount<100) ? "" : (InValidCount == 0) ? " ... Sweet as." : " ... Bummer dude";
            Console.WriteLine($"Validated {FileCount} Ckan Files. {InValidCount} were \"invalid\" using chosen rules.{Fool}");
            return 0;
        }

        static void Usagesmsg()
        {
            Console.WriteLine("Usages: TODO ");
        }


        static public void Validate(string[] args)
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
                }
                else
                {
                    throw new CommandException($"Path Does Not Exist: <{P}>");
                }
            }
        }

        static public void ValidateDirectory(string P)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(P, "*.ckan");
            foreach (string fileName in fileEntries)
            {
 //               Console.Write($"ValFile {fileName}");
                ValidateFile(fileName);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(P);
            foreach (string subdirectory in subdirectoryEntries)
            {
 //               Console.Write($"Dir {subdirectory}");
                ValidateDirectory(subdirectory);
            }
        }

        static public void ValidateFile(string P)
        {
            FileCount++;
            //        Console.Write($"File {P}");
            CkanTokeniser CT = new CkanTokeniser(P);
            CT.AllowSlashN = true;
            TokenFile Foo = new TokenFile(CT);
            Foo.parse();
            CKanFormat bar = new CKanFormat(Foo);
            bar.AllowEOLs = true;

            if (!bar.validation())
            {
                InValidCount++;
                Console.WriteLine($"Error: Parsing Failure ({InValidCount}) in File: <{P}>");
                bar.validation(true);
                pause();
            } else
            {
    //            Console.WriteLine(" ok");
            }
            // Foo.writeTo(Console.Out);
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
