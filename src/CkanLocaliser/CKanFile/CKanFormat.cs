using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace CkanLocaliser
{
    /// <summary>
    ///  This class encapuslates the higher level knowledge about what CKan 
    ///  a file format is in terms of what sequence its tokens are in.
    ///  it also implents functions to perform various transformations
    /// </summary>
    class CKanFormat
    {
        public bool AllowEOLs { get; set; } = false;
        public int EOLMissedCount { get; set; } = 0;
        public int MaxMissedCount { get; set; } = 14; // yeah some files is the repo are somewhat badly formatted by hand
        public int EOLExtraCount { get; set; } = 0;
        public int MaxExtraCount { get; set; } = 4;

        List<NameValueData> Required;

        TokenFile TokFile { get; set;  }

        bool FileIsValid = true;

        TokenFile.Cursor Curs = null;

        int Level = 0; // 0 is outside first {



        public CKanFormat(TokenFile t)
        {
            TokFile = t;

            string[] RequiredNames = new[] { "spec_version", "identifier", "name", "abstract" };
            string[] NotRequiredNames = new[] { "version", "conflicts" };
        }

        /// <summary>
        /// This function does not fully validate the file against a Ckan schema.
        /// What it foes do is validate the format enough to be sure the file 
        /// looks like std format Ckan file such as netkan produces.
        /// </summary> 
        /// <remarks>
        /// There exist other json comaptible files (with strange 100% optional 
        /// white space structures) that would validate as correct versus 
        /// the json schemea that this code rejects, as it is too visually 
        /// different and this code thus this code 'decides' that a human probably wont 
        /// have properly validated the input file.
        /// </remarks>
        /// <returns>returns true if the file is sufficiently valid for our purposes.</returns>
        public bool validation(bool echo=false)
        {
            // reset parser state.    
            FileIsValid = true;
            Level = 0;
            AllowEOLs = true;
            EOLMissedCount = 0;
            EOLExtraCount = 0;

            // grammar: 
            // <CkanFile>    := <CompoundValue> <EOF>   
            Curs = TokFile.getCursor(echo);
            try
            {
                parseCompound();

                // grammar:
                // <EOF> := <EOL> >spaces tabs etc< / physical end of file.
                // <EOF> := >spaces tabs etc< / physical end of file.  
                if (Curs.TokenObj.isCategory(TokenCategory.tokEOL))
                {   // alow 1 optional EOL then expect the EOF.
                    expectToken(TokenCategory.tokEOL);
                }
                expectToken(TokenCategory.tokEOF); // may have leading spaces.
            } catch (FormatException fe)
            {
                Console.WriteLine(fe.Cause);
                return false;
            }
            return true;
        }

        bool parseCompound()
        {   // grammar:
            //<CompoundValue> := "{" <EOL> <ListNV> <EOL> "}"
            if (AllowEOLs && EOLExtraCount < MaxExtraCount && isToken(TokenCategory.tokEOL))
            {
                EOLExtraCount = EOLExtraCount + 1;
                expectToken(TokenCategory.tokEOL);
            }
            expectToken("{", TokenCategory.Token);  // This one is known by the caller to be "{" but for simplcity/encapsualtion we check.
            expectToken(TokenCategory.tokEOL);
            parseListNV();
            expectToken(TokenCategory.tokEOL);
            expectToken("}", TokenCategory.Token);
            return true;
        }

        bool parseArray()
        {   // grammar:
            // <ArrayValue>    := "[" <EOL> <ValueList> <EOL> "]"
            if (AllowEOLs && EOLExtraCount < MaxExtraCount && isToken(TokenCategory.tokEOL) ) 
            {
                EOLExtraCount = EOLExtraCount + 1;
                expectToken(TokenCategory.tokEOL); 
            }
            expectToken("[", TokenCategory.Token); // This one is known by the caller to be "{" but for simplcity/encapsualtion we check.
            expectToken(TokenCategory.tokEOL);
            parseListVals();
            expectToken(TokenCategory.tokEOL);
            expectToken("]", TokenCategory.Token);
            return true;
        }

        bool parseListNV()
        {   // grammar:
            // NOPE Currently not this <ListNV>        := <Empty>
            // <ListNV>        := <NameValue>  <MoreListNV>
            // <MoreListNV>    := <Empty>
            // <MoreListNV>    := "," <EOL> <NameValue> <MoreListNV>
            // WARNING: Ckan accepts trailing "," without a Value AKA this is legal...  >>[ "value1", ]<<We dont becuase yuk.
            // meaning only two token types are viable NameString Starting a NameValue or
            // "}" terminating compund  But we let the caller do the check. 

            bool EmptyNVisOk = false; //chnage to true to allow empty NV lists AKA .. { <Empty> } ..
            // Check For First NV
            while (!Curs.TokenObj.isCategory(TokenCategory.tokEOF))
            {
                if (AllowEOLs && EOLExtraCount < MaxExtraCount && isToken(TokenCategory.tokEOL))
                {   // Allow one solo extra spacing??? line in Ckan file 
                    EOLExtraCount = EOLExtraCount + 1;
                    expectToken(TokenCategory.tokEOL);
                }
                if (Curs.TokenObj.isCategory(TokenCategory.String))
                {  // next token is of form "ascii"  good enough for us.
                    expectToken(TokenCategory.String);  // Eat 1 name string
                    expectToken(":", TokenCategory.Token);
                    if (AllowEOLs && EOLExtraCount < MaxExtraCount && isToken(TokenCategory.tokEOL))
                    {
                        EOLExtraCount = EOLExtraCount + 1;
                        expectToken(TokenCategory.tokEOL);
                    }
                    bool Ret = parseValue();
                    if (Ret!= true)
                    {
                        throw new FormatException($"Fatal Format Error in {TokFile.Name}: \n\t Expected a Value  got <{Curs.TokenObj.theToken}>.");
                    }
                } else
                {
                    if (EmptyNVisOk == false)
                    {
                        // Note may say <> if it hits either EOl or EOF or Illgl or ..? .. TODO: ok?
                        throw new FormatException($"Fatal Format Error in {TokFile.Name}: \n\t Expected NAme:Value Pair got <{Curs.TokenObj.theToken}>.");
                    }
                }
                if (!Curs.TokenObj.isCategory(TokenCategory.Token) || !Curs.TokenObj.theToken.Equals(","))
                {
                    // anything after an NV pair that is NOT "," ends the list    
                    // yes that means we mandate ",' is before <EOL> not after 
                    return true;
                } 
                expectToken(",", TokenCategory.Token);  // Eat the ","
                expectToken(TokenCategory.tokEOL);     // Eat required EOL
                EmptyNVisOk = false;                 // If we got a "," require an NV
            }
            throw new FormatException($"Fatal Format Error in {TokFile.Name}:\n\t  UnExpected <EOF> in {{ nameValueList }}.");

        }

        bool parseListVals()
        {   // grammar
            // Nope: <ValueList> := <Empty> Illegal for now
            // <ValueList>     := <Value>
            // <ValueList>     := <Value> <,> <EOL>  <ValueList>
            // leading Vlaue is not optional as .. [ ] .. is for us illegal.
            bool EmptyVisOk = false; //change to true to allow empty Valuelists AKA .. [ <Empty> ] ..
            // Check For First V
            while (!Curs.TokenObj.isCategory(TokenCategory.tokEOF))
            {
                TokenFile.TokenObject toko = Curs.TokenObj;
                if (parseValue())
                {  
                   // We parsed 1 value.  (incs [ ...] or { ...} as 1 value 
                }
                else
                {
                    if (EmptyVisOk == false)
                    {
                        // Note may say <> if it hits either EOl or EOF or Illgl or ..? .. TODO: ok?
                        throw new FormatException($"Fatal Format Error in {TokFile.Name}: \n\t Expected NAme:Value Pair got <{Curs.TokenObj.theToken}>.");
                    }
                }
                if (!Curs.TokenObj.isCategory(TokenCategory.Token) || !Curs.TokenObj.theToken.Equals(","))
                {
                    // anything after an NV pair that is NOT "," ends the list    
                    // yes that means we mandate ",' is before <EOL> not after 
                    return true;
                }
                expectToken(",", TokenCategory.Token);  // Eat the ","
                expectToken(TokenCategory.tokEOL);     // Eat required EOL
                EmptyVisOk = false;                 // If we got a "," require an NV
            }
            throw new FormatException($"Fatal Format Error in {TokFile.Name}:\n\t  UnExpected <EOF> in {{ nameValueList }}.");





        }
        /// <summary>
        /// parses 1 Value (which may even vbe a counpound or Array value 
        /// </summary>
        /// <returns> true if it found a value </returns>
        bool parseValue()
        {   // grammar:
            //<Value>         := <ValueString>
            //<Value>         := <ValueNumber>
            //<Value>         := <boolean> 
            //<Value>         := <CompundValue>
            //<Value>         := <ArrayValue>
            TokenCategory tcat = Curs.TokenObj.TokenCategory;
            bool FoundValue = false;
            switch (tcat)
            {
                case TokenCategory.String:  // <ValueString>
                    expectToken(TokenCategory.String);
                    FoundValue = true;
                    break;
                case TokenCategory.Boolean:  // <ValueString>
                    expectToken(TokenCategory.Boolean);
                    FoundValue = true;
                    break;
                case TokenCategory.Number: // <ValueNumber>
                case TokenCategory.Decimal:
                    expectToken(tcat);
                    FoundValue = true;
                    break;
                case TokenCategory.Token:  // Could be <{> or <[> or error
                    if (Curs.TokenObj.theToken.Equals("{") )
                    {
                        return parseCompound();
                    }
                    if (Curs.TokenObj.theToken.Equals("["))
                    {
                        return parseArray();
                    }
                    FoundValue = true;
                    break;
            }

            return FoundValue;
        }

        bool isToken(TokenCategory tc)
        {
            return Curs.TokenObj.TokenCategory == tc;
        }

        bool expectToken (string tokStr, TokenCategory tc)
        {
            if (Curs.TokenObj.TokenCategory == tc && Curs.TokenObj.theToken.Equals(tokStr)  )
            {   // we match the category and the string     
                return Curs.advance();
            }
            throw new FormatException($"Fatal Format Error in {TokFile.Name}:\n\t Expected <{tokStr}:{tc}> got <{Curs.TokenObj.theToken}:{Curs.TokenObj.TokenCategory}>.");
            // return FileIsValid = false; 
        }
        bool expectToken(TokenCategory tc)
        {
            if (Curs.TokenObj.TokenCategory == tc )
            {   // we match the category and dont care about the string.   
                return Curs.advance();
            } 
            else if (AllowEOLs && EOLMissedCount < MaxMissedCount && (tc == TokenCategory.tokEOL))
            {   // missing EOLs is deemed Ok
                //dont eat the NON EOL token
                EOLMissedCount = EOLMissedCount + 1;
                return true;
            }
            tc.ToString();

            throw new FormatException($"Fatal Format Error in {TokFile.Name}:\n\t Expected <{tc.ToString()}> got <{Curs.TokenObj.TokenCategory}>.");
            // return FileIsValid = false; 
        }
    }

    public class FormatException : Exception
    {
        public string Cause { get; set;  }

        public FormatException(string s) { Cause = s; }
    }


    class NameValueData {
        public int Count {get; set;} = 0;
        public string Value { get; set; } = "";
        public string Name { get; set; }

        public bool Required { get; set; }

        NameValueData(string n, bool required)
        {
            Name = n;
            Required = required;
        }

    } 

}
