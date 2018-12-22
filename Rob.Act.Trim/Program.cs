using System;
using System.Linq;
using Aid.Extension;
using System.Text.RegularExpressions;

namespace Rob.Act
{
	using Configer = System.Configuration.ConfigurationManager ;
	class Program
	{
		static Regex MainTag = Configer.AppSettings["Path.Main.Regex"].Get(r=>new Regex(r)) ;
		static int IsMain( string file ) => /*-MainTag.Match(file).Groups.Count*/ MainTag?.Match(file).Success==true?0:1 ;
		static Oper Operation = (Configer.AppSettings["Combining"]!=null?Oper.Combi:Oper.Merge)|(Configer.AppSettings["Smoothing"]!=null?Oper.Smooth:Oper.Merge)|(Configer.AppSettings["Trimming"]!=null?Oper.Trim:Oper.Merge)|(Configer.AppSettings["Relating"]!=null?Oper.Relat:Oper.Merge) ;
		static Oper Performed( string arg ) => Enum.GetNames(typeof(Oper)).Aggregate(Oper.Merge,(a,o)=>a|=arg.Consists(o,StringComparison.OrdinalIgnoreCase)?Oper.Trim:Oper.Merge) ;
		static void Main( string[] args )
		{
			Path path = null ; args = args.OrderBy(p=>IsMain(p)).ToArray() ;
			foreach( var arg in args ) path |= ( arg.ReadAllText().Internalize() & path.Null(p=>Operation.HasFlag(Oper.Combi)||arg.Consists(Oper.Combi.Stringy(),StringComparison.OrdinalIgnoreCase)) ) * (Operation&~Performed(arg)) ;
			args.At(0).Get(f=>$"{f.LeftFromLast('.')}.{(Operation&~Performed(f)).Stringy().Replace('|','.').ToLower()}.{f.RightFrom('.',true)}").Set(f=>f.WriteAll((path).Externalize(f.RightFrom('.')))) ;
		}
	}
}
