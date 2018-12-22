using System;
using System.Linq;
using Aid.Extension;

namespace Rob.Act
{
	using Configer = System.Configuration.ConfigurationManager ;
	class Program
	{
		static Oper Operation = Oper.Combi|(Configer.AppSettings["Smoothing"]!=null?Oper.Smooth:Oper.Merge)|(Configer.AppSettings["Trimming"]!=null?Oper.Trim:Oper.Merge)|(Configer.AppSettings["Revaling"]!=null?Oper.Relat:Oper.Merge) ;
		static Oper Performed( string arg ) => Enum.GetNames(typeof(Oper)).Aggregate(Oper.Merge,(a,o)=>a|=arg.Consists(o,StringComparison.OrdinalIgnoreCase)?Oper.Trim:Oper.Merge) ;
		static void Main( string[] args )
		{
			Path path = null ; foreach( var part in args.Select(arg=>arg.ReadAllText().Internalize().Set(p=>p.Label=arg)*(Operation&~Performed(arg))).OrderBy(p=>p.Date) ) path |= part ;
			path.Label.Get(f=>$"{f.LeftFromLast('.')}.{(Operation&~Performed(f)).Stringy().Replace('|','.').ToLower()}.{f.RightFrom('.',true)}").Set(f=>f.WriteAll((path).Externalize(f.RightFrom('.')))) ;
		}
	}
}
