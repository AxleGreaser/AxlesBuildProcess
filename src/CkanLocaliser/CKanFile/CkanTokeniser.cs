using System;
using System.Text;
using System.IO;

using static CkanLocaliser.TokenFile;
using System.Runtime.ExceptionServices;

namespace CkanLocaliser
{
    /// <summary>
    /// Interface to a Tokeniser of some type. Currently only JSON.
    /// </summary>
    interface ITokeniser
    {
        public TokenObject ReadToken();
        public bool SomeTokenIllegal { get; set; }

        abstract public string FilePath { get; set; }
    }

    class CkanTokeniser : ITokeniser
    {

        public bool SomeTokenIllegal { get; set; }

       // public string Name { get; set; }

        StringBuilder wsWork = new StringBuilder(20);
        StringBuilder tokWork = new StringBuilder(200);

        private StreamReader strm;
        public string FilePath { get;  set; }
        public bool Exists
        {
            get { return strm != null; }
        }
        /// <summary>
        /// the two character sequence "\n" is allowed in Strings if true.
        /// </summary>
        public bool AllowSlashN { get; set; }  = false;


        public CkanTokeniser(string filePath)
        {
            FilePath = filePath;
            // Check file exists
            FileInfo fi = new FileInfo(FilePath);
            bool Exists = fi.Exists;
            SomeTokenIllegal = false;
            if (Exists)
            {
                strm = new StreamReader(FilePath, Encoding.UTF8);
            }
            else
            {
                strm = null;
                // TODO Error handling
                Console.WriteLine($"File '{FilePath}' Not found");
            }
        }

//        enum TokState { Start, InString, hasEscape, End }

        public TokenObject ReadToken()
        {
            tokWork.Clear();   // set both to ""
            wsWork.Clear();
            if (strm == null)
            {
                return getEOF();
            }

            //    TokState state = Start;
            int cc;
            string ws;
            string T;
            TokenCategory cat;
            char C = (char)stripWhiteSpace(out ws);
            switch (C)
            {
                case '\n':
                case '\r':
                    // 
                    return new TokenObject(ws, "", TokenCategory.tokEOL, 0xA);

                case '\uFFFF':
                    return new TokenObject(ws, "", TokenCategory.tokEOF, -1);

                case '"':
                    // Start of a string 
                    TokenObject t = parseString(ws,'"');
                    return t;
                case ':':
                case '{':
                case '}':
                case '[':
                case ']':
                case ',':
                    cc = strm.Read();
                    return new TokenObject(ws, new string((char)cc,1), TokenCategory.Token, cc);

                default:
                    // TODO are signed numbers allowed
                    if (char.IsDigit(C))
                    {
                        cat = parseNumber(out T);
                        return new TokenObject(ws, T, cat, '1');
                    }
                    else
                    {
                        return LastDitch(ws);

                    }
            }
            
        }

        /// <summary>
        /// Up until this point the file has been a lookahead 1 char token format.... eg seeing " => it must be a string etc.
        /// Well numbers are always weird because numbers are.  1.0E- has no valid meaning and should be an error in every file format because yuk.
        /// Now   true and false are legal tokens but we cant know whether it going to to turn out to be truck until we get there
        /// So we do the best we can and parse the next C like token then check if it is true or false.
        /// hence LastDitch() 
        /// All bare words that are not the literals true or false are unknown tokens and willresult in an File format error ecventually at the lexical level.
        /// AKA even though we know for certain it will be an error as th okensier we just dont throw.
        /// We just gve back token category the lexical analyser will always barf at.
        /// </summary>
        private TokenObject LastDitch(string ws)
        {
            // First Check for EOF
            int cc = strm.Peek();
            if (cc == -1)
            {
                return getEOF();
            }
            // parse/tokenise   Alpha [AlphaNum]  //Note we can/do assume first char is not alpha
            do
            {
                tokWork.Append((char)strm.Read());
                cc = strm.Peek();
            } while (cc < 127  && char.IsLetter((char)cc) || char.IsDigit((char)cc));
            // true EOF is going to parse a s valid token eventhought he EOF will likely be an error inthe next token.
            // well, that is, depending on the file format, but its not a tokenising level error
            string tok = tokWork.ToString();

            if (tok.Equals("true") || tok.Equals("false"))
            {
                return new TokenObject(ws, tok, TokenCategory.Boolean, 'b');
            }
            return new TokenObject(ws, tok, TokenCategory.TokenUnk, 'u' );
        }


