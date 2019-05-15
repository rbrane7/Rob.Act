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

namespace Rob.Act
{
	public class Book : Aid.Collections.ObservableList<Path>.Filtered.Texted , Aid.Gettable<int,Path>
	{
		public Book( string subject = null ) => Subject = subject ;
		public string Subject { get ; protected set ; }
		public Path this[ DateTime date ] => this.FirstOrDefault(p=>p.Date==date) ;
		public static Book operator+( Book book , Path path ) => book.Set(b=>path.Set(b.Add)) ;
		public static Book operator-( Book book , Path path ) => book.Set(b=>path.Set(b.Remove)) ;
		public static Book operator-( Book book , Predicate<Path> path ) => book.Set(b=>path.Set(b.Remove)) ;
		public static Book operator|( Book book , Path path ) => book.Set(b=>path.Set(p=>{ var i = b.IndexWhere(p.Match) ; if( i<0 ) b.Add(p) ; else if( b[i]!=p ) b[i] = p.Set(n=>n.Spectrum=b[i].Spectrum) ; })) ;
	}
	static class PathExtension
	{
		internal static bool Match( this Path x , Path y ) => x?.Spectrum.Origin!=null && y?.Spectrum.Origin!=null && x.Spectrum.Origin==y.Spectrum.Origin ;
	}
}
