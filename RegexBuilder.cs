using System ;
using System.Text ;
using System.Collections.Generic ;
using System.Text.RegularExpressions;

namespace RegexBuilders {
  abstract class RegexBuilder {
    
      private enum TermType {Self,Follower,Alternative} ;
      private record Term(int quantifier,TermType t, RegexBuilder p) {
        //number of times the term is repeated 
        //0 --> term is optionl 
        //-1 --> term could be repeated any number of times, possibly 0
        //-2 --> term could be repeated any number of times, at least 1
        //-3 --> term is min-max quantified by the pair mmQuantifier
        public int quantifier {get; set;} = quantifier ;
        //(n > 0,0) ---> Term must occur at least n times, but could occur arbitarily many times
        //(0,n > 0) ---> Term must occur at most  n times, but could be omitted
        //(n > 0, m > 0) ---> Terms must occur at least n times, and at most m times
        //(0,0) ---> Not a valid min-max quantification
        public (uint,uint) mmQuantifier {get; set;} = (0,0) ;
        public TermType t {get; init;} = t ;
        public RegexBuilder p {get; init;} = p ;
      }
      private List<Term> terms ;

      public RegexBuilder() {
        terms = new List<Term>() ;
        terms.Add(new Term(1,TermType.Self,this)) ;
      }

      private void quantifyLastTerm(int q, (uint,uint)? mmQ = null){
        terms[terms.Count - 1].quantifier = q ;
        if(q == -3 && mmQ.HasValue){
          terms[terms.Count - 1].mmQuantifier = mmQ.Value ;
         }
      }

      public RegexBuilder optionally {
        get {
            quantifyLastTerm(0);
            return this ;
        }
      }

      public RegexBuilder anyTimesRepeated {
        get {
            quantifyLastTerm(-1);
            return this ;
        }
      }

      public RegexBuilder manyTimesRepeated {
        get {
            quantifyLastTerm(-2);
            return this ;
        }
      }

      public RegexBuilder nTimesRepeated(uint numtimes) {
        if(numtimes <= 1) {
          throw new 
            ArgumentOutOfRangeException("Method RegexBuilder.nTimesRepeated should be called with an argument > 1, passed argument was "+ numtimes) ;
        }

        quantifyLastTerm((int)numtimes);
        return this ;
      }

      public RegexBuilder repeatedLessThanOrEqual(uint maxtimes) {
        if(maxtimes == 0) throw new 
          ArgumentOutOfRangeException("No pattern can be repeated less than or equal 0 times");
        if(maxtimes == 1) throw new 
          ArgumentOutOfRangeException("Use method RegexBuilder.optionally (implementing the ? regex quantifier) to express a pattern occuring 0 or 1 times ");

        quantifyLastTerm(-3,(0,maxtimes)) ;
        return this ;
      }
      public RegexBuilder repeatedMoreThanOrEqual(uint mintimes) {
        quantifyLastTerm(-3,(mintimes,0));
        return this ;
      } 
      public RegexBuilder repeatedLessThan(uint maxtimes) {
        if(maxtimes == 0) throw new 
          ArgumentOutOfRangeException("No pattern can repeat less than 0 times") ;
        return repeatedLessThanOrEqual(maxtimes - 1);
      } 
      public RegexBuilder repeatedMoreThan(uint mintimes) {
        if(mintimes != uint.MaxValue) return repeatedMoreThanOrEqual(mintimes + 1);
        throw new ArgumentOutOfRangeException("No pattern can repeat more than uint.MaxValue");
      } 
      public RegexBuilder repeatedBetween(uint mintimes,uint maxtimes) {
        if(maxtimes == mintimes) throw new 
          ArgumentException("min-max repetition requires distinct bounds, use method RegexBuilder.nTimesRepeated to express repetition a constant number of times");

        if(maxtimes > mintimes) quantifyLastTerm(-3,(mintimes,maxtimes)) ;
        else                    quantifyLastTerm(-3,(maxtimes,mintimes)) ;
        return this ;
      }    

      public RegexBuilder then(RegexBuilder follower) {
        if(follower == this) terms.Add(new Term(1,TermType.Self,this));
        else                 terms.Add(new Term(1,TermType.Follower,follower)) ;
        return this ;
      }

      public RegexBuilder or(RegexBuilder alternative) {
        if(alternative == this) terms.Add(new Term(1,TermType.Self,null));
        else                    terms.Add(new Term(1,TermType.Alternative,alternative)) ;
        return this ;
      }

      //Since RegexBuilder is abstract, it doesn't actually know what it's supposed to parse 
      //Therefore it can't generate anything when asked to generate a regex pattern
      //It delegates this to the derieved class via this method
      protected abstract string selfToRegexString() ;

