using System ;
using System.Text ;
using System.Collections.Generic;

namespace RegexBuilders {
  class LiteralString : RegexBuilder {
    string val ;

    public LiteralString(string str) : base() {
      val = str ;
    }

    public static LiteralString literally(string str){
      return new LiteralString(str);
    }
    
    private static readonly HashSet<char> ESCAPED_CHARS = new HashSet<char>(new char[]{
      '[',
      ']',
      '*',
      '+',
      '?',
      '^',
      '$',
      '(',
      ')',
      '\\'
    });
    protected override string selfToRegexString(){
      //Assume we need 20% more chars to escape
      StringBuilder regexString = new StringBuilder((int)(1.2*val.Length));
      //If we're correct, we avoid resizing the buffer too much, which is good
      //If we're overestimating, we would be wasting memory
      
      foreach(char c in val){
        if(ESCAPED_CHARS.Contains(c)){
          regexString.Append('\\');
        }
        regexString.Append(c);
      }
      return regexString.ToString();
    }
  } 
}