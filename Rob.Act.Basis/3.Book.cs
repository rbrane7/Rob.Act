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
		public Path this[ DateTime date ] => this.FirstOrDefault(p=>p.Date==date) ;
		public static Book operator+( Book book , Path path ) => book.Set(b=>path.Set(b.Add)) ;
		public static Book operator-( Book book , Path path ) => book.Set(b=>path.Set(b.Remove)) ;
		public static Book operator-( Book book , Predicate<Path> path ) => book.Set(b=>path.Set(b.Remove)) ;
		public static Book operator|( Book book , Path path ) => book.Set(b=>path.Set(p=>{ var i = b.IndexWhere(p.Match) ; if( i<0 ) b.Add(p) ; else if( b[i]!=p ) b[i].Adopt(p) ; })) ;
		public void Save( string path , string matter ) { if( path!=null && Count>0 && matter.Compile<Func<Path,Path>>()(new Act.Path(this.Min(a=>a.Date),this.OfType<Act.Path>()){Dominant=true}.Set(a=>a.Origin=path.Pathin(a.Spec))) is Act.Path p && !p.Equals(Entries.SingleOrNo(a=>a.Origin==p.Origin)) ) p.Origin.WriteAll((string)p) ; }
	}
	static class PathExtension
	{
		internal static bool Match( this Path x , Path y ) => x?.Origin!=null && y?.Origin!=null && x.Origin==y.Origin ;
	}
}
