using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
			readonly IDictionary<(Frame Lon,Frame Lat),Nib> Cash = new Dictionary<(Frame Lon,Frame Lat),Nib>() ;
			readonly IDictionary<string,Nib> Unic = new Dictionary<string,Nib>() ;
			protected Nib this[ Point point ] { get => this[point,null].nib ; set { if( point.Geo is Geos p ) this[p] = value ; } }
			protected (Nib nib,bool geo) this[ Point point , byte? vici = null ] => point.Geo is Geos p && this[p,vici] is Nib c ? (c,true) : (Unic.By(point.Tags),false) ;
			/// <summary>
			/// <see cref="Nib"/> <see cref="Markage"/> afiine to <paramref name="point"/> from the persepctive of <see cref="Land"/> .
			/// </summary>
			/// <param name="point"> <see cref="Geos"/> point to get close <see cref="Markage"/> to </param>
			/// <param name="vici"> Vicinity spec </param>
			/// <returns> Closest <see cref="Nib"/> to <paramref name="point"/> , if available </returns>
			Nib this[ Geos point , byte? vici = null ] { get => this[point.Lon,point.Lat,vici] ; set => this[point.Lon,point.Lat] = value ; }
			Nib this[ Quant lon , Quant lat , byte? vici = null ] { get => Vicinity((lon.By(Grane),lat.By(Grane)),vici).optimal(c=>(+(c.Geo-(lon,lat))).nil(v=>v>Grane*(vici??Vicination)))?.one ; set => this[lon.By(Grane),lat.By(Grane)] = value ; }
			Nib this[ Frame lon , Frame lat ] { get => this[(lon,lat)] ; set => this[(lon,lat)] = value ; }
			Nib this[ (Frame lon,Frame lat) point ] { get => Cash.By(point) ; set { if( value==null ) { this[point].Set(c=>Unic.Remove(c.Tags)) ; Cash.Remove(point) ; } else Unic[value.Tags] = Cash[point] = value.Set(c=>c.Ori=point) ; } }
			IEnumerable<Nib> Vicinity( (Frame Lon,Frame Lat) point , byte? vici = null ) { var vic = vici??Vicination ; for( var i=-vic ; i<=vic ; ++i ) for( var j=-vic ; j<=vic ; ++j ) if( this[point.Lon+i,point.Lat+j] is Nib s ) yield return s ; }
			#endregion
			#region Base
			/// <summary>
			/// Only points with Tags whic
			/// </summary>
			protected override bool Applicable( Point point ) => !point.Tags.No() && point is not Path && point.IsGeos ;
			bool Insensible( Sharable point ) => (point.Mark&Nib.Globals)==Mark.No && !point.Tags.Contains(' ') || point.Tags[0]==' ' ;
			/// <summary>
			/// Applies traits of point .
			/// </summary>
			/// <returns> True if there was any medium modification . </returns>
			protected override bool Applied( Point point )
			{
				if( this[point,null] is var res && res.nib is Nib nib ) if( nib.Equals(point)&&res.geo || !point.Mark.HasFlag(Mark.Own) ) return false ; else nib.Take(point,res.geo) ;
				else if( Insensible(point) ) return false ; else this[point] = new Nib(this,point) ;
				return true ;
			}
			protected override void Clean() { using( Incognit ) foreach( var p in Cash.ToArray() ) if( Insensible(p.Value) ) { Unic.Remove(p.Value.Tags) ; Cash.Remove(p.Key) ; Dirty = true ; } }
			#endregion
			/// <summary>
			/// <see cref="Markage"/> element specifies geografic point and its markage attributes 
			/// </summary>
			protected class Nib : IEquatable<Point> , Sharable
			{
				readonly Land Context ;
				public static readonly Mark Globals = Mark.Lap|Mark.Ato|Mark.Sub|Mark.Sup|Mark.Hyp|Mark.Act|Mark.Aim ;
				public Geos Geo , Aim ;
				public (Frame Lon,Frame Lat)? Ori { get => ori ; set { if( ori==value ) return ; if( ori is (Frame Lon,Frame Lat) o ) Context.Cash.Remove(o) ; ori = value ; } } (Frame Lon,Frame Lat)? ori ;
				public Mark Mark {get;private set;}
				public string Tags { get => tags ; private set { if( tags==value ) return ; tags.Set(s=>Context.Unic.Remove(s)) ; (tags=value).Set(s=>Context.Unic[s]=this) ; } } string tags ;
				public Nib( Land context , Point point ) { Context = context ; point.Set(p=>Take(p,false)) ; }
				public bool Equals( Point point ) => Mark==(point.Mark&Globals) && Tags==point.Tags ;
				public void Take( Point point , bool geo )
				{
					var reaim = Mark.HasFlag(Mark.Aim)!=point.Mark.HasFlag(Mark.Aim) ; Mark = point.Mark&Globals ; Tags = point.Tags ;
					if( !geo ) { Ori = null ; Context[Geo=point.Geo.Value] = this ; Reaim(point) ; } else if( reaim ) Reaim(point) ;
				}
				void Reaim( Point point ) { if( Mark.HasFlag(Mark.Aim) ) Aim = point.Aim??default ; else Aim = default ; }
				public void Affect( (Point point,bool geo)_ ) { _.point.Mark = _.point.Mark&~Globals|(_.geo?Mark&Globals:Mark.No) ; _.point.Tags = _.geo ? Tags : null ; }
				public void Affect( IEnumerable<(Point point,bool geo)> points ) => points.Each(Affect) ;
				public override string ToString() => $"{base.ToString()} {Mark} : {Tags} : Geo={Geo}{Aim.nil().Get(a=>$"+Aim={a}")} Ori={Ori}" ;
			}
		}
	}
	public partial class Path
	{
		public class Marklane : Markage.Land
		{
			public static bool Persisting ;
			public static Quant Tolerancy = 1e-3 ;
			public static TimeSpan Independency = new(0,1,0) , Discrepancy = new(0,0,7) ;
			class Map : Dictionary<Nib,List<(Point point,bool geo)>> { public new List<(Point point,bool geo)> this[ Nib core ] { get => this.By(core) ; set => base[core] = value ; } }
			struct Pointery : Aid.Supcomparable<Pointery>
			{
				/// <summary>
				/// Point of which criterion is this 
				/// </summary>
				public Point The ;
				public bool Geo ;
				/// <summary>
				/// Registered Markage to get distance from 
				/// </summary>
				public Nib Ori ;
				/// <summary>
				/// Coordinates distance . Flat 2D distance 
				/// </summary>
				Quant Dist => +(The.Geo-Ori.Geo).Value ;
				/// <summary>
				/// Compare of this instance with <paramref name="other"/> giving distance from <see cref="Ori"/> mark-point 
				/// </summary>
				/// <returns> This is closer to <see cref="Ori"/> than <paramref name="other"/> : -1 ; This is equal to <paramref name="other"/> by distance to <see cref="Ori"/> : 0 ; otherwise : 1 </returns>
				public int? CompareTo( Pointery other ) => Incomparable(other) ? null : Distant(other) ? The.Tags==Ori.Tags ? int.MinValue : int.MaxValue : Compare(other) ;
				/// <summary>
				/// relative time within action defines independency of two points and excludes point from candidates
				/// </summary>
				bool Incomparable( Pointery other ) => !(Geo&&other.Geo) || (The.Date-other.The.Date).Abs()>Independency&&(The.Time-other.The.Time).Abs()>Discrepancy ;
				/// <summary>
				/// Tags defines first level of affinity 
				/// </summary>
				bool Distant( Pointery other ) => (The.Tags==Ori.Tags)!=(other.The.Tags==Ori.Tags) && Dist>0!=other.Dist>0 ;
				/// <summary>
				/// sharp relation : only one of dependants will be selected : distance and then date diference (earlier has precedence) defines criterion 
				/// </summary>
				Frame Compare( Pointery other ) => Dist.CompareTo(other.Dist).nil() ?? The.Date.CompareTo(other.The.Date) ;
				public override string ToString() => $"{base.ToString()} {The.Geo}+{Dist} {The.Mark} : {The.Tags} : {Ori}" ;
			}
			/// <summary>
			/// Takes traits of from medium to path and points 
			/// </summary>
			public override void Interact( Path path )
			{
				try
				{
					Map map = null ; Geos? lap = null ; var vici = path.Vicination ; foreach( var point in path )
					{
						if( this[point,vici] is var c && c.nib is Nib nib && (nib.Aim==default||(nib.Aim&point.Aim)>0.5) ) ((map??=new Map())[nib]??=new()).Add((point,c.geo)) ;
						lap = point.Geo ; // Each point which hase affine Nib is assigned to that Nib candidate set , just according to it's geo coordinates
					}
					if( map is not null ) using( new Aid.Closure(()=>Blocked=true,()=>Blocked=false) )
					{
						using( path.Incognit ) foreach( var item in map )
						{
							var optimals = item.Value.OptimalOnes(p=>new Pointery{The=p.point,Geo=p.geo,Ori=item.Key}).ToList() ; // Refining candidates by it's distance from Nib to just closest ones
							for( var i=0 ; i<optimals.Count ; ++i ) if( optimals[i].geo || optimals.Any(c=>c.geo&&(c.point.Time-optimals[i].point.Time).Abs()<=Independency) ); else optimals.RemoveAt(i--) ;
							item.Key.Affect(optimals) ; // multiple optimal undertakes
						}
						//item.Value.Optimal(p=>p.Tags==item.Key.Tags?Quant.MinValue:+(p.Geo.Value-item.Key.Geo))?.one // single optimal undertakes , not intended to use , legacy
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
		internal static TimeSpan Abs( this TimeSpan time ) => new(Math.Abs(time.Ticks)) ;
	}
}
