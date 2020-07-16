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
using System.Runtime.CompilerServices;

namespace CkanLocaliser
{
    partial class CkanLocaliserClass
    {
        static string OldIdentifier = new string("Dummy");
        static string SHA1 =   "";
        static string SHA256 = "";
        static long DownloadSize = 0;

        public static bool DoAWhiteBoxtest = false;

       // static string[] NameFields = { "\"spec_version\"", "\"identifier\"", "\"name\"", "\"author\"", "\"version\"",
       //     "\"ksp_version_min\"", "\"ksp_version_max\"", "\"license\"", "\"provides\"", "\"conflicts\"" };
        /// <summary>
        /// This function validates the localises one specified input ckan File and generates/overwrites one output ckanfile
        /// CkanLocaliser Localise  AbsPathToMod SrcFile.ckan  DestFile.ckan [options]. 
        /// </summary>
        static public int Localise(string[] args)
        {
            CkanLocaliserClass.DoingWhat.Push("Localise");

            // Validate command line Parameters.
            if (args.Length < 4)
            {
                Usagesmsg();
                return -1;
            }
            if (true)
            {
                using (StreamWriter sw = File.CreateText(args[3]))
                {   // This makes sure that if we can to avoid confusion the outfile does in no sense look valid
                    sw.WriteLine(" { Localise error } ");
                    sw.Close();
                }
            }


            if (!args[1].EndsWith(".zip"))
            {
                Console.Error.WriteLine("ModFile MUSTR be a zip File");
                Usagesmsg();
                return -1;
            }
            DownloadPath = args[1];
            if (DoAWhiteBoxtest)
            {   // This allows test code to skip having an actual binary zip file.
                DownloadSize = 9999;
                SHA1 = "F550FBBEF92224DD32E1E0D045BAA8C95EF45343";
                SHA256 = "D51D322B8F68FCDE25FCF4334327C0B86AAE0A65E9B2B70B1B03ECC7761AFD2C";
            }
            else
            {
                FileInfo fi1 = new FileInfo(DownloadPath);
                if (!fi1.Exists)
                {
                    Console.Error.WriteLine("ModFile Must exist");
                    Usagesmsg();
                    return -1;
                }
                if (!Path.IsPathRooted(DownloadPath))
                {
                    throw new FormatException($"Fatal Error: DownLoad path must be an absolute rooted path \"{DownloadPath}\" is not.", true);
                }
                DownloadSize = fi1.Length;
         
                SHA1 = GetFileHashSha1(DownloadPath);
                SHA256 = GetFileHashSha256(DownloadPath);
            }

            FileInfo fi2 = new FileInfo(args[2]);
            if (!fi2.Exists)
            {
                Console.Error.WriteLine("SrcFile Must exist");
                Console.Error.WriteLine(" PWD is :" + Directory.GetCurrentDirectory());
                Usagesmsg();
                return -1;
            }
            // Validate Ckan File

            CkanTokeniser CT = new CkanTokeniser(args[2]);
            // CT.AllowSlashN = true;
            TokenFile Foo = new TokenFile(CT);
            Foo.parse();
            CKanFormat bar = new CKanFormat(Foo);
            bar.AllowEOLs = true;

            if (!bar.validation())
            {
                string DL = "\n====================================\n";
                Console.Error.WriteLine($"\n\n{DL}### Validation Error: Parsing Failure in File: <{args[2]}> \n Context in which That happened {DL}");
                bar.validation(true);
                return -1;
            }
            // do The localisation and write the file
            try
            {   // First tag all three common human readable Fields to identify That we are localised
                CkanLocaliserClass.DoingWhat.Push("RewritingFields");
                parseToValueFor(bar,"\"identifier\"");
                OldIdentifier = bar.Curs.TokenObj.theToken; // Preserve what it was Called We will need that later
                bar.Curs.TokenObj.theToken = "\"" + Prefix + OldIdentifier.Substring(1); // prepend the Localising prefix
                                                                                         //                Console.WriteLine(Curs.TokenObj.theToken);
                parseToValueFor(bar,"\"name\"");
                bar.Curs.TokenObj.theToken = "\"" + Prefix + ":" + bar.Curs.TokenObj.theToken.Substring(1); // prepend the Localising prefix

                parseToValueFor(bar, "\"abstract\"");
                bar.Curs.TokenObj.theToken = "\"" + Prefix + ":" + bar.Curs.TokenObj.theToken.Substring(1); // prepend the Localising prefix

                // Now Replace download
                parseToValueFor(bar, "\"download\"");
                System.Uri DldURI = new System.Uri(DownloadPath); // Convert it to a URI
                bar.Curs.TokenObj.theToken = "\"" + DldURI.AbsoluteUri + "\""; // replace the URI

                // Now Replace download_Size
                parseToValueFor(bar, "\"download_size\"");
                bar.Curs.TokenObj.theToken = DownloadSize.ToString();

                // Now Replace download_Size
                if (bar.hasANameField("\"x_generated_by\"") == true)
                {
                    parseToValueFor(bar, "\"x_generated_by\"");
                    bar.Curs.TokenObj.theToken = "\"CkanLocaliser\"";
                }
                // Now for the not so easy bits  Values in CompoundTypes.
                CkanLocaliserClass.DoingWhat.Pop();
                CkanLocaliserClass.DoingWhat.Push("Wiping_Resoruces_Links");
                killResourcesLinks(bar);

                CkanLocaliserClass.DoingWhat.Pop();
                CkanLocaliserClass.DoingWhat.Push("Fixing_Hashes");
                fixHashValues(bar);
                // Now for the hard bits
                // done backwards through the file.. just to feel safer
                CkanLocaliserClass.DoingWhat.Pop();

                // Insert "Conflicts" preceded by "Provides" just after "license"
                InsertProvidesConflicts(bar);
                // Insert an author either makign the string into an array and/or prepending our Author string

                CkanLocaliserClass.DoingWhat.Push("Adding_Author");

                PieceOfPaper p = new PieceOfPaper();
                p.valueToAdd = Stringify(Author);
                p.listToAddItTo = "\"author\"";
                p.thingToAdditAfter = new List<string>();
                p.thingToAdditAfter.Add("\"abstract\"");
                p.EndProvidesLineNo = 0;
                p.EndProvidesTokenNo = 0;
                fixValueInList(bar, ref p);

                CkanLocaliserClass.DoingWhat.Pop();
            }
            catch (FormatException fe)
            {

                if (fe.KnownReal == false)
                {   // EG: we know it is real if we try to localise ckan file and it has no "identifier" Name:Value pair   etc.
                    Console.WriteLine("CAVEAT: There is reasonably good chance th following is a code error.");
                    Console.WriteLine("CAVEAT: The code has been mainly/entirely checking things we thoguht we just checked already.");
                    Console.WriteLine("CAVEAT: ALL examples of ckan files (schema valid or not) that do this when localised gratefully appreciated.");
                }
                Console.WriteLine($"While Doing : {CkanLocaliserClass.WhatString()} ");
                Console.WriteLine(fe.Cause);

                return -1;
            }
            if (true)
            {
                using (StreamWriter sw = File.CreateText(args[3]))
                {
                    Foo.writeTo(sw);
                    sw.Close();
                }
            }
            CkanLocaliserClass.DoingWhat.Pop();

            return 0;
        }

        static void InsertProvidesConflicts(CKanFormat bar)
        {
            // These will get set to the place the <"conflicts"> <[> <"OldIdentifier"> <]> <EOL> entry should be added.    

            CkanLocaliserClass.DoingWhat.Push("Fixing_Provides");

            PieceOfPaper p1;
            p1.valueToAdd = OldIdentifier;
            p1.listToAddItTo = "\"provides\"";
            p1.thingToAdditAfter = new List<string>();
            p1.thingToAdditAfter.Add("\"license\"");
            p1.EndProvidesLineNo = 0;
            p1.EndProvidesTokenNo = 0;
            p1.IndentationHack = "";
            fixValueInList(bar, ref p1);

            CkanLocaliserClass.DoingWhat.Pop();
            CkanLocaliserClass.DoingWhat.Push("Fixing_Conflicts");

            // The memory image of the File now has a provides statement that we put there or one we amended or checked...
            // BUT irrespective our cursor is at the last token of a Line object that follows 
            // "provides" <:> [<[>] <"OldIdentifier"> [<,> <otherValue> <]>] >here< <EOL>
            // and hence we are ready to add the Conflicts value right here... unless there is one somewhere else....
            // And conflicts is worse it is an Array of Compound named values...  YAY hooray.
            if (bar.hasANameField("\"conflicts\"") == true)
            {  // Bugger...
               // There is an existing conflicts we will check if an exact copy of what we want is already there
               //   bool hasExactConflictsEntry = false;
                CkanLocaliserClass.DoingWhat.Push("Existing_Conflicts");

                PieceOfPaper p2;
                p2.valueToAdd = "Foobar1";
                p2.listToAddItTo = "\"conflicts\"";
                p2.thingToAdditAfter = new List<string>();
                p2.thingToAdditAfter.Add("\"FooBar2\"");
                p2.EndProvidesLineNo = 0;
                p2.EndProvidesTokenNo = 0;
                p2.IndentationHack = "";
                parseToValueFor(bar, ref p2); // We are now here <"conflicts"> <:> >here< <[>
// TODO is "conflicts" : { "name" : "ModID" }, legal without the []  do we care?
// validate various egs against schema using tools.
                p2.IndentationHack = p2.IndentationHack + "    "; // yep oops I just did that.... again but first?
                bar.expectToken("[", TokenCategory.Token);
                // Remember where this is
                int oldConflictStartLineNo = bar.Curs.LineNo;
                int oldConflictStartTokenNo = bar.Curs.TokNo;
                bool FileHasAGoodConflistsEntry = false;
                // Now follows asequence of <{> ... <}> [ <,> <{> ... <}> ]  blocks terminated by <]>
                // Check if any are exactly what we are about to add.
                while (true)
                {
                    bar.skipEOL(); // skips all EOLS and WS
                    if (bar.isTokenTextUse("]"))
                    {
                        // bar.expectToken("]", TokenCategory.Token);
                        // We are here <"conflicts"> <:> <[>  <]> >here< 
                        // or  possibly if <"conflicts"> <:> <[> ... <,>  <]> >here< if it passed validation phase
                        // we are NOT <"conflicts"> <:> <[> <name> <> <:> <value> [ <,> <name> <> <:> <value>]  <]> >here<
                        // the array list is either empty [] or comma  [ ... , ] terminated ...
                        // See ###ZZZ2 below
                        break;
                    }
                    // is it the end of the list

                    // rmember any indentation cues that we see.    
                    // override the +4 space decison above and mirror any other provides?
                    if (p2.IndentationHack.Length < bar.Curs.TokenObj.WhiteSpace.Length)
                    {   // But only if it is more indented than the earlier decision
                        // AKA we willindent 4 more spaces than provides and ignore the others...
                        // >>    "provides":[<EOL><< 
                        // >> "Thing1Provided", "Thing2Provided" <EOL> << 
                        p2.IndentationHack = bar.Curs.TokenObj.WhiteSpace;
                    }

                    // is it the one we are after?
                    bool MatchesEntry = true; //so far it does

                    if (bar.isTokenTextUse("{"))
                    {  // comma sep list of <name> <:> <value> pairs termianted by <}> 
                        MatchesEntry = true; //set it to true and see it this fits the pattern
                        bar.skipEOL(); // skips all EOLS and WS
                        while (bar.isTokenTextUse("}") == false)
                        {  // Wasnt a <}> didnt use it.
                            // So it ought to be  <name>
                            if ( bar.Curs.TokenObj.theToken.Equals("\"name\"") == false) 
                            {
                                MatchesEntry = false;
                            }
                            // We are here  <conflicts> : <[>  <{> [...] >here<  <name> <:> <value> [...] <}>
                            bar.expectToken(TokenCategory.String);  // No EOLs
                            bar.expectToken(":",TokenCategory.Token); // No EOLs
                            if (bar.Curs.TokenObj.theToken.Equals(OldIdentifier) == false)
                            {
                                MatchesEntry = false;
                            }
                            bar.expectToken(TokenCategory.String);
                            bar.skipEOL(); // skips all EOLS and WS
                            // ending a list with <,> is apprently OK ish?
                            // whatever if our validation step didnt balk we accept it here.

                            // We are here  <conflicts> : <[>  <{> [...] <name> <:> <value> >here< <,> [...] <}>
                            // or are here  <conflicts> : <[>  <{> [...] <name> <:> <value> >here< <}>
                            // or are here  <conflicts> : <[>  <{> [...] <name> <:> <value> >here< <,> <}>
                            bar.isTokenTextUse(","); // Eat the <,> iff its there...

                            // GROK AKA yes I know, 
                            // Note: this code here permits  >> ... conflicts [ { "name" : "bar" "version" : "1.1.1." } ] ... <<
                            // which ought to have a missing comme eror but the validator will have flagegd that. Its not our job to do that again.
                            // trying to undertsand the ckan file with this code is enough.
                            bar.skipEOL(); // skips all EOLS and WS

                        }
                        // We are here  <conflicts> : <[>  <{> [...] <}> >here<  [<,>] [<{> [...] <}>]
                        if (MatchesEntry == true)
                        {  // There just right now was  { "name" : <OldIdentifier> } 
                           // why that might exist in this file i have no idea but it does so were done 
                            FileHasAGoodConflistsEntry = true; // just to be clear even if somethign elss would work.
                            break;
                        }
                    }
                    bar.skipEOL(); // skips all EOLS and WS
                    if (bar.isTokenTextUse("]"))
                    {
                        break;
                    }
                    bar.skipEOL(); // skips all EOLS and WS
                    bar.isTokenTextUse(","); // Eat the <,> iff its there...
                    // GROK AKA yes I still know, 

                    bar.skipEOL(); // skips all EOLS and WS
                }
                // Either we found an existing conflictstatement or we want to add one.
                if (FileHasAGoodConflistsEntry == false)
                {
                    bar.Curs.setPosition(oldConflictStartLineNo, oldConflictStartTokenNo);
                    CheckForEOLorThrow(bar);
                    // Ok good to go same deal as the others:  We can append our insertion att he EOL here  <conflicts> : <[> >here< <EOL>
                    TokenFile.Line L = bar.Curs.Line;
                    L.TheLine.Add(new TokenFile.TokenObject(p2.IndentationHack , "{", TokenCategory.Token, (int)'{'));
                    L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                    L.TheLine.Add(new TokenFile.TokenObject(p2.IndentationHack + "    ", "\"name\"", TokenCategory.String, (int)'"'));
                    L.TheLine.Add(new TokenFile.TokenObject("", ":", TokenCategory.Token, (int)':'));
                    L.TheLine.Add(new TokenFile.TokenObject(" ", OldIdentifier, TokenCategory.String, (int)'"'));
                    L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                    L.TheLine.Add(new TokenFile.TokenObject(p2.IndentationHack , "}", TokenCategory.Token, (int)'}'));
                    L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                    L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                }
                CkanLocaliserClass.DoingWhat.Pop();
            }
            else
            {  // Deal with the case where we just append a new Conflicts statement
                CkanLocaliserClass.DoingWhat.Push("Adding_Conflicts");
                TokenFile.Line L = bar.Curs.Line;
                L.TheLine.Add(new TokenFile.TokenObject(p1.IndentationHack, "\"conflicts\"", TokenCategory.String, (int)'"'));
                L.TheLine.Add(new TokenFile.TokenObject("", ":", TokenCategory.Token, (int)':'));
                L.TheLine.Add(new TokenFile.TokenObject(" ", "[", TokenCategory.Token, (int)'['));
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                L.TheLine.Add(new TokenFile.TokenObject(p1.IndentationHack + "    ", "{", TokenCategory.Token, (int)'{'));
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                L.TheLine.Add(new TokenFile.TokenObject(p1.IndentationHack + "        ", "\"name\"", TokenCategory.String, (int)'"'));
                L.TheLine.Add(new TokenFile.TokenObject("", ":", TokenCategory.Token, (int)':'));
                L.TheLine.Add(new TokenFile.TokenObject(" ", OldIdentifier, TokenCategory.String, (int)'"'));
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                L.TheLine.Add(new TokenFile.TokenObject(p1.IndentationHack + "    ", "}", TokenCategory.Token, (int)'}'));
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                L.TheLine.Add(new TokenFile.TokenObject(p1.IndentationHack, "]", TokenCategory.Token, (int)']'));
                L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                // reuse whatever whites space it is that follows the cursor: see CheckForEOLorThrow above
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                CkanLocaliserClass.DoingWhat.Pop();
            }
            CkanLocaliserClass.DoingWhat.Pop();
        }


