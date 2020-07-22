using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace CkanLocaliser
{
    partial class CkanLocaliserClass
    {
        /// <summary>
        /// rather than have a proper unit test framewokr for now I am doing this 
        /// </summary>
        /// <returns></returns>
        public static int Regression()
        {
            int ret = 0;
            DoAWhiteBoxtest = true; // thuis diable the checks on the zip file we give it.

            string path = "../../testData/"; 
            string srcpath =  "SrcCkanFiles/";
            string outpath = "testOutput/";
            string[] aFnames =  { "FMRSContinued-Actual.ckan", "F3CC22038D887BBFAAB4850BD7152B698688CE2A",
                                  "FMRSContinued-11.ckan", "059682B8D845C2BFD2FAE5A2349750BA22DC7155",
                                  "FMRSContinued-12.ckan", "5C29310698EEBA13338464940E4D9C0B173BB331",
                                  "FMRSContinued-13.ckan", "F6544F70C140104BBC0E23AC593A47114C5DDFCA",
                                  "FMRSContinued-14.ckan", "636ACB032CA7B7A0F19BB8DFA4CCD135D0983502"
            };



            string[] theArgs = { "Localise", "zip", "src", "Dst"    };


            using (StreamWriter hashSW = File.CreateText(path + outpath + "hashes.txt"))
            {

                for (int i= 0; i < aFnames.Length-1; i+=2 )
                {

                    theArgs[0] = "localise";
                    theArgs[1] = "T:/test/File/Location/TheMod.zip";
                    theArgs[2] = path + srcpath + aFnames[i];
                    theArgs[3] = path + outpath + "lcl_" + aFnames[i];

                    CkanLocaliserClass.DoingWhat.Clear();
                    Localise(theArgs);

                    string SHA1 = GetFileHashSha1(theArgs[3]);
                    if (SHA1.Equals(aFnames[i+1]) == false)
                    {
                        Console.Error.WriteLine($"Regression Error: SHA1 has does not match for {theArgs[3]} ");
                        ret = -1;
                    }        
                    hashSW.WriteLine($"{theArgs[3]} SHA1:=> [{SHA1}]");
                }
                hashSW.Close();
            }
            if (ret == 0)
            {
                Console.Error.WriteLine($"Regression tests All passed.");
            }
            return ret;
        }

    }
}
