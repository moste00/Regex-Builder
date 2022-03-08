using System ;
using System.Text ;

namespace RegexBuilders {
  class Numerals : RegexBuilder {
    private uint? lowerlimit ;
    private uint? upperlimit ;

    public Numerals() : base() {
      lowerlimit = null ;
      upperlimit = null ;
    }
    public static Numerals numerals { get { return new Numerals() ; } }
    
    public Numerals between(uint low, uint high) {
      if(low < high) {
        lowerlimit = low ;
        upperlimit = high ;
      }
      else {
        upperlimit = low ;
        lowerlimit = high ;
      }
      return this ;
    }

    public Numerals greaterThan(uint low) {
      lowerlimit = low ;
      return this ;
    }

    public Numerals lesserThan(uint high) {
      upperlimit = high ;
      return this ;
    }

    protected override string selfToRegexString() {
      //(1) :-
      //no range limit
      //easy and common
      if(lowerlimit == null && upperlimit == null) {
          return "[0-9]+" ;
      }
      //(2) :-a
      //One range limit
      //much harder
      if(lowerlimit == null) { //only the upper limit exists
          return numeralsLessThanUpperWithLeadingZeros((uint)upperlimit);
      }
      //(2) :-b
      if(upperlimit == null) { //only the lower limit exist
          return numeralsGreaterThanLower((uint)lowerlimit);
      }
    
      //(3) :- 
      //Both range limits exist
      //Very Tricky 
      return numeralsBetweenLowerAndUpperWithLeadingZeros((uint)lowerlimit,(uint)upperlimit);
    }

    private static int charToDigit(char d) => (int)d - 48 ;
    
    private static string numeralsLessThanUpperWithLeadingZeros(uint upperlimit) {
          //------------------------------------------------------------------------------
          //Let the upper limit have n+1 digits d_n,...,d_0
          //The regex pattern we are constructing has the general form
          //           [0-<<<(d_n) - 1>>>][0-9]{n}
          //         | (d_n)R_(n-1)
          //Where R_i is a regex generated from d_i according to the following rules :
          //  R_0 = [0-<<<d_0>>>]
          //  R_i = [0-<<<(d_i) - 1>>>][0-9]{i}|(d_i)R_(i-1) 
          //        if d_i >= 1 else 
          //        0R_(i-1)
          //  for all i between n-1 and 1
          //------------------------------------------------------------------------------
          //for example, suppose you want all numbers less than 81314 inclusive
          // then n=4, d_n = 8,...,d_0 = 4
          // then the above regex template yields [0-7][0-9]{4}| 
          //                                      8(?:0[0-9]{3}|1
          //                                       (?:[0-2][0-9]{2}|3
          //                                       (?:0[0-9]|1[0-4])))
          // The first part (before the very first '|') matches numbers from 00000 to 79999
          // And the second takes it from 80000 up to 81314
          //------------------------------------------------------------------------------
          string upAsStr = upperlimit.ToString() ;
          int numDigits = upAsStr.Length ;
          int mostSignificantDigit = charToDigit(upAsStr[0]);

          //Annoying special case for n=0
          if(numDigits == 1) {
            if(mostSignificantDigit > 0) return "[0-"+ mostSignificantDigit +"]";
            return "0" ;
          }

          StringBuilder regexString = new StringBuilder();
          //[0-<<<(d_n) - 1>>>][0-9]{n}
          if(mostSignificantDigit > 1) {
            regexString.Append("[0-"+ (mostSignificantDigit-1) +"]");
          }
          else {
            regexString.Append("0");
          }
          regexString.Append("[0-9]");
          if(numDigits > 2) regexString.Append("{"+ (numDigits-1) +"}");

          //| (d_n)R_(n-1)
          regexString.Append("|"+mostSignificantDigit);

          //Another annoying special case
          if(numDigits == 2) {
            if(upAsStr[1] == '0') regexString.Append('0');
            else regexString.Append("[0-"+ (upAsStr[1]) +"]");
            
            return regexString.ToString() ;
          }

          //Now we can generate the R_is
          StringBuilder closingBrackets = new StringBuilder();
          for(int i = 1; i<numDigits-1; i++){
            int currDigit = charToDigit(upAsStr[i]) ;
            int remainingDigits = numDigits - i - 1 ;

            if(currDigit == 0) {
              regexString.Append("0");
              continue;
            }
            
            if (currDigit == 1) {
              regexString.Append("(?:0[0-9]");
            }
            else {
              regexString.Append("(?:[0-"+ (currDigit-1) +"][0-9]");
            }
          
            if(remainingDigits > 1) regexString.Append("{"+ remainingDigits +"}") ;
            regexString.Append("|"+currDigit);
            closingBrackets.Append(")");
            
          }
      
          if(upAsStr[numDigits - 1] == '0') regexString.Append("0");
          else regexString.Append("[0-"+ (upAsStr[numDigits-1]) +"]");

          regexString.Append(closingBrackets);
          return regexString.ToString();
    }