        /// <summary>
        /// ckan File can only contain integrs and decimals not Floats so we only parse inetegrs and decimals
        /// All char sequences yield valid tokenised results. the text  <1.4.5.6> will tokenise first as <1.4> then as <.5> then as <.6>
        /// The lexical parser will decide that is not a valid sequence for ckan file, but it will tokenise.
        /// </summary>
        /// <param name="t">returns the literal chars in this token</param>
        /// <returns>the token category Decimal or Number as appropriate</returns>
        private TokenCategory parseNumber(out string t)
        {
            tokWork.Append((char)strm.Read()); // append opening Quote
            int C;
            bool hasDec = false;
            while (char.IsDigit((char)(C = strm.Peek())) ||  (hasDec==false && (hasDec= (C == '.'))==true))
            {
                tokWork.Append((char)strm.Read());
            }
            // TODO: are floats allowed?
            t = tokWork.ToString();
            return hasDec ? TokenCategory.Decimal : TokenCategory.Number;
        }

        /// <summary>
        /// Parses a string terminated by double quotes.
        /// Illegal things are:  most \ sequences except \\
        /// \n is allowed only if expressly permitted.
        /// </summary>
        /// <param name="t">returns the literal chars in this token including both quotes </param>
        /// <param name="term">the char we expect to terminate the string always a " </param>
        /// <returns>TokenCategory.String or TokenCategory.tokIllegal if bad thigns happen</returns>
        private TokenObject parseString(string ws,char term)
        {
            tokWork.Append((char)strm.Read()); // append opening Quote
            int C=0;
            string t;
            int ret = 'A'; // A is for Ascci
            while ((C = strm.Peek()) != term)
            {
                bool UnKnownUTF8 = (C >= 127    // is non vanilla ascii
                                &&  C != 0xFEFF   // except FEFF BOM ... 'ZERO WIDTH NO-BREAK SPACE' <﻿> <<yes there is one there... arrow past it carefully.
                                && C!= 0x2019  // Right single quotaion mark <’>
                                && C!= 0xA0   // No break Space  < >
                                && C!= 0xD7   // 'MULTIPLICATION SIGN' <×> 
                                && C!= 0x2014 //  'EM DASH'  <—>
                                && C!= 0xFC   // 'LATIN SMALL LETTER U WITH DIAERESIS'  <ü> 
                                && C != 0x201C // 'LEFT DOUBLE QUOTATION MARK'          <“>
                                && C != 0x201D // 'RIGHT DOUBLE QUOTATION MARK'         <”>
                                && C != 0xE8   // 'LATIN SMALL LETTER E WITH GRAVE'     <è>
                                && C != 0xC7   // 'LATIN CAPITAL LETTER C WITH CEDILLA' <Ç>
                                && C != 0xFF1F // 'FULLWIDTH QUESTION MARK'             <？>
                                && C != 0x2018 // 'LEFT SINGLE QUOTATION MARK'          <‘>
                                && C != 0xFF01 // 'FULLWIDTH EXCLAMATION MARK'          <！>
                                && C != 0xE9   // 'LATIN SMALL LETTER E WITH GRAVE'     <é>
                                && C != 0xFF08 // 'FULLWIDTH LEFT PARENTHESIS'          <（>
                                && C != 0xFF09 // 'FULLWIDTH RIGHT PARENTHESIS'          <）>
                                && C != 0x4E07 // 'ten thousand; innumerable'           <万>
                                && C != 0x6237 // 'door; family'                        <户>
                                && C != 0x515A // 'political party, gang, faction'      <党>
                                && C != 0x51FA // 'go out, send out; stand; produce'    <出>
                                && C != 0x54C1 // 'article, product, commodity'         <品>
                                && C != 0xFF0C // 'FULLWIDTH COMMA'                     <，>
                                && C != 0x5FC5 // 'surely, most certainly; must'        <必>
                                && C != 0x5C5E // 'class, category, type; belong to'    <属>
                                && C != 0x7CBE // 'essence; semen; spirit'              <精>
                                && C != 0xB3   // 'SUPERSCRIPT THREE'                   <³>
                                && C != 0xE7   // 'LATIN SMALL LETTER C WITH CEDILLA'   <ç>
                                && C != 0xE3   // 'LATIN SMALL LETTER A WITH TILDE'     <ã>
                                && C != 0xEA   // 'LATIN SMALL LETTER E WITH CIRCUMFLEX' <ê>
                                && C != 0xF5   // 'LATIN SMALL LETTER O WITH TILDE'     <õ>
                                && C != 0xED   // 'LATIN SMALL LETTER I WITH ACUTE'     <í>
                                && C != 0xB4   // 'ACUTE ACCENT'                        <´>
                                && C != 0xB0   // 'DEGREE SIGN'                         <°>
                               );
                bool BadChar = (C == -1) ||   // is EOF
                               (C < 32 && !Char.IsWhiteSpace((char)C)) ||  // is control Char
                               UnKnownUTF8;
                if (UnKnownUTF8)
                {
                    ret = 'U';  // U is for UTF8
                }

                if (BadChar)
                {  // unexpected EOF inside string or other unexpected control char or non ascii char 
                    t = tokWork.ToString() + "<" + C.ToString() + ":" + C.ToString("X") + ">";
                    SomeTokenIllegal = true;
                    return new TokenObject(ws ,t,TokenCategory.tokIllegal, C==-1?-1:7);
                }
                if (C == '\\') 
                {
                    tokWork.Append((char)strm.Read());
                    C = strm.Peek();
                    if (C == -1)
                    {
                        t = tokWork.ToString();
                        SomeTokenIllegal = true;
                        return new TokenObject(ws, t, TokenCategory.tokIllegal, -1);
                        ;
                    }
                    if (C != '\\' && C != '\"'  && (C!='n' || AllowSlashN == false))
                    {
                        // keep parsing but all \x sequences except \\ are currently defined illegal
                        SomeTokenIllegal = true;
                        t = tokWork.ToString(); 
                        return new TokenObject(ws, t, TokenCategory.tokIllegal, 7); ;
                    }
                }
                tokWork.Append((char)strm.Read());
            }
            tokWork.Append((char)strm.Read());
            t = tokWork.ToString();

            return new TokenObject(ws, t, TokenCategory.Strung, ret); ;
        }