      public string toRegexString() {
        StringBuilder regexString = new StringBuilder() ;
        
        regexString.Append("(?:");
        regexString.Append(selfToRegexString()) ;
        regexString.Append(")");
        regexString.Append(numToRegexQuantifier(terms[0].quantifier,terms[0].mmQuantifier));

        string selfReferencePlaceHolder = Guid.NewGuid().ToString() +"__SELF__HERE__"+ Guid.NewGuid().ToString();

        string selfReferenceDelimiter = Guid.NewGuid().ToString() +"__SELF_DELIMIT__"+ Guid.NewGuid().ToString();
    
        for(int i = 1; i<terms.Count; i++){
          var term = terms[i];
          
          switch(term.t){
            //A recursive insertion, should be done lazily at the end of construction
            case TermType.Self:
              regexString.Append(selfReferenceDelimiter);
              
              if(term.p == null) regexString.Append("|");

              regexString.Append("(?:");
              regexString.Append(selfReferencePlaceHolder);
              
              break ;
              
            case TermType.Follower:
              regexString.Append("(?:");
              regexString.Append(term.p.toRegexString()) ;
              break ;
              
            case TermType.Alternative:
              regexString.Append("|");
              regexString.Append("(?:");
              regexString.Append(term.p.toRegexString());
              break ;
          }
          
          regexString.Append(")");
          regexString.Append(numToRegexQuantifier(term.quantifier,term.mmQuantifier));
          
          if(term.t == TermType.Self) regexString.Append(selfReferenceDelimiter);
        }

        Regex selfReferentialTerms = new Regex(selfReferenceDelimiter +".*?"+ selfReferenceDelimiter);
        string regexStringWithoutPlaceHolders = selfReferentialTerms.Replace(
                                                                            regexString.ToString(),
                                                                            "");
      
        regexString.Replace(selfReferencePlaceHolder,regexStringWithoutPlaceHolders);
        regexString.Replace(selfReferenceDelimiter,"");
        return regexString.ToString() ;
      }
      public static implicit operator RegexBuilder(string str) => LiteralString.literally(str) ;

      private static string numToRegexQuantifier(int q,(uint,uint) mmQ) => 
                                                           (q >  1)? "{"+ q +"}":
                                                           (q == 0)? "?"        :
                                                           (q ==-1)? "*"        :
                                                           (q ==-2)? "+"        :
                                                           (q ==-3)? minmaxQuantifierToString(mmQ):"" ;
      private static string minmaxQuantifierToString((uint,uint) mmQ) => (mmQ.Item1 == 0)? "{,"+ mmQ.Item2 +"}" :
                                                                         (mmQ.Item2 == 0)? "{"+  mmQ.Item1 +",}":
                                                                         "{"+ mmQ.Item1 +","+ mmQ.Item2 +"}"    ;
  }

  //singleton for whitespace specifically, just to enhance readability
  class WhiteSpace : RegexBuilder {
    private WhiteSpace() : base() {}

    public static readonly WhiteSpace 
          Whitespace = new WhiteSpace() ;

    protected override string selfToRegexString() => "\\s" ;
  }

  //extensions methods
  public static class Utils {
    
    //As a start, implements the simplification rule
    //            (?:(?:(?:...(?:"""ARBITARY_REGEX_HERE"")...))) 
    //                             <---Simplifies To--->
    //                        (?:"""ARBITARY_REGEX_HERE"")
    //(This is EXTREMLY hard to get right.)
    //Also implements the substitution (?0[0-9]|[1-8][0-9]|9[0-9]) <---> [0-9]{2}
    private static readonly Lazy<Regex> redundantOpeningParen 
      = new Lazy<Regex>(() => new Regex(@"(\(\?:){2,}",RegexOptions.Compiled)) ; 
    public static string cleanRegexString(this string reStr) {
      var cleanStr = new StringBuilder(reStr.Length) ;
      var searchRe = redundantOpeningParen.Value ;
      var redundantParens = searchRe.Matches(reStr);

      int lastInsertedPos = 0;
      foreach(Match m in redundantParens){
        if(m.Index > lastInsertedPos){
          cleanStr.Append(reStr.Substring(lastInsertedPos,m.Index - lastInsertedPos));
        }
        int start = m.Index + m.Length ;
        int curr  = start ;
        int outerOpeningParen = m.Length/3 ;
        int internalOpeningParen = 0 ;

        while(!(internalOpeningParen == 0 && reStr[curr] == ')')){
          switch(reStr[curr]){
              case '(':
                if(reStr[curr+1] == '?' && reStr[curr+2] == ':'){
                   curr = curr + 2 ;
                   internalOpeningParen++ ;
                }
              break ;
              case ')':
                internalOpeningParen-- ;
              break ;
              default : break ;
          }
          curr++ ;
        }

        int numClosingParen = 0 ;
        while(curr < reStr.Length && reStr[curr] == ')'){
          numClosingParen++ ;
          curr++;
        }

        if(numClosingParen == outerOpeningParen){
          cleanStr.Append("(?:");
          cleanStr.Append(reStr.Substring(start,curr - numClosingParen - start));
          cleanStr.Append(")");
          lastInsertedPos = curr ;
        }
        else lastInsertedPos = m.Index ;
      }
      if(lastInsertedPos < reStr.Length){
        cleanStr.Append(reStr.Substring(lastInsertedPos,reStr.Length - lastInsertedPos));
      }

      cleanStr.Replace("(?:0[0-9]|[1-8][0-9]|9[0-9])","[0-9]{2}");
      return cleanStr.ToString() ;
    }
  }
}