    private static string numeralsGreaterThanLower(uint lowerlimit){
      //Let the lowerlimit be expressed as a list of (n+1) digits
      //lo = [d_n,...,d_0]
      //A regex that specifies all numerals greater than lo has the following form
      //                        [0-9]{n+2,}
      //                       |
      //                        [<<<(d_n)+1>>>-9][0-9]{n} if d_n < 9 else nothing
      //                       |
      //                        d_nR_(n-1)
      //Where R_(n-1) is a regex generated from d_(n-1) by the following recurrence
      //                R_0 = [<<<d_0>>>-9]
      //                R_i = [<<<(d_i)+1>>>-9][0-9]{i}|d_iR_(i-1) 
      //                      if d_i < 9 else 
      //                      9R_(i-1)
      //--------------------------------------------------------------------------------------
      string loAsStr = lowerlimit.ToString();
      int numDigits = loAsStr.Length ;

      //[0-9]{n+2,}
      StringBuilder regexString = new StringBuilder("[0-9]{"+ (numDigits+1) +",}");

      //|[<<<(d_n)+1>>>-9][0-9]{n} if d_n < 9 else nothing
      if(loAsStr[0] != '9'){
        regexString.Append('|');
          
        if(loAsStr[0] == '8') regexString.Append('9');
        else {
          regexString.Append("["+ (charToDigit(loAsStr[0])+1) +"-9]");
        }
      
        if(numDigits > 1){
          regexString.Append("[0-9]");
          if(numDigits > 2) regexString.Append("{"+ (numDigits - 1) +"}");
        }
      }

      //|d_nR_(n-1)
      regexString.Append('|');
      regexString.Append(loAsStr[0]);

      //if n = 0, no valid R_i
      if(numDigits == 1) return regexString.ToString() ;
      //if n = 1, only valid R_i is R_0 
      if(numDigits == 2) {
        if(loAsStr[1] == '9') regexString.Append('9');
        else regexString.Append("["+ loAsStr[1] +"-9]");

        return regexString.ToString();
      }

      //Otherwise, go over string from second digit (d_(n-1), or loAsStr[1]) and calculate it's R
      StringBuilder closingBrackets = new StringBuilder();
      for(int i = 1; i<numDigits-1; i++){
        int remainingDigits = numDigits - i - 1 ;

        if(loAsStr[i] == '9'){
          regexString.Append('9');
          continue ;
        }
      
        regexString.Append("(?:");
        
        //R_i = [<<<(d_i)+1>>>-9][0-9]{i}|d_iR_(i-1)
        if(loAsStr[i] == '8'){
          regexString.Append('9');
        }
        else{
          regexString.Append("["+ (charToDigit(loAsStr[i])+1) +"-9]");
        }
        regexString.Append("[0-9]");
        if(remainingDigits > 1) regexString.Append("{"+ remainingDigits +"}");
                      
        regexString.Append('|');
        regexString.Append(loAsStr[i]);
                            
        closingBrackets.Append(")");
      }
      //final digit
      if(loAsStr[numDigits-1] == '9') regexString.Append('9');
      else regexString.Append("["+ loAsStr[numDigits-1] +"-9]");

      regexString.Append(closingBrackets);
      
      return regexString.ToString();
    }

    private static string numeralsBetweenLowerAndUpperWithLeadingZeros(uint lowerlimit,uint upperlimit){
      var loAsStr = lowerlimit.ToString() ;
      var upAsStr = upperlimit.ToString() ;
      var digitsDifference = upAsStr.Length - loAsStr.Length ;
  
      if(digitsDifference == 0){
        return numeralsBetweenLowerAndUpperSameNumDigits(loAsStr,upAsStr);
      }
    
      StringBuilder zeroExtendedLo = new StringBuilder();
      for(int i = 0; i<digitsDifference; i++) zeroExtendedLo.Append('0');
      zeroExtendedLo.Append(loAsStr);

      return numeralsBetweenLowerAndUpperSameNumDigits(zeroExtendedLo.ToString(),upAsStr);
    }