        /// <summary>
        /// read zero or more ws characters stop at first \r\n \n\r or bare \r[-\n] \n[-\r]  
        /// </summary>
        /// <param name="ws"> resulting whitespeace string includign the trailing EOL/EOF chars </param>
        /// 
        /// <returns> the char/byte? that caused ws to end (\r\n on EOL) or -1 if EOF reached</returns>
        private int stripWhiteSpace(out string ws)
        {
            int lastC;
            int C = strm.Peek();
            while ((C & 0x7FFF) == C && char.IsWhiteSpace((char)C))
            {
                if (C == '\r' || C == '\n')
                {   // Current char is \r || \n
                    // What about ... all them whacky unicode chars?  or even 0xA0
                    wsWork.Append((char)strm.Read());
                    lastC = strm.Peek();
                    if ((lastC == '\r' || lastC == '\n') && lastC != C)
                    {   // last char was the other one: Take the pair as 1 new line
                        wsWork.Append((char)(lastC = strm.Read()));
                        C = lastC; 
                    } 

                    break; // break with C as the value of the (whichever of)or(last of) \r or \n added to ws.
                }
                wsWork.Append((char)(lastC = strm.Read()));
                C = strm.Peek();
            }
            // AT this point C is the first char that is not whitespace. 
            // OR if we met EOL it is the last EOL encountered char
            // if we met EOF  any_of { \r\EOF \n\EOF \w\EOF } we return C = -1.
            ws = wsWork.ToString();

            return C;
        }
    }



}
