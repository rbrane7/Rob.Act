using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using System.Collections;
	using Quant = Double;
	public class Altiplane
	{
		public const Quant Locus = 1e-2 , Factor = 1e5 ;
		public const string FileSign = "Alti" , ArgSep = "-" , ExtSign = ".alp" ;
		public const string DateForm = "yyyyMMdd.HHmmss" ;
		public static byte VicinityRad = 1 , HitWeight = 10 ;
		public readonly Quant Grane ;
		readonly Dictionary<(short Lon,short Lat),Segment> Cash = new Dictionary<(short Lon,short Lat),Segment>() ;
		public byte Radius ;
		readonly ISet<DateTime> Dates = new HashSet<DateTime>() ;
		public Altiplane( Quant? grane = null ) => Grane = (grane??10).nil()??1 ;
		public bool Include( DateTime date ) => Dates.Add(date) ;
		public Quant? this[ Geos? point , byte? radius = null ] { get => point is Geos p ? this[p.Lon,p.Lat,radius] : null ; set { if( point is Geos p && value is Quant v ) this[p.Lon,p.Lat] = v ; } }
		#region Calculus
		public Quant? this[ Quant lon , Quant lat , byte? rad = null ]
		{
			get => Vicinity(((short)(lon/Locus),(short)(lat/Locus))).Close(((short)(lon%Locus*Factor),(short)(lat%Locus*Factor)),(rad??Radius)*Grane).Calculate() ;
			set { if( value is Quant alt ) Insure(((short)(lon/Locus),(short)(lat/Locus))).Join(((short)((short)(lon%Locus*Factor/Grane)*Grane),(short)((short)(lat%Locus*Factor/Grane)*Grane),(short)alt,1)) ; }
		}
		IEnumerable<(short Lon,short Lat,short Alt,ushort Wei)> Vicinity( (short Lon,short Lat) point )
		{
			for( var i=-VicinityRad ; i<=VicinityRad ; ++i ) for( var j=-VicinityRad ; j<=VicinityRad ; ++j ) if( Cash.By(((short)(point.Lon+i),(short)(point.Lat+j))) is Segment s )
				foreach( var ap in s ) yield return ((short)(ap.Lon+i*Locus*Factor),(short)(ap.Lat+j*Locus*Factor),ap.Alt,ap.Wei) ;
		}
		Segment Insure( (short Lon,short Lat) point ) => Cash.By(point) ??( Cash[point] = new Segment() ) ;
		#endregion
		#region Serialization
		public virtual void Save( string file )
		{
			using var wrt = new System.IO.StreamWriter(file) ; var space = false ; foreach( var date in Dates ) { if( space ) wrt.Write(' ') ; wrt.Write(date.ToString(DateForm)) ; space = true ; }
			foreach( var seg in Cash ) { wrt.WriteLine() ; wrt.Write($"{seg.Key.Lon},{seg.Key.Lat} ") ; foreach( var point in seg.Value ) wrt.Write($"{point.Lon},{point.Lat}:{point.Alt}*{point.Wei};") ; }
		}
		public Altiplane( string file = null )
		{
			Grane = file.RightFrom(ArgSep).LeftFrom(ExtSign).Parse(0D) ;
			if( System.IO.File.Exists(file) ) using( var rdr = new System.IO.StreamReader(file) )
			{
				rdr.ReadLine().Separate(' ',false).Each(d=>Dates.Add(d.Parse<DateTime>(DateForm).Value)) ;
				for( string seg ; !(seg=rdr.ReadLine()).No() ; )
				{
					var val = Cash[seg.LeftFrom(' ').get(s=>(s.LeftFrom(',').Parse<short>().Value,s.RightFrom(',').Parse<short>().Value)).Value] = new Segment() ;
					foreach( var point in seg.RightFrom(' ').Separate(';',false) )
					{
						var key = point.LeftFrom(':') ; var value = point.RightFrom(':') ;
						val.Add((key.LeftFrom(',').Parse<short>().Value,key.RightFrom(',').Parse<short>().Value,value.LeftFrom('*').Parse<short>().Value,value.RightFrom('*').Parse<ushort>().Value)) ;
					}
				}
			}
		}
		#endregion
		class Segment : List<(short Lon,short Lat,short Alt,ushort Wei)>
		{
			public void Join( (short Lon,short Lat,short Alt,ushort Wei) point )
			{
				var i = this.IndexWhere(c=>c.Lon==point.Lon&&c.Lat==point.Lat) ;
				if( i<0 ) Add(point) ; else { var (Lon,Lat,Alt,Wei) = this[i] ; Alt = (short)((Alt*Wei+point.Alt*point.Wei)/(Wei+point.Wei)) ; Wei += point.Wei ; this[i] = (Lon,Lat,Alt,Wei) ; }
			}
		}
	}
	public partial class Path
	{
		public class Altiplane : Act.Altiplane
		{
			public bool Dirty { get ; private set ; }
			public readonly Quant Grade ;
			public Altiplane( Quant grade , Quant grane = 10 ) : base(grane) => Grade = grade ;
			public void Include( Path path )
			{
				if( path==null || !Include(path.Date) ) return ; Dirty = true ;
				for( var i=0 ; i<path.Count ; ++i )
				{
					var alt = path[i].Alti ;
					//for( var j=Math.Max(0,i-170) ; j<Math.Min(i+171,path.Count) ; ++j ) if( path[i].Alti is Quant a && path[j].Alti is Quant b && Math.Abs(a-b)>Grade*(path[i]-path[j]).Euclid(path[i]) ) alt = null ;
					this[path[i].Geo] = alt ;
				}
			}
			public Altiplane( string file ) : base(file) => Grade = file.RightFrom(FileSign).LeftFrom(ArgSep).Parse(.999) ;
			public override void Save( string file ) { base.Save(file) ; Dirty = false ; }
		}
	}
	static class AltiExtension
	{
		internal static IEnumerable<(short Alt,Quant Dis,ushort Wei)> Close( this IEnumerable<(short Lon,short Lat,short Alt,ushort Wei)> vicinity , (short Lon,short Lat) point , Quant limit )
		{
			foreach( var cand in vicinity ) if( cand.Dist(point) is Quant d && d<limit ) yield return (cand.Alt,d.nil()??1D/Altiplane.HitWeight,cand.Wei) ;
		}
		internal static Quant? Calculate( this IEnumerable<(short Alt,Quant Dis,ushort Wei)> cand ) { var ar = cand.ToArray() ; return ar.Length>0 && ar.Sum(c=>c.Wei/c.Dis) is Quant n ? ar.Sum(c=>c.Alt*c.Wei/c.Dis/n) : null as Quant? ; }
		static int Sqr( this int val ) => val*val ;
		static Quant Dist( this (short Lon,short Lat,short Alt,ushort Wei) x , (short Lon,short Lat) y ) => Math.Sqrt((x.Lon-y.Lon).Sqr()+(x.Lat-y.Lat).Sqr()) ;
	}
}
