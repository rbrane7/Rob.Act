using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Aid.Extension;

namespace Rob.Act
{
	using Configer = System.Configuration.ConfigurationManager ;
	using Quant = Double ;
	/// <summary>
	/// Kind of separation marks .
	/// </summary>
	[Flags] public enum Mark { No = 0 , Stop = 1 , Lap = 2 , Act = 4 }
	[Flags] public enum Oper { Merge = 0 , Combi = 1 , Trim = 2 , Smooth = 4 , Relat = 8 }
	public enum Axis : uint { Lon , Longitude = Lon , Lat , Latitude = Lat , Alt , Altitude = Alt , Dist , Distance = Dist , Drag , Flow , Beat , Bit , Ergy , Energy = Ergy , Effort , Time }
	public struct Bipole : IFormattable
	{
		Bipole( Quant a , Quant b ) { A = Math.Abs(a) ; B = -Math.Abs(b) ; }
		public Bipole( Quant a ) { A = Math.Max(0,a) ; B = Math.Min(0,a) ; }
		public Quant A { get; }
		public Quant B { get; }
		public static Quant operator+( Bipole x ) => x.A-x.B ;
		public static Bipole operator-( Bipole x ) => new Bipole(x.B,x.A) ;
		public static Bipole operator+( Bipole x , Bipole y ) => new Bipole(x.A+y.A,x.B+y.B) ;
		public static Bipole operator-( Bipole x , Bipole y ) => x+-y ;
		public static Bipole operator+( Bipole x , Quant y ) => x+(Bipole)y ;
		public static Bipole operator-( Bipole x , Quant y ) => x-(Bipole)y ;
		public static Bipole operator*( Bipole x , Quant y ) => y>=0 ? new Bipole(x.A*y,x.B*y) : -x*-y ;
		public static Bipole? operator/( Bipole x , Quant y ) => y>0 ? new Bipole(x.A/y,x.B/y) : y<0 ? -x/-y : null as Bipole? ;
		public static Bipole operator*( Bipole x , Bipole y ) => x*(Quant)y ;
		public static Bipole? operator/( Bipole x , Bipole y ) => x/(Quant)y ;
		public static Bipole? operator+( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value+y.Value : null as Bipole? ;
		public static Bipole? operator-( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value-y.Value : null as Bipole? ;
		public static Bipole? operator+( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value+y.Value : null as Bipole? ;
		public static Bipole? operator-( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value-y.Value : null as Bipole? ;
		public static Bipole? operator*( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value*y.Value : null as Bipole? ;
		public static Bipole? operator/( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value/y.Value : null as Bipole? ;
		public static Bipole? operator*( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value*y.Value : null as Bipole? ;
		public static Bipole? operator/( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value/y.Value : null as Bipole? ;
		public static implicit operator Bipole( Quant v ) => new Bipole(v) ;
		public static explicit operator Quant( Bipole v ) => v.A+v.B ;
		public override string ToString() => $"{A}-{-B}" ;
		public string ToString( string format , IFormatProvider formatProvider ) => $"{A.ToString(format,formatProvider)}-{(-B).ToString(format,formatProvider)}" ;
	}
	public struct Geos
	{
		public Quant Lon , Lat ;
		public static Geos operator~( Geos a ) => new Geos{Lon=a.Lat,Lat=-a.Lon} ;
		public static Quant operator+( Geos a ) => Math.Sqrt(a|a) ;
		public static Geos? operator~( Geos? a ) => a.use(x=>~x) ;
		public static Quant? operator+( Geos? a ) => a.use(x=>+x) ;
		public static Geos operator+( Geos a , Geos b ) => new Geos{Lon=a.Lon+b.Lon,Lat=a.Lat+b.Lat} ;
		public static Geos operator-( Geos a , Geos b ) => new Geos{Lon=a.Lon-b.Lon,Lat=a.Lat-b.Lat} ;
		public static Geos? operator+( Geos? a , Geos? b ) => a is Geos x && b is Geos y ? x+y : null as Geos? ;
		public static Geos? operator-( Geos? a , Geos? b ) => a is Geos x && b is Geos y ? x-y : null as Geos? ;
		public static Quant operator|( Geos a , Geos b ) => a.Lon*b.Lon+a.Lat*b.Lat ;
		public static Quant? operator|( Geos? a , Geos? b ) => a is Geos x && b is Geos y ? x|y : null as Quant? ;
		public static implicit operator Geos?( Point point ) => point?.IsGeo==true ? new Geos{Lon=point[Axis.Lon].Value,Lat=point[Axis.Lat].Value} : null as Geos? ;
	}
	public interface Quantable : Aid.Gettable<uint,Quant?> , Aid.Gettable<Quant?> {}
	public interface Pointable : Quantable , Aid.Accessible<uint,Quant?> , Aid.Accessible<Quant?> { DateTime Date { get; } TimeSpan Time { get; } uint Dimension { get; } string Action { get; } Mark Mark { get; } void Adopt( Pointable path ) ; }
	public interface Pathable : Pointable , Aid.Countable , Aid.Gettable<DateTime,Pointable> , Aid.Gettable<int,Pointable> { string Origin { get; } Path.Aspect Spectrum { get; } string Object { get; } string Subject { get; } string Locus { get; } }
	static class Basis
	{
		#region Axis specifics
		static List<string> axis = Enum.GetNames(typeof(Axis)).ToList() ; static List<uint> vaxi = Enum.GetValues(typeof(Axis)).Cast<uint>().ToList() ;
		internal static uint Axis( this string name ) => vaxi.At(axis.IndexOf(name)).nil(i=>i<0) ?? (uint)axis.Set(a=>{a.Add(name);vaxi.Add((uint)vaxi.Count);}).Count-1 ;
		internal static Quant ActLim( this Axis axis , string activity ) => 50 ;
		#endregion

		#region Point interpolation
		/// <summary>
		/// Interpolation point from given points at given time .
		/// </summary>
		/// <param name="date"> Time to which interpolate the point . </param>
		/// <param name="points"> Points are expected to be ordered by time . </param>
		/// <returns> Interpolation point at given time of given points . </returns>
		internal static Point Give( this DateTime date , IEnumerable<Point> points ) { Point bot = null ; foreach( var point in points ) if( point.Date<date ) bot = point ; else if( point.Date==date ) return point ; else if( bot==null ) return new Point(date)|point ; else return new Point(date)|date.Give(bot,point) ; return new Point(date)|bot ; }
		internal static Quant?[] Give( this DateTime date , Point bot , Point top ) { var quo = (Quant)( (date-bot.Date).TotalSeconds/(top.Date-bot.Date).TotalSeconds ) ; var cuo = 1-quo ; return ((int)Math.Max(bot.Dimension,top.Dimension)).Steps().Select(i=>(bot[(uint)i]*cuo+top[(uint)i]*quo)).ToArray() ; }
		internal static Quant? Quotient( this Quant? x , Quant? y ) => x / y.Nil() ;
		#endregion

		#region Euclid metrics
		public static int GradeAccu = Configer.AppSettings["Grade.Accumulation"].Parse(7) , VeloAccu = Configer.AppSettings["Speed.Accumulation"].Parse(1) ;
		static Quant? Sqrm( this Point point , Axis axis , Point at ) { Quant? value = point[axis]??0 ; if( axis==Act.Axis.Lat ) value *= Degmet ; if( axis==Act.Axis.Lon ) value *= Londeg(at[Act.Axis.Lat]) ; return value*value ; }
		static readonly Quant Degmet = 111321.5 ;
		internal const Quant Gravity = 10 ;
		static Quant? Londeg( Quant? latdeg ) => latdeg.Rad().use(Math.Cos) * Degmet ;
		static Quant? Rad( this Quant? deg ) => deg/180*Math.PI ;
		static Quant? Polar( this Point point , Point offset ) => point.Sqrm(Act.Axis.Lon,offset)+point.Sqrm(Act.Axis.Lat,offset) ;
		internal static Quant? Euclid( this Point point , Point offset ) => (point.Polar(offset)+point.Sqrm(Act.Axis.Alt,offset)).use(Math.Sqrt) ;
		internal static Quant? Sphere( this Point point , Point offset ) => point.Polar(offset).use(Math.Sqrt) ;
		internal static Quant? Grade( this Point point , Point offset ) => point.Get(p=>p[Act.Axis.Alt]/p.Sphere(offset).Nil(d=>d==0)) ;
#if true // grade average by count
		internal static Quant? Grade( this Path path , int at , Point offset ) { var count = 0 ; Quant? grade = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(g=>grade+=g)!=null ) ++count ; return count>0 ? grade/count : null ; }
#else	// grade average by distance
		internal static Quant? Grade( this Path path , int at , Point offset ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(v=>val+=v*path[i].Sphere(offset))!=null ) vol+=path[i].Sphere(offset).Value ; return vol>0 ? val/vol : null ; }
#endif
		internal static Quant? Devia( this Geos? x , Geos? y ) => (~(x+y)|x-y).Quotient(+(x+y)) * Degmet ;
		internal static Quant? Veloa( this Path path , int at ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-VeloAccu,0) ; i<Math.Min(path.Count,at+VeloAccu+1) ; ++i ) if( path[i].Speed.Set(v=>val+=v*path[i].Time.TotalSeconds)!=null ) vol+=path[i].Time.TotalSeconds ; return (vol>0?val/vol:null) ; }
		internal static Quant? Vibre( this Path path , int at ) => path[at].Speed / path.Veloa(at) ;
		#endregion
	}
	namespace Pre
	{
		public class Metax
		{
			IDictionary<string,uint> Map = new Dictionary<string,uint>() ;
			public uint this[ string ax ] => Map.At(ax,uint.MaxValue) ;
		}
		public abstract class Point : DynamicObject , Pointable
		{
			#region Construction
			Point() => Quantity = new Quant?[Dimension] ;
			public Point( DateTime date ) : this() => Date = date ;
			public Point( Point point ) : this() { Date = point.Date ; Quantity = point.Quantity.ToArray() ; Mark = point.Mark ; Spec = point.Spec ; Action = point.Action ; }
			#endregion

			#region Setup
			protected virtual void From( Pointable point ) { Time = point.Time ; for( uint i=0 ; i<point.Dimension ; ++i ) this[i] = point[i] ; }
			public virtual void Adopt( Pointable point ) { From(point) ; Date = point.Date ; Action = point.Action ; Mark = point.Mark ; }
			#endregion

			#region State
			/// <summary>
			/// Quanitity data vector .
			/// </summary>
			Quant?[] Quantity ;
			/// <summary>
			/// Referential date of object .
			/// </summary>
			public DateTime Date { get => date ; set { if( date==value ) return ; date = value ; sign = null ; } } DateTime date ;
			/// <summary>
			/// Relative time of object .
			/// </summary>
			public TimeSpan Time { get => time ; set { if( time==value ) return ; time = value ; sign = null ; } } TimeSpan time ;
			/// <summary>
			/// Signature of the point .
			/// </summary>
			public string Sign => sign ?? ( sign = $"{Date}{Time.nil(t=>t==TimeSpan.Zero).Get(t=>$"+{t:hh\\:mm\\:ss}")}" ) ; string sign ;
			/// <summary>
			/// Assotiative text .
			/// </summary>
			public virtual string Spec { get => spec ?? ( spec = $"{Action} {Sign}" ) ; set { if( value!=spec ) spec = value ; } } string spec ;
			/// <summary>
			/// Action specification .
			/// </summary>
			public string Action { get => action ; set { if( action==value ) return ; var a = action ; action = value ; if( spec==$"{a} {Sign}" ) Spec = null ; } } string action ;
			/// <summary>
			/// Kind of demarkaition .
			/// </summary>
			public Mark Mark { get; set; } public Mark? Marklet => Mark.nil() ;
			/// <summary>
			/// Metadata of axes .
			/// </summary>
			protected internal Metax Metax ;
			#endregion

			#region Trait
			public abstract uint Dimension { get ; }
			public Quant? this[ uint axis ] { get => Quantity.At((int)axis) ; set { if( axis>=Quantity.Length && value!=null && axis<uint.MaxValue ) Quantity.Set(q=>q.CopyTo(Quantity=new Quant?[axis+1],0)) ; if( axis<Quantity.Length ) Quantity[axis] = value ; } }
			public Quant? this[ string axis ] { get => this[Metax?[axis]??axis.Axis()] ; set => this[Metax?[axis]??axis.Axis()] = value ; }
			public override bool TrySetMember( SetMemberBinder binder , object value ) { this[binder.Name] = (Quant?)value ; return base.TrySetMember( binder, value ) ; }
			public override bool TryGetMember( GetMemberBinder binder , out object result ) { result = this[binder.Name] ; return true ; }
			public static implicit operator Quant?[]( Point point ) => point?.Quantity ;
			#endregion

			#region Info
			public override string ToString() => $"{Action} {Sign} {Exposion} {Trace}" ;
			public virtual string Quantities => $"{((int)Dimension).Steps().Select(i=>Quantity[i].Get(q=>$"{(Axis)i}={q}")).Stringy(' ')}" ;
			public virtual string Exposion => null ;
			public virtual string Trace => null ;
			#endregion
		}
	}
}