    private static string numeralsBetweenLowerAndUpperSameNumDigits(string loAsStr,string upAsStr){
        //let the the two numbers be the two digits lists
        //lo = [l_n,...,l_0]
        //up = [u_n,...,u_0]
        //Now suppose they share a common prefix of length 0 <= m <= n+1
        //That is, the equality 
        //                    l_i = u_i 
        //is true for all i from n to n-m+1 inclusive
        //(if m is 0 then no equality holds by definition)
        //Let that (possibly empty) shared prefix be called P
        //---------------------------------------------------------------------------------------------------------
        //Let the rest of the two numbers from digit k=n-m to digit 0 be represented as the two digits lists
        //lo_rest = [L_k,...,L_0]
        //up_rest = [U_k,...,U_0]
        //(if k is negative then there is no remainder of the two numbers by definition)
        //----------------------------------------------------------------------------------------------------------
        //The regex we want to construct is then as follows
        //                    [[A regex that matches P]]
        //                     THEN 
        //                        [[A regex that matches the numerals from [L_k,...,L_0]     to [L_k,9,...,9]     ]]
        //                        OR
        //                        [[A regex that matches the numerals from [(L_k)+1,0,...,0] to [(U_k)-1,9,...,9] ]]
        //                        OR
        //                        [[A regex that matches the numerals from [U_k,0,...,0]     to [U_k,...,U_0]     ]]
        //---------------------------------------------------------------------------------------------------------
        //Both the prefix regex and the second regex are easy, the first and the third can be obtained recursively 
        //As the first contains the common prefix L_k, while the third contains U_k
        //Therefore, after removing L_k and U_k, we have a smaller instance of the problem we started with
        //We reach a base case when the two limits are identical (e.g. 98,98), 
        //Then the regex that matches them is the prefix regex alone 
        //Another base case is when the two limits are single-digit
        //----------------------------------------------------------------------------------------------------------
        //For example, suppose we want to match a numeral between 1900 and 3100
        //The above formula tells us that regex should be as follows
        //                    [[No shared prefix, therefore no regex here]]
        //                      THEN
        //                    [[A regex that matches numerals from 1900 to 1999]]
        //                      THEN
        //                    [[A regex that matches numerals from 2000 to 2999]]
        //                      THEN
        //                    [[A regex that matches numerals from 3000 to 3100]]
        //For two numbers that share a prefix, then that prefix should simply be matched as-is at the very beginning
      
        var numDigits = loAsStr.Length ;
        // [[A regex that matches P]]
        int m = 0 ;
        while(m < numDigits && (upAsStr[m] == loAsStr[m])) {
          m++ ;
        }
        string prefixRegex = loAsStr.Substring(0,m);

        string upRest = upAsStr.Substring(m);
        string loRest = loAsStr.Substring(m);

        
        //Base case 1 : The limits were identical, the prefix_regex is any one of them
        if(String.IsNullOrEmpty(loRest)) return prefixRegex ;

        var restNumDigits = numDigits - m ; //at least 1, if m was numDigits we would have exited the line before
        int dlo = charToDigit(loRest[0]) ;
        int dup = charToDigit(upRest[0]) ;
        //Base case 2 : the remaining limits are single-digit, which means the problem is now reduced to a simple character range
        if(restNumDigits == 1) return prefixRegex +"["+ dlo +"-"+ dup +"]";
        
        
        //General case : recurse over the resulting strings
        StringBuilder L_kThen9s = new StringBuilder();
        L_kThen9s.Append(loRest[0]);
        for(int i = 0; i<restNumDigits-1; i++) L_kThen9s.Append('9');

        StringBuilder U_kThen0s = new StringBuilder();
        U_kThen0s.Append(upRest[0]);
        for(int i = 0; i<restNumDigits-1; i++) U_kThen0s.Append('0');


        StringBuilder regexString = new StringBuilder(prefixRegex);
        regexString.Append("(?:");
      
        //[[A regex that matches the numerals from [L_k,...,L_0]     to [L_k,9,...,9]     ]]
        regexString.Append(
            numeralsBetweenLowerAndUpperSameNumDigits(loRest,
                                                      L_kThen9s.ToString()));
        //[[A regex that matches the numerals from [(L_k)+1,0,...,0] to [(U_k)-1,9,...,9] ]]
        if(dup - dlo > 1){
          regexString.Append("|");
          if(dup - dlo == 2) regexString.Append(dlo+1);
          else regexString.Append("["+ (dlo+1) +"-"+ (dup-1) +"]");
          
          regexString.Append("[0-9]");
          
          if(restNumDigits > 2) regexString.Append("{"+ (restNumDigits-1) +"}");
        }
        //[[A regex that matches the numerals from [U_k,0,...,0]     to [U_k,...,U_0]     ]]
        regexString.Append("|");
        regexString.Append(
            numeralsBetweenLowerAndUpperSameNumDigits(U_kThen0s.ToString(),
                                                      upRest));
        regexString.Append(")");
      
        return regexString.ToString();
    }
  }
}