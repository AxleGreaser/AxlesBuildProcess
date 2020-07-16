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
            string[] aFnames =  { "FMRSContinued-Actual.ckan", "933369F49440B3CA75DD40CFAD3B93CDCC6F7ED6",
                                  "FMRSContinued-11.ckan", "B658CAB284E1736A08A75ECEFD4AE8D4A6C80FBD",
                                  "FMRSContinued-12.ckan", "1EF52DDAC670BE07241F78D8F9C31C0B16F74AA4",
                                  "FMRSContinued-13.ckan", "C5147DB1A2F4C7D00587867F789B986F5BE7A5EB",
                                  "FMRSContinued-14.ckan", "B43041158049206C703A493434EE2BADBABD32AF" 
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
