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
        public bool Exists { get; private set; }

        public bool AllowSlashN { get; set; }  = false;


        public CkanTokeniser(string filePath)
        {
            FilePath = filePath;
            // Check file exists
            FileInfo fi = new FileInfo(FilePath);
            Exists = fi.Exists;
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

        enum TokState { Start, InString, hasEscape, End }

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
            String T;
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
                    cat = parseString(out T, '"');
                    return new TokenObject(ws, T, cat, C);
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


        private TokenCategory parseString(out string t, char term)
        {
            tokWork.Append((char)strm.Read()); // append opening Quote
            int C=0;
           
            while ((C = strm.Peek()) != term)
            {
                if (C == -1)
                {  // unexpected EOF inside string
                    t = tokWork.ToString();
                    SomeTokenIllegal = true;
                    return TokenCategory.tokIllegal;
                }
                if (C == '\\') 
                {
                    tokWork.Append((char)strm.Read());
                    C = strm.Peek();
                    if (C == -1)
                    {
                        t = tokWork.ToString();
                        SomeTokenIllegal = true;
                        return TokenCategory.tokIllegal;
                        ;
                    }
                    if (C != '\\' && C != '\"'  && (C!='n' || AllowSlashN == false))
                    {
                        // keep parsing but all \x sequences except \\ are currently defined illegal
                        SomeTokenIllegal = true;
                        t = tokWork.ToString(); 
                        return TokenCategory.tokIllegal;
                    }
                }
                tokWork.Append((char)strm.Read());
            }
            tokWork.Append((char)strm.Read());
            t = tokWork.ToString();

            return TokenCategory.String; ;
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
