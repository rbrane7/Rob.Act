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
	public class Mediator
	{
		public const Quant Locus = 1e-2 , Factor = 1e5 ;
		public const string FileSign = "Alti" , ArgSep = "-" , ExtSign = ".alp" ;
		public const string DateForm = "yyyyMMdd.HHmmss" ;
		public static byte VicinityRad = 1 , HitWeight = 10 ;
		public readonly Quant Grane ;
		readonly Dictionary<(short Lon,short Lat),Segment> Cash = new Dictionary<(short Lon,short Lat),Segment>() ;
		public byte Radius ;
		public bool Dirty {get;private set;}
		public Mediator( Quant? grane = null ) => Grane = (grane??10).nil()??1 ;
		/// <summary>
		/// Applies traites .
		/// </summary>
		public void Interact( Point point ) { if( Dirty = Modifies(point) ); else return ; }
		bool Modifies( Point point ) => false ;
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
			using var wrt = new System.IO.StreamWriter(file) ;
			foreach( var seg in Cash ) { wrt.WriteLine() ; wrt.Write($"{seg.Key.Lon},{seg.Key.Lat} ") ; foreach( var point in seg.Value ) wrt.Write($"{point.Lon},{point.Lat}:{point.Alt}*{point.Wei};") ; }
		}
		public Mediator( string file = null )
		{
			Grane = file.RightFrom(ArgSep).LeftFrom(ExtSign).Parse(0D) ;
			if( System.IO.File.Exists(file) ) using( var rdr = new System.IO.StreamReader(file) )
			{
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
		public class Mediator : Act.Mediator
		{
			public readonly Quant Grade ;
			public Mediator( Quant grade , Quant grane = 10 ) : base(grane) => Grade = grade ;
			/// <summary>
			/// Takes traits .
			/// </summary>
			public void Interact( Path path )
			{
				for( var i=0 ; i<path?.Count ; ++i )
				{
					var alt = path[i].Alti ;
					//for( var j=Math.Max(0,i-170) ; j<Math.Min(i+171,path.Count) ; ++j ) if( path[i].Alti is Quant a && path[j].Alti is Quant b && Math.Abs(a-b)>Grade*(path[i]-path[j]).Euclid(path[i]) ) alt = null ;
					this[path[i].Geo] = alt ;
				}
			}
			public Mediator( string file ) : base(file) => Grade = file.RightFrom(FileSign).LeftFrom(ArgSep).Parse(.999) ;
		}
	}
	static class MediExtension
	{
	}
}
