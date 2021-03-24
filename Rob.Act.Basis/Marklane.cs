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
			public Quant Grane = 1e-4 ;
			public byte Vicination = 2 ;
			readonly IDictionary<(Frame Lon,Frame Lat),Core> Cash = new Dictionary<(Frame Lon,Frame Lat),Core>() ;
			readonly IDictionary<string,Core> Unic = new Dictionary<string,Core>() ;
			protected Core this[ Point point ] { get => this[point,null].core ; set { if( point.Geo is Geos p ) this[p] = value ; } }
			protected (Core core,bool geo) this[ Point point , bool?_ ] => point.Geo is Geos p && this[p] is Core c ? (c,true) : (Unic.By(point.Tags),false) ;
			Core this[ Geos point ] { get => this[point.Lon,point.Lat] ; set => this[point.Lon,point.Lat] = value ; }
			Core this[ Quant lon , Quant lat ] { get => Vicinity((lon.By(Grane),lat.By(Grane))).optimal(c=>(+(c.Geo-(lon,lat))).nil(v=>v>Grane*Vicination))?.one ; set => this[lon.By(Grane),lat.By(Grane)] = value ; }
			Core this[ Frame lon , Frame lat ] { get => this[(lon,lat)] ; set => this[(lon,lat)] = value ; }
			Core this[ (Frame lon,Frame lat) point ] { get => Cash.By(point) ; set { if( value==null ) { this[point].Set(c=>Unic.Remove(c.Tags)) ; Cash.Remove(point) ; } else Unic[value.Tags] = Cash[point] = value.Set(c=>c.Ori=point) ; } }
			IEnumerable<Core> Vicinity( (Frame Lon,Frame Lat) point ) { for( var i=-Vicination ; i<=Vicination ; ++i ) for( var j=-Vicination ; j<=Vicination ; ++j ) if( this[point.Lon+i,point.Lat+j] is Core s ) yield return s ; }
			#endregion
			#region Base
			/// <summary>
			/// Only points with Tags whic
			/// </summary>
			protected override bool Applicable( Point point ) => !( point.Tags.No() || point is Path ) && point.IsGeos ;
			bool Insensible( Sharable point ) => (point.Mark&Core.Globals)==Mark.No && !point.Tags.Contains(' ') ;
			/// <summary>
			/// Applies traits of point .
			/// </summary>
			/// <returns> True if there was any medium modification . </returns>
			protected override bool Applied( Point point )
			{
				if( this[point,null] is var res && res.core is Core core ) if( core.Equals(point)&&res.geo ) return false ; else core.Take(point,res.geo) ;
				else if( Insensible(point) ) return false ; else this[point] = new Core(this,point) ;
				return true ;
			}
			protected override void Clean() { using( Incognit ) foreach( var p in Cash.ToArray() ) if( Insensible(p.Value) ) { Unic.Remove(p.Value.Tags) ; Cash.Remove(p.Key) ; Dirty = true ; } }
			#endregion
			protected class Core : IEquatable<Point> , Sharable
			{
				Land Context ;
				public static readonly Mark Globals = Mark.Lap|Mark.Ato|Mark.Sub|Mark.Sup|Mark.Hyp|Mark.Act ;
				public Geos Geo ;
				public (Frame Lon,Frame Lat)? Ori { get => ori ; set { if( ori==value ) return ; if( ori is (Frame Lon,Frame Lat) o ) Context.Cash.Remove(o) ; ori = value ; } } (Frame Lon,Frame Lat)? ori ;
				public Mark Mark {get;private set;}
				public string Tags { get => tags ; private set { if( tags==value ) return ; tags.Set(s=>Context.Unic.Remove(s)) ; (tags=value).Set(s=>Context.Unic[s]=this) ; } } string tags ;
				public Core( Land context , Point point ) { Context = context ; point.Set(p=>Take(p,false)) ; }
				public bool Equals( Point point ) => Mark==(point.Mark&Globals) && Tags==point.Tags ;
				public void Take( Point point , bool geo ) { Mark = point.Mark&Globals ; Tags = point.Tags ; if( !geo ) { Ori = null ; Context[Geo=point.Geo.Value] = this ; } }
				public void Affect( (Point point,bool geo)_ ) { _.point.Mark = _.point.Mark&~Globals|(_.geo?Mark&Globals:Mark.No) ; _.point.Tags = _.geo ? Tags : null ; }
				public void Affect( IEnumerable<(Point point,bool geo)> points ) => points.Each(Affect) ;
			}
		}
	}
	public partial class Path
	{
		public class Marklane : Markage.Land
		{
			public static bool Persisting ;
			public static Quant Tolerancy = 1e-3 ;
			public static TimeSpan Independency = new TimeSpan(0,1,0) , Discrepancy = new TimeSpan(0,0,7) ;
			class Map : Dictionary<Core,IList<(Point point,bool geo)>> { public new IList<(Point point,bool geo)> this[ Core core ] { get => this.By(core) ; set => base[core] = value ; } }
			struct Pointery : Aid.Supcomparable<Pointery>
			{
				public Point The ; public bool Geo ; public Core Ori ;
				Quant Dist => +(The.Geo-Ori.Geo).Value ;
				public int? CompareTo( Pointery other ) =>
					!(Geo&&other.Geo) || (The.Date-other.The.Date).Abs()>Independency&&(The.Time-other.The.Time).Abs()>Discrepancy ? (int?)null : // relative time within action defines independency of two points
					(The.Tags==Ori.Tags)!=(other.The.Tags==Ori.Tags) ? The.Tags==Ori.Tags ? int.MinValue : int.MaxValue : // Tags defines first leve of affinity
					Dist.CompareTo(other.Dist).nil() ?? -1 ; // sharrp relation : only one of dependants will be selected
			}
			/// <summary>
			/// Takes traits of frommedium to path and points .
			/// </summary>
			public override void Interact( Path path )
			{
				try
				{
					Map map = null ; foreach( var point in path ) if( this[point,null] is var c && c.core is Core core ) ((map??=new Map())[core]??=new List<(Point point,bool geo)>()).Add((point,c.geo)) ;
					if( map!=null ) using( new Aid.Closure(()=>Blocked=true,()=>Blocked=false) )
					{
						using( path.Incognit ) foreach( var item in map )
						{
							var optimals = item.Value.OptimalOnes(p=>new Pointery{The=p.point,Geo=p.geo,Ori=item.Key}).ToList() ;
							for( var i=0 ; i<optimals.Count ; ++i ) if( optimals[i].geo || optimals.Any(c=>c.geo&&(c.point.Time-optimals[i].point.Time).Abs()<=Independency) ); else optimals.RemoveAt(i--) ;
							item.Key.Affect(optimals) ; // multiple optimal undertakes
						}
						//item.Value.Optimal(p=>p.Tags==item.Key.Tags?Quant.MinValue:+(p.Geo.Value-item.Key.Geo))?.one // single optimal undertakes
						if( Persisting ) path.Edited() ;
					}
				}
				catch( System.Exception e ) { System.Diagnostics.Trace.TraceError($"Failed to update{(Persisting?" or save":null)} {path} from medium Marklane !\n{e}") ; }
			}
			protected override bool Applicable( Point point ) => !Blocked && base.Applicable(point) ; bool Blocked ;
		}
	}
	static class MerklaneExtension
	{
		internal static Frame By( this Quant value , Quant grane ) => (Frame)Math.Round(value/grane) ;
		internal static TimeSpan Abs( this TimeSpan time ) => new TimeSpan(Math.Abs(time.Ticks)) ;
	}
}
