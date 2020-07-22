using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace CkanLocaliser
{
	public enum TokenCategory
	{
		/// <summary> Some kind of String literal " [char][s] " </summary>
		Strung = 1,
		/// <summary> [sign] digit[s] </summary>
		Number = 3,
		/// <summary> [sign] [digit][s] . [digit][s] </summary>
		Decimal= 4,
		/// <summary> [sign] [digit][s] . [digit][s] [ E [sign]digit[s] ] :E is optional but if present requires a digit </summary>
		Float = 5,
		/// <summary> case sensitive literal [true] [false] </summary>
		Boolean = 6,
		/// <summary> Most Everything Else likely as single chars </summary>
		Token = 7,
		/// <summary> Anything defined as being a multichar special if we have them. eg <=  may be unused? </summary>
		TokenGraph = 8,
		/// <summary>  A punctuation style token that we were not expecting. 
		/// but not strictly illegal like some malformed UTF8 or 1.1E- would be </summary>
		TokenUnk = 9,
		/// <summary> WS ENDS in Some kind of line feed \r \n combo that represents 1 line. Token is always "". </summary>
		tokEOL = 10,
		/// <summary> WS is "", Token is "". File Ends. </summary>
		tokEOF = 99,
		/// <summary> These are hard to come by but encountering 1.1E-  outside a string literal will usually do the trick. </summary>
		tokIllegal = 100,
    };

	/// <summary>
	/// A file is decomposed into tokens according to the tokenisation rules of the tokeniser.
	/// This file encapsualtes the knowledge that the file is composed of lines and that each line is sequences of tokens.
	/// Where each token also records any WS that occured before it.
	/// EOL tokens have empty strings in the theToken, and the chars that caused the EOL in at the end of the WS.
	/// EOF Tokens also have empty string theToken, but may have non EOL whitepace in the whitespace.
	/// String literal conatentaion of every token in the file, will barring anomalies, be an exact charecter for character copy of the file read. 
	/// Anomalies may happen if we gracefully fail when encountering some defined to be illegal (in our context not just w3Cs) chars etc.
	/// So while we we do parse Json file there may well exist some strictly legal ones we strictly barf at (or quitely modify).
	/// RTFM.
	/// </summary>
	/// <param name="mgr">KSPManager containing our instances</param>
	/// <param name="user">IUser object for interaction</param>
	class TokenFile
	{
		/// <summary>
		/// While exact token type syntax are language ITtokensier defined, generic patterns accross all file types exist.
		/// and files are then parsed in terms of those.
		/// </summary>
		private ITokeniser Tokeniser;

		//		private list<string> File; // = new List<string> ();
		public string FilePath
		{
			get { return Tokeniser.FilePath; }
		}

		public List<TokenFile.Line> File { get; set; } = new List<TokenFile.Line>();


		/// <summary>
		/// Construct a tokensier
		/// RTFM.
		/// </summary>
		/// <param name="t">ITokeniser defines the syntax</param>
		public TokenFile(ITokeniser t)
		{
			File = new List<TokenFile.Line>();
			Tokeniser = t;
		}

		public Cursor getCursor(bool echo) { return new Cursor(this, echo); }


		public TokenCategory parse()
		{ 
			Line L = new Line();
			File.Add(L);
			TokenObject T;
			do
			{
				L.TheLine.Add(T = Tokeniser.ReadToken());
				if (T.TokenCategory == TokenCategory.tokEOL)
				{
					L = new Line();
					File.Add(L);
				}

			} while (T.TokenCategory != TokenCategory.tokEOF);
			return T.TokenCategory;
		}

		public void writeTo( TextWriter  Dest)
        {
			for (int i=0; i < File.Count; i++ )
            {
				Line L = File[i];
				for (int j = 0; j < L.TheLine.Count; j++)
                {
					TokenObject T = L.TheLine[j];
					Dest.Write(T.WhiteSpace);
					Dest.Write(T.theToken);
				}
			}
		}



		/// <summary>
		/// A line from a file decomposed into tokens, when read it always ends in EOL or EOF.  
		/// 
		/// ***WARNING WARNIGN DANGER WILL ROBINSON***
		/// When the file is read EVERY LINE object ends with EOL or EOF and there is never an EOL mid Line Object.
		/// When later modifying the data structure outside this class that is not a requirement at all.
		/// Do not think that EOL ending line objects is a permanent part of the contract. Its not. 
		/// Only that it comes out of read file that way.
		/// </summary>
		public class Line
		{
			public List<TokenObject> TheLine { get; set; } = new List<TokenObject>();
 

		}    

		/// <summary>
		/// This is deliberately a struct/class to chnage how it is stored in memory and the efficiency of that.
		/// </summary>
		public class TokenObject
		{
			public string WhiteSpace { get;  set; }
			public string theToken { get;  set; }

			/// <summary>
			/// broad classes of token objects
			/// </summary>
			public TokenCategory _tt;  
			public TokenCategory TokenCategory { get { return _tt; }  }

			/// <summary>
			/// More specific Token information that will be parsed file specific.
			/// often is the Ascii value of the tokens only Char
			/// TODO it needs to be thought about if/when ause for it become required.
			/// Most parsers Ive built before needed it and the category sooner or later.
			/// </summary>
			public int TokenType { get; }

			public TokenObject(string ws, string tok, TokenCategory t, int TType) 
			{ 
				WhiteSpace = ws; 
				theToken = tok; 
				_tt = t;
				TokenType = TType;
			}

			public bool isCategory(TokenCategory cat)
            {
				return TokenCategory == cat;
			}

		}

		private static TokenObject _EOF; // = new TokenObject("", null, getEOF());

		public static TokenObject getEOF() {  return _EOF;  }

		static TokenFile()
        {
			_EOF = new TokenObject("", null, TokenCategory.tokEOF, -1);
		}


		public class Cursor
        {
			public TokenFile TokFile { get; set; }
			public int LineNo { get; set; } = 0;
			public int TokNo { get; set; } = 0;

			public bool Echo { get; set; } = false;

			public Line Line { get { return TokFile.File.ElementAt(LineNo); } }
			public TokenObject TokenObj { get { return Line.TheLine.ElementAt(TokNo); } }

			public bool advance()
            {
				Line L = Line;
				TokenObject tok = TokenObj;
				if (tok.TokenCategory == TokenCategory.tokEOF) 
				{   // we are already at EOF 
					if (Echo)
                    {
						Console.Write("<EOF>");
					}
					return false; 
				}
				if (Echo)
				{
					Console.Write(tok.WhiteSpace);
					Console.Write(tok.theToken);
				}
				TokNo++;
				if (TokNo < Line.TheLine.Count) 
				{  // we are now at next token on the same line
					return true; 
				}
				TokNo = 0;
				LineNo++;
				if (LineNo >= TokFile.File.Count) 
				{   // There is probably a bug somewhere as we ran out of lines & tokens but didnt meet an EOF. 
					return false; 
				}
				// we are now at next token on the same line
				return true;
            }

			public Cursor(TokenFile t, bool echo)
            {
				TokFile = t;
				Echo = echo;
			}

			public Cursor(TokenFile t, int LNo, int TNo)
			{
				TokFile = t;
				LineNo = LNo;
				TokNo = TNo;
				Echo = false;
			}

			public void setPosition(int LNo, int TNo)
            {
				LineNo = LNo;
				TokNo = TNo;
			}

		}


	}

}