        struct PieceOfPaper
        {
            public string valueToAdd;
            public string listToAddItTo;
            public List<string> thingToAdditAfter;
            public int EndProvidesLineNo;  // for now they point somewhere obviously wrong
            public int EndProvidesTokenNo;
            public string IndentationHack;

        }

        static string Stringify(string S)
        {
            String S1 = "", S2="";
            if (S.StartsWith('\"') == false)
            {
                S1 = "\"";
            }
            if (S.EndsWith('\"') == false)
            {
                S2 = "\"";
            }
            S.Replace('\"', '~');
            return (S1 + S + S2);
        }

        /// <summary>
        /// WARNING: Be aware this code leaves things (Paper && Curs) in a state relied on by InsertProvidesConflicts
        /// See comments there before messing with this. 
        /// But genrally speaking this function :
        /// Either adds one value to a Name : [ ... ] list 
        /// Or creates the list 
        /// Or fxies a Name : Value into and array as required.
        /// </summary>
        /// <param name="bar"> object holding Format and File</param>
        /// <param name="p"> Literally a Place where things are writtendown for use elsewhere... scratchpad</param>
        static void fixValueInList(CKanFormat bar, ref PieceOfPaper p)
        {
            if (bar.hasANameField(p.listToAddItTo) == true)
            { // we already have a provides entry
              // Check AND Add that we provide OldIdentifier
                bool hasOldIdentifierProvided = false;

                parseToValueFor(bar, ref p); // We are now here <"provides"> <:> >here< <[>  (where provides might also be author)
                                             // or worse here ...  <"author"> <:> >here< <"authors_handle"> <,>


                if (bar.isTokenTextUse("[") == false)
                { // yep its worse here ...  <"author"> <:> >here< <"authors_handle"> <,> 
                  // there is no EOL trick we can use.      
                    string ExistingAuthor = bar.Curs.TokenObj.theToken;
                    if (ExistingAuthor.Equals(p.valueToAdd) == false)
                    {  // The one item in the list is not the thign we wer meant to add.
                        //First valid date to expliclty double check. Whats been implied bythis beign a Valid File and thsi NOT beigna <[>.
                        TokenFile.Line L = bar.Curs.Line;
                        int len = L.TheLine.Count;
                        string t = "Huh?"; // if it prints huh then this line in the file is an very unexpected length. Sorry: lesson dont use ugly ckan files.
                        // This may be valid line in a ckan file <SOL>"author":"Axle","version":"1.2.9.1"<EOL> but youare SOL(not the star) if you want localiser to localise it.
                        if ((len != bar.Curs.TokNo + 3) || ((t=L.TheLine[L.TheLine.Count-2].theToken).Equals(",") == false))
                        {  // Yeah nah that kinda cant happen.  and if it does I dont care... make your file prettier.
                           // TODO if the <,> is on another should we keep it and insert here?
                           throw new FormatException($"Fatal Format Error in {bar.TokFile.FilePath}:\n\t Expected <\"author\": \"Authorname\" , \"> got <\"author\": \"Authorname\" {t} >.");
                        }
                        // Okay so the file is still just like we already validated it to be ... a legal(ish) Json File...
                        // delete the Bits we cant keep.  >here< <"ExistingAuthor"> <,> <EOL>
                        L.TheLine.RemoveAt(bar.Curs.TokNo);
                        // delete the Bits we cant keep.  >here< <,> <EOL> 
                        bar.Curs.TokenObj.theToken = "[";  // reuse the , token as a [
                        // bar.Curs.TokenObj.TokenCategory = TokenCategory.Token; // already verified as true as it was a ","
                        // delete the Bits we cant keep.  >here< <[> <EOL> 
                        // Not strictly required but this code is now more like most of the otehr edit code  see reuse the white space code for why thats good diea.
                        bar.expectToken("[", TokenCategory.Token);
                        // we are here <[> >here< <EOL> 
                        L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack + "    ", p.valueToAdd, TokenCategory.String, (int)'"'));
                        L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                        L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                        // we are here <"author"> <:> <[> >here< <EOL> <Author> <,> <EOL>  
                        L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack + "    ", ExistingAuthor, TokenCategory.String, (int)'"'));
                        // nope no comma here L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                        L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                        // we are here <"author"> <:> <[> >here< <EOL> <Author> <,> <EOL> <ExistingAuthor> <,> <EOL> 
                        L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack, "]", TokenCategory.Token, (int)']'));
                        // This is now the lexcial equivalent of the <,>  token we deleted above    
                        L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                        L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                            // we are here <"author"> <:> <[> >here< <EOL> <Author> <,> <EOL> <ExistingAuthor> <EOL> <]> <,> <EOL> 
                    } 
                    else
                    {
                        bar.expectToken(p.valueToAdd, TokenCategory.String);
                        bar.expectToken(",", TokenCategory.Token);
                        CheckForEOLorThrow(bar);
                    }
                }
                else
                {
                    p.IndentationHack = p.IndentationHack + "    "; // yep I just did that....


                    // already used bar.expectToken("[", TokenCategory.Token);
                    // Remember where this is
                    int oldLineNo = bar.Curs.LineNo;
                    int oldTokenNo = bar.Curs.TokNo;
                    int NumOfIdentifiers = 0;
                    //We are here <"provides"> <:> <[> here <EOL>
                    bar.expectToken(TokenCategory.tokEOL); // We require one of these here. We fail if its missing.
                                                           // AKA this file is, for us, NOT valid   >>   "provides" : [ ],<EOL><<  as we require an EOL after the <[>
                    bar.skipEOL(); // skips all EOLS and WS // be generous ?
                    while (true)
                    {
                        if (bar.isTokenTextUse("]"))
                        {
                            // We are here <"provides"> <:> <[>  <]> >here< 
                            // or  possibly if <"provides"> <:> <[> ... <,>  <]> >here< if it passed validation phase
                            // we are NOT <"provides"> <:> <[> <name> <> <:> <value> [ <,> <name> <> <:> <value>]  <]> >here<
                            // the array list is either empty [] or comma  [ ... , ] terminated ...
                            // See ###ZZZ1 below
                            break;
                        }
                        // is it the end of the list
                        // rmember any indentation cues that we see.    
                        // override the +4 space decison above and mirror any other provides?
                        if (p.IndentationHack.Length < bar.Curs.TokenObj.WhiteSpace.Length)
                        {   // But only if it is more indented than the earlier decision
                            // AKA we willindent 4 more spaces than provides and ignore the others...
                            // >>    "provides":[<EOL><< 
                            // >> "Thing1Provided", "Thing2Provided" <EOL> << 
                            p.IndentationHack = bar.Curs.TokenObj.WhiteSpace;
                        }

                        // is it the one we are after?
                        if (bar.Curs.TokenObj.theToken.Equals(p.valueToAdd))
                        {
                            //We are here <"provides"> <:> <[> ... >here< <OldIdentifier> ... <]>
                            hasOldIdentifierProvided = true;
                        }
                        // Nope it some other thing that the mod  "provides"

                        bar.expectToken(TokenCategory.String); // skip it eitehr way
                        NumOfIdentifiers++; //count them
                        bar.skipEOL(); // skips all EOLS and WS
                        if (bar.Curs.TokenObj.theToken.Equals(",") == false)
                        {   // So now we add our value for provides
                            // Failing this epctationThis probably should not have passed valdiation so it cant happen?
                            bar.expectToken("]", TokenCategory.Token);
                            bar.skipEOL(); // skips all EOLS and WS
                            bar.expectToken(",", TokenCategory.Token);
                            CheckForEOLorThrow(bar);
                            // see ###ZZZ1 above
                            // We are here <"provides"> <:> <[> ... <]> >here< <EOL>
                            // Non empty arrays (probably hopefully all the >valid< real one in the wild.) exit here
                            break;
                        }
                        bar.expectToken(",", TokenCategory.Token);
                        // there is (or better be as we were just promsied one). more provides values ...
                        // We are here <"provides"> <:> <[> ... <value> <,> >here< <value> 
                        bar.skipEOL(); // skips all EOLS and WS
                    }
                    p.EndProvidesLineNo = bar.Curs.LineNo;
                    p.EndProvidesTokenNo = bar.Curs.TokNo;
                    // we check for the EOL here a bit later.

                    if (hasOldIdentifierProvided == false)
                    {  // So now we go back and insert OldIdentifier at the front of the list/array of values
                        bar.Curs.setPosition(oldLineNo, oldTokenNo);
                        // as previously established.
                        // Cursor is at an EOL *and* it is the last token on a Line in File.
                        // insert our extra provides value here
                        /////////////////////////////////////////////////////////////////////
                        //   WARNING DANGER DANGER WILL ROBINSON. 
                        /////////////////////////////////////////////////////////////////////
                        // After this operation the LINE object will have multiple output lines that it prints due to an <EOL> in the middle
                        // That is never true just after reading a file, and unless we edit it like this, never at all.
                        /////////////////////////////////////////////////////////////////////    
                        // We are here <"provides"> <:> <[> ... <]> >here< <EOL> 
                        // Which is cool so if we append our Stuff to the line object and end in an EOL too then its been inserted in the file 
                        //  **AND**
                        // Every LineNo TokenNo pair that exists still points to the exact same stuff/token... as before the edit/insert

                        // add this <"provides"> <:> <[> >here< <EOL> <wsIndetation/OldIdentifier> [<,>] <EOL>
                        // where the optional <,> is added only if there is  follwing identifier in the array.
                        CheckForEOLorThrow(bar); // out of an abundance of caution. Check again.
                        TokenFile.Line L = bar.Curs.Line;
                        L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack, p.valueToAdd, TokenCategory.String, (int)'"'));
                        if (NumOfIdentifiers > 0)
                        {
                            //  it was NOT <"provides"> <:> <[> <]> it did have at ldast one idetifier, so we need a comma 
                            L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                        }
                        // reuse whatever whites space it is that follows the cursor: see CheckForEOLorThrow above
                        L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                        // Line Object Now looks like this <"provides"> <:> <[> >here< <EOL> <wsIndetation/OldIdentifier> [<,>] <EOL>
                        // The comma is present if and only if there is another preexisting identifier in the Provides list
                    }
                    bar.Curs.setPosition(p.EndProvidesLineNo, p.EndProvidesTokenNo);
                    // File Now looks like this <"provides"> <:> <[> <EOL> <wsIndetation/OldIdentifier> [<,>] <EOL> ... <]> >here< <EOL>  
                }
            }  // well that was fun 
            else
            {  // Now for the easy more usual alternative There no existing provides name value insert one after license.
                // or insert author after abstract if we doing author
                if (bar.hasANameField(p.thingToAdditAfter[0]) == false)
                {
                    //Force an error to throw
                    bar.expectToken(p.thingToAdditAfter[0], TokenCategory.Token);
                    throw new FormatException("Programmer Error: This really cant (logically) happen.");
                }

                // This is a required by the schema field... so it better be there or who cares?
                // find where that nameField is move to the { that is just after it and the ":".
                string t = p.listToAddItTo;
                p.listToAddItTo = p.thingToAdditAfter[0];
                parseToValueFor(bar, ref p); // find that name:value instead
                p.listToAddItTo = t;
                p.IndentationHack = p.IndentationHack + ""; // yep, oops I just did that.... again
                // like <"author"> The schema specifies either a single value or an array for license (humans: see licenses at bottom of schema)
                // 
                if (bar.isTokenTextUse("["))
                {
                    // already used bar.expectToken("[", TokenCategory.Token);
                    while (bar.Curs.TokenObj.theToken.Equals("]") == false)
                    {   // Accept anythign we find until we get one ]....
                        bar.expectToken(bar.Curs.TokenObj.theToken, bar.Curs.TokenObj.TokenCategory);
                    }
                    bar.expectToken("]", TokenCategory.Token);

                }
                else
                { // if is not a <[> it needs to be a single license string (or a single string abstract)
                    bar.expectToken(TokenCategory.String);
                }
                // Now we require a <,> after that name:value pair followed by an <EOL>
                bar.skipEOL();
                bar.expectToken(",", TokenCategory.Token);
                CheckForEOLorThrow(bar);
                // We are here <"license"> <:> {one_of String or Array} >here< <EOL>   (or equiv but for <"abstract">)
                // Now we add  <IndentationHack:"provides"> <:> <OldIdentifier> <,> <EOL>
                TokenFile.Line L = bar.Curs.Line;
                L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack, p.listToAddItTo, TokenCategory.String, (int)'"'));
                L.TheLine.Add(new TokenFile.TokenObject("", ":", TokenCategory.Token, (int)':'));
                L.TheLine.Add(new TokenFile.TokenObject(" ", "[", TokenCategory.Token, (int)'['));
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack + "    ", p.valueToAdd, TokenCategory.String, (int)'"'));
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));
                L.TheLine.Add(new TokenFile.TokenObject(p.IndentationHack, "]", TokenCategory.Token, (int)']'));
                L.TheLine.Add(new TokenFile.TokenObject("", ",", TokenCategory.Token, (int)','));
                // reuse whatever whites space it is that follows the cursor: see CheckForEOLorThrow above
                L.TheLine.Add(new TokenFile.TokenObject(bar.Curs.TokenObj.WhiteSpace, "", TokenCategory.tokEOL, (int)'\n'));

                /////////////////////////////////////////////////////////////////////
                //   WARNING DANGER DANGER WILL ROBINSON. 
                //   read carefully.
                /////////////////////////////////////////////////////////////////////
                p.EndProvidesLineNo = bar.Curs.LineNo;
                p.EndProvidesTokenNo = L.TheLine.Count - 1; // Yep as we just added the EOL after "provides" we know right where we put it.
                bar.Curs.setPosition(p.EndProvidesLineNo, p.EndProvidesTokenNo);
            }

        }

        /// <summary>
        /// We should consider also checking thatthe EOL is in fact at the end of the Line Object it is on.
        /// It is but, we could check, as we require it to be true.
        /// </summary>
        /// <param name="bar"></param>
        static void CheckForEOLorThrow(CKanFormat bar)
        {
            if (bar.Curs.TokenObj.TokenCategory != TokenCategory.tokEOL)
            {
                bar.AllowEOLs = false;
                bar.expectToken(TokenCategory.tokEOL);
                throw new FormatException("Programmer Error: This really cant (logically) happen.");
            }
            return;
        }


        static void killResourcesLinks(CKanFormat bar)
        {
            if (bar.hasANameField("\"resources\"") == false )
            {
                return;
            }
            // find where that nameField is move to the { that is just after it and the ":".
            parseToValueFor(bar, "\"resources\"");
            // That should leave us with the cursor on  a {
            bar.expectToken("{", TokenCategory.Token); //advance over the ":"  NO EOLS allowed

            while (bar.Curs.TokenObj.theToken.Equals("}") == false )
            {   // For every resource field remove them all.
                bar.skipEOL(); // skips all EOLS and WS
                bar.expectToken(bar.Curs.TokenObj.theToken, TokenCategory.String); //advance over the ":"  NO EOLS allowed
                bar.expectToken(":", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
                // We are now pointed as are resource field of some name we dont care what ... blow it up
                bar.Curs.TokenObj.theToken = "\"https://youtu.be/ub82Xb1C8os\"";
                bar.expectToken(bar.Curs.TokenObj.theToken, TokenCategory.String);
                if (bar.Curs.TokenObj.theToken.Equals(",") ) {
                    bar.expectToken(",", TokenCategory.Token);
                }
                bar.skipEOL(); // skips all EOLS and WS
            }
            // we could do this but there is no need or purpose.
            // bar.expectToken("}", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
        }



        static void fixHashValues(CKanFormat bar)
        {
            // Now Replace download_Hash and its  SHA1 and SHA256
            parseToValueFor(bar, "\"download_hash\"");
            // That should leave us with the cursor on  a {
            bar.expectToken("{", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
            bar.skipEOL(); // skips all EOLS and WS
            bool doSha1 = true;
            bool doSha256 = true;
            String S;
            do
            {
                if (doSha1 & bar.Curs.TokenObj.theToken.Equals("\"sha1\""))
                {
                    doSha1 = false;
                    bar.expectToken("\"sha1\"", TokenCategory.String); //advance over the ":"  NO EOLS allowed
                    bar.expectToken(":", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
                    S = "\"" + SHA1 + "\"";
                    bar.Curs.TokenObj.theToken = S;
                    bar.expectToken(S, TokenCategory.String);  // yes we expect the value we just set to be there.
                } else if (doSha256 & bar.Curs.TokenObj.theToken.Equals("\"sha256\"")) {
                    doSha256 = false;
                    bar.expectToken("\"sha256\"", TokenCategory.String); //advance over the ":"  NO EOLS allowed
                    bar.expectToken(":", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
                    S = "\"" + SHA256 + "\"";
                    bar.Curs.TokenObj.theToken = S;
                    bar.expectToken(S, TokenCategory.String);  // yes we expect the value we just set to be there.
                }
                else
                {  //We reached named value pair that is not Sha1 or Sha256... ignore it?
                    bar.expectToken(TokenCategory.String); // eat it
                    bar.expectToken(":", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
                    bar.expectToken(bar.Curs.TokenObj.TokenCategory); // eat the value whatever string number whatever it is.
                }

                if (bar.Curs.TokenObj.theToken.Equals(","))
                {
                    bar.expectToken(",", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
                    bar.skipEOL(); // skips all EOLS and WS
                } else
                {
                    bar.skipEOL(); // skips all EOLS and WS
                    break;
                }
            } while (true);
            // We reached the first time we got <Name> <:> <Value> folowoed by somethginthat was not <,>
            // it had better be the }
            // We require pretty strict EOL placement around here. "Sha1" : "HEX", EOL or "Sha256" : "HEX", EOL 
            bar.expectToken("}", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
        }




        /// <summary>
        /// Search for the Level 1 NameField matching namefield, then 
        /// walk forward to the token holding is value or 
        /// opening [ or opening { right after the :
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="nameField"></param>
        static void parseToValueFor(CKanFormat bar, ref PieceOfPaper p)
        {
            TokenFile.Cursor Curs = bar.MoveCursTo(p.listToAddItTo);
            //WARNING This is awful, but I did it anyways....
            // This as side effect sets the global variable IndentationGloabalHack to the whitespace preceding nameField
            // just in case someone needs it later read allthe code everywher to see how this works or doesnt.
            p.IndentationHack = Curs.TokenObj.WhiteSpace;
            Curs.advance(); // advance over the just found identifier Token
            // does not accept EOL between name and ":"
            bar.expectToken(":", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
            bar.skipEOL(); // skips all EOLS and WS
            return; // Cursor points to Value String OR [ OR { dependign on Namevalue Type
        }

        static void parseToValueFor(CKanFormat bar, string S)
        {
            TokenFile.Cursor Curs = bar.MoveCursTo(S);
            //WARNING This is awful, but I did it anyways....
            // This as side effect sets the global variable IndentationGloabalHack to the whitespace preceding nameField
            // just in case someone needs it later read allthe code everywher to see how this works or doesnt.
            // p.IndentationHack = Curs.TokenObj.WhiteSpace;
            Curs.advance(); // advance over the just found identifier Token
            // does not accept EOL between name and ":"
            bar.expectToken(":", TokenCategory.Token); //advance over the ":"  NO EOLS allowed
            bar.skipEOL(); // skips all EOLS and WS
            return; // Cursor points to Value String OR [ OR { dependign on Namevalue Type
        }



        ////////////////////////////////////////////////////////////////////////////////////////
        /// These fucntions derived from and are required to be 100% identifcal in efefct to the equivalent ckanCode.
        /// Minus all the fluff about caching


        /// <summary>
        /// Generate the hash used for caching. We probaly dont need this one. 
        /// </summary>
        /// <param name="url">URL to hash</param>
        /// <returns>
        /// Returns the 8-byte sha1 hash of the utf8 text representation of a given url
        /// </returns>
        public static string CreateURLHash(Uri url)
        {
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(url.ToString()));

                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
            }
        }

        /// <summary>
        /// Always freshly Calculate the SHA1 hash of a file's contents
        /// </summary>
        /// <param name="filePath">Path to file to examine</param>
        /// <returns>
        /// SHA1 hash, in all-caps hexadecimal format
        /// </returns>
        public static string GetFileHashSha1(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BufferedStream bs = new BufferedStream(fs))
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(sha1.ComputeHash(bs)).Replace("-", "");
            }
        }




        /// <summary>
        /// Always freshly Calculate the SHA256 hash of a file's contents
        /// </summary>
        /// <param name="filePath">Path to file to examine</param>
        /// <returns>
        /// SHA256 hash, in all-caps hexadecimal format
        /// </returns>
        public static string GetFileHashSha256(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BufferedStream bs = new BufferedStream(fs))
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                return BitConverter.ToString(sha256.ComputeHash(bs)).Replace("-", "");
            }
        }


    }
}
