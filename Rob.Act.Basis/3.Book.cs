using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Aid.Extension;

namespace Rob.Act.Gen
{
	using Path = Pathable ;
	public class Book : Aid.Collections.ObservableList<Path>.Filtered.Texted , Aid.Gettable<int,Path>
	{
		public string Subject { get ; protected set ; }
		public Book( string subject = null ) => Subject = subject ;
		public Path this[ DateTime date ] => this[p=>p.Date==date] ;
		public static Book operator+( Book book , Path path ) => book.Set(b=>path.Set(b.Add)) ;
		public static Book operator-( Book book , Path path ) => book.Set(b=>path.Set(b.Remove)) ;
		public static Book operator-( Book book , Predicate<Path> path ) => book.Set(b=>path.Set(b.Remove)) ;
		public static Book operator|( Book book , Path path ) => book.Set(b=>path.Set(p=>{ var i = b.IndexWhere(p.Match) ; if( i<0 ) b.Add(p) ; else if( b[i]!=p ) b[i].Adapt(p) ; })) ;
		public void Save( string path , string matter ) { if( new Act.Path(this.Min(a=>a.Date)){Dominant=true}.On(this.OfType<Act.Path>()).Set(matter.Compile<Action<Act.Path>>()).Set(a=>a.Origin=path.Pathin(a.Spec.ToFileName())) is Act.Path p && Entries.All(a=>a.Origin!=p.Origin||!p.Equals(a)) ) p.Origin.WriteAll((string)p) ; }
	}
	static class PathExtension
	{
		internal static bool Match( this Path x , Path y ) => x?.Origin!=null && y?.Origin!=null && x.Origin==y.Origin ;
	}
}
