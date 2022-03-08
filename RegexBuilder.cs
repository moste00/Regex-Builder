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
        public (int,int) mmQuantifier {get; set;} = (0,0) ;
        public TermType t {get; init;} = t ;
        public RegexBuilder p {get; init;} = p ;
      }
      private List<Term> terms ;

      public RegexBuilder() {
        terms = new List<Term>() ;
        terms.Add(new Term(1,TermType.Self,this)) ;
      }

      private void quantifyLastTerm(int q, int? min = null, int? max = null){
        terms[terms.Count - 1].quantifier = q ;
        if(q == -3){
          var mmQ = terms[terms.Count - 1].mmQuantifier ;
          if(min != null) mmQ = (min.Value ,mmQ.Item2) ;
          if(max != null) mmQ = (mmQ.Item1 ,max.Value) ;
          terms[terms.Count - 1].mmQuantifier = mmQ ;
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

      public RegexBuilder nTimesRepeated(int numtimes) {
        if(numtimes <= 1) {
          throw new 
            ArgumentOutOfRangeException("Method RegexBuilder.nTimesRepeated should be called with an argument > 1, passed argument was "+ numtimes) ;
        }

        quantifyLastTerm(numtimes);
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
        regexString.Append(numToRegexQuantifier(terms[0].quantifier));

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
          regexString.Append(numToRegexQuantifier(term.quantifier));
          
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

      private static string numToRegexQuantifier(int q) => (q >  1)? "{"+ q +"}":
                                                          (q == 0)? "?"        :
                                                          (q ==-1)? "*"        :
                                                          (q ==-2)? "+"        : "" ;
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
    
    //TODO
    //As a start, implement the simplification rule
    //            (?:(?:(?:...(?:"""ARBITARY_REGEX_HERE"")...))) 
    //                             <---Simplifies To--->
    //                        (?:"""ARBITARY_REGEX_HERE"")
    //This is EXTREMLY hard to get right.
    public static string cleanRegexString(this string reStr) {
      StringBuilder cleanStr = new StringBuilder(reStr.Length) ;

      return cleanStr.ToString() ;
    }
  }
}
