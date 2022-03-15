using System;
using RegexBuilders;
using System.Text.RegularExpressions ;

using static RegexBuilders.LiteralString ;
using static RegexBuilders.WhiteSpace ;
using static RegexBuilders.Numerals ;


class Program {
  //TODO
  //1- Add negative numbers in range limits for Numerals
  //2- Add the capability to specify non-necessity of leading zeros 
  //(e.g. if the constraint is Numerals().lesserThan(314), then user can choose that 0 is an acceptable match, currently only 000 is)
  //4- Add the capability to wrap a regex tree in a name, that would translate to a numbered capturing group
  //The library have the responsibility to translate between user names and indices for capture groups (Use a context object ?)
  //5- Add the capability to wrap regex trees in a (capturing or noncapturing) groups 
  //6- Add a cleaning method for the resulting regex string, as it's extremly ugly.
  //For example, the generated regex strings often contain patterns like (?:(?:(?:....)))
  //One idea for cleaning could be : Replace the previous patterns with just (?:....)
  //(be very careful not to modify the semantics of the string)
  //7- Add min-max quantification 
  //(x{2,3} matches either xx or xxx.)
  //If the min limit is left out, it's assumed 0 )
  //If the max limit is left out, it's assumed infinity
  //That is, x{,} is equivalent to x*
  public static void Main(string[] args) {
    var seperatorP = literally("-")
                    .or("/")
                    .or("\\")
                    .or(Whitespace.manyTimesRepeated);
    
    var dateP =  
              numerals.between(1900,2010)
             .then(seperatorP)
             .then(
               numerals.between(1,12)
             )
             .then(seperatorP)
             .then(
               numerals.between(1,31)
             );

    var xyP = dateP.then(dateP).or(dateP).then(dateP).manyTimesRepeated.then(dateP).toRegexString();    
    
    Console.WriteLine(xyP.cleanRegexString()) ;
    Console.WriteLine("--------------------------------------------------------");
    Console.WriteLine() ;
    Console.WriteLine() ;
    Console.WriteLine(xyP);
  }
}
//(?:(?:(?:(?:xyz|w(?:omega)))))
//(?:xyz|w(?:omega)