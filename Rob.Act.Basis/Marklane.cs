using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	using Frame = Int32 ;

	public class Markage
	{
		public abstract class Land : Medium
		{
			#region Core
			public const Quant Grane = 1e-4 ;
			public static byte VicinityRad = 1 ;
			readonly Dictionary<(Frame Lon,Frame Lat),Core> Cash = new Dictionary<(Frame Lon,Frame Lat),Core>() ;
			public byte Radius ;
			protected Core this[ Geos? point ] { get => point is Geos p ? this[p.Lon,p.Lat] : null ; set { if( point is Geos p && value is Core v ) this[p.Lon,p.Lat] = v ; } }
			Core this[ Quant lon , Quant lat ] { get => Vicinity(((Frame)(lon/Grane),(Frame)(lat/Grane))).Best(c=>+(c.Geo-(lon,lat)))?.one ; set { if( value is Core ) this[(Frame)(lon/Grane),(Frame)(lat/Grane)] = value ; } }
			Core this[ Frame lon , Frame lat ] { get => this[(lon,lat)] ; set => this[(lon,lat)] = value ; }
			Core this[ (Frame lon,Frame lat) point ] { get => Cash.By(point) ; set => Cash[point] = value ; }
			IEnumerable<Core> Vicinity( (Frame Lon,Frame Lat) point ) { for( var i=-VicinityRad ; i<=VicinityRad ; ++i ) for( var j=-VicinityRad ; j<=VicinityRad ; ++j ) if( this[((short)(point.Lon+i),(short)(point.Lat+j))] is Core s ) yield return s ; }
			#endregion
			#region Base
			protected override bool Applicable( Point point ) => !point.Tags.No() && point.IsGeo ;
			/// <summary>
			/// Applies traits of point .
			/// </summary>
			/// <returns> True if there was any medium modification . </returns>
			protected override bool Applied( Point point )
			{
				if( point.Geo is Geos geo ); else return false ;
				if( this[geo] is Core core ) if( core.Equals(point) ) return false ; else core.Take(point) ; else this[geo] = new Core(point) ;
				return true ;
			}
			#endregion
			protected class Core : IEquatable<Point>
			{
				static readonly Mark Globals = Mark.Ato|Mark.Sub|Mark.Sup|Mark.Hyp ;
				public Geos Geo ; public Mark Mark ; public string Tags ;
				public Core( Point point ) => point.Set(Take).Set(p=>Geo=p.Geo.Value) ;
				public bool Equals( Point point ) => Mark==(point.Mark&Globals) && Tags==point.Tags ;
				public void Take( Point point ) { Mark = point.Mark&Globals ; Tags = point.Tags ; }
				public void Affect( Point point ) { if( point is Point p ); else return ; p.Mark = p.Mark&~Globals|Mark&Globals ; p.Tags = Tags ; }
			}
		}
	}
	public partial class Path
	{
		public class Marklane : Markage.Land
		{
			class Map : Dictionary<Core,IList<Point>> { public new IList<Point> this[ Core core ] { get => this.By(core) ; set => this[core] = value ; } }
			/// <summary>
			/// Takes traits of frommedium to path and points .
			/// </summary>
			public override void Interact( Path path )
			{
				Map map = null ; path.Each(p=>this[p].Set(c=>((map??=new Map())[c]??=new List<Point>()).Add(p))) ; if( map==null ) return ;
				foreach( var item in map ) item.Key.Affect(item.Value.Best(p=>+(p.Geo.Value-item.Key.Geo))?.one) ;
			}
		}
	}
	static class MerklaneExtension
	{
	}
}
