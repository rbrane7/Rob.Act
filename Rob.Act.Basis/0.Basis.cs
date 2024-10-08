﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	/// <summary>
	/// Kind of separation marks .
	/// </summary>
	[Flags] public enum Mark { No=0 , Stop=1 , Lap=2 , Act=4 , Ato=8 , Sub=16 , Sup=32 , Hyp=64 , Aim=128 , Own=256 }
	[Flags] public enum Oper { Merge=0 , Combi=1 , Trim=2 , Smooth=4 , Relat=8 }
	public enum Axis : uint { Lon , Lat , Alt , Dist , Drag , Flow , Beat , Bit , Energy , Grade , Top , Lim=Hyp-1 , Time=uint.MaxValue , Date=Time-1 , Lap=Date-1 , Stop=Lap-1 , Act=Stop-1 , No=Act-1 , Ato=No-1 , Sub=Ato-1 , Sup=Sub-1 , Hyp=Sup-1 }
	#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
	#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
	public readonly struct Bipole : IFormattable
	#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
	#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
	{
		Bipole( Quant a , Quant b ) { A = Math.Abs(a) ; B = -Math.Abs(b) ; }
		public Bipole( Quant a ) { A = Math.Max(0,a) ; B = Math.Min(0,a) ; }
		public Quant A {get;}
		public Quant B {get;}
		public static Quant operator+( Bipole x ) => x.A-x.B ;
		public static Bipole operator-( Bipole x ) => new(x.B,x.A) ;
		public static Bipole operator+( Bipole x , Bipole y ) => new(x.A+y.A,x.B+y.B) ;
		public static Bipole operator-( Bipole x , Bipole y ) => x+-y ;
		public static Bipole operator+( Bipole x , Quant y ) => x+(Bipole)y ;
		public static Bipole operator-( Bipole x , Quant y ) => x-(Bipole)y ;
		public static Bipole operator*( Bipole x , Quant y ) => y>=0 ? new Bipole(x.A*y,x.B*y) : -x*-y ;
		public static Bipole? operator/( Bipole x , Quant y ) => y>0 ? new Bipole(x.A/y,x.B/y) : y<0 ? -x/-y : null ;
		public static Bipole operator*( Bipole x , Bipole y ) => x*(Quant)y ;
		public static Bipole? operator/( Bipole x , Bipole y ) => x/(Quant)y ;
		public static Bipole? operator+( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value+y.Value : null ;
		public static Bipole? operator-( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value-y.Value : null ;
		public static Bipole? operator+( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value+y.Value : null ;
		public static Bipole? operator-( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value-y.Value : null ;
		public static Bipole? operator*( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value*y.Value : null ;
		public static Bipole? operator/( Bipole? x , Quant? y ) => x!=null&&y!=null ? x.Value/y.Value : null ;
		public static Bipole? operator*( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value*y.Value : null ;
		public static Bipole? operator/( Bipole? x , Bipole? y ) => x!=null&&y!=null ? x.Value/y.Value : null ;
		public static implicit operator Bipole( Quant v ) => new Bipole(v) ;
		public static explicit operator Quant( Bipole v ) => v.A+v.B ;
		public static explicit operator Quant?( Bipole? v ) => v.use(q=>(Quant)q) ;
		public static bool operator==( Bipole x , Bipole y ) => x.A==y.A && x.B==y.B ;
		public static bool operator!=( Bipole x , Bipole y ) => !(x==y) ;
		public override readonly string ToString() => $"{A}-{-B}" ;
		public readonly string ToString( string format , IFormatProvider formatProvider ) => $"{A.ToString(format,formatProvider)}-{(-B).ToString(format,formatProvider)}" ;
	}
	public struct Geos( Quant lon , Quant lat )
	{
		public Quant Lon = lon , Lat = lat ;
		public static Geos operator~( Geos a ) => new(a.Lat,a.Lon) ;
		public static Quant operator+( Geos a ) => Math.Sqrt(a|a) ;
		public static Geos? operator~( Geos? a ) => a.use(x=>~x) ;
		public static Quant? operator+( Geos? a ) => a.use(x=>+x) ;
		public static Geos operator+( Geos a , Geos b ) => new(a.Lon+b.Lon,a.Lat+b.Lat) ;
		public static Geos operator-( Geos a , Geos b ) => new(a.Lon-b.Lon,a.Lat-b.Lat) ;
		public static Geos? operator+( Geos? a , Geos? b ) => a is Geos x && b is Geos y ? x+y : null as Geos? ;
		public static Geos? operator-( Geos? a , Geos? b ) => a is Geos x && b is Geos y ? x-y : null as Geos? ;
		public static Quant operator|( Geos a , Geos b ) => a.Lon*b.Lon+a.Lat*b.Lat ;
		public static Quant? operator&( Geos a , Geos b ) => (a|b)/(+a*+b).nil() ;
		public static Quant? operator|( Geos? a , Geos? b ) => a is Geos x && b is Geos y ? x|y : null as Quant? ;
		public static Quant? operator&( Geos? a , Geos? b ) => (a|b)/(+a*+b) ;
		public static implicit operator Geos?( Point point ) => point?.IsGeos==true ? new Geos{Lon=point[Axis.Lon].Value,Lat=point[Axis.Lat].Value} : null ;
		public static implicit operator Geos( (Quant lon,Quant lat) point ) => new(point.lon,point.lat) ;
		public static bool operator==( Geos x , Geos y ) => x.Lon==y.Lon && x.Lat==y.Lat ;
		public static bool operator!=( Geos x , Geos y ) => !(x==y) ;
		public override readonly string ToString() => $"({Lon:0.00000},{Lat:0.00000})" ;
	}
	public struct Geom
	{
		public Geos G ; public Quant? Alt ; public DateTime? Dat ;
		public Quant Lon { get => G.Lon ; set => G.Lon = value ; } public Quant Lat { get => G.Lat ; set => G.Lat = value ; }
		public Geom( Quant lon , Quant lat , Quant? alt = null , DateTime? dat = null ) : this((lon,lat),alt,dat) {}
		public Geom( Geos g , Quant? alt = null , DateTime? dat = null ) { G = g ; Alt = alt ; Dat = dat ; }
		public static Geom operator~( Geom a ) => new() { G=~a.G,Alt=a.Alt,Dat=a.Dat} ;
		public static Quant operator+( Geom a ) => Math.Sqrt(a|a) ;
		public static Geom operator-( Geom a ) => (0,0,0,a.Dat)-a ;
		public static Geom? operator~( Geom? a ) => a.use(x=>~x) ;
		public static Quant? operator+( Geom? a ) => a.use(x=>+x) ;
		public static Geom? operator-( Geom? a ) => a.use(x=>-x) ;
		public static Geom operator+( Geom a , Geom b ) => new(a.G+b.G,a.Alt+b.Alt,a.Dat) ;
		public static Geom operator-( Geom a , Geom b ) => new(a.G-b.G,a.Alt-b.Alt,a.Dat) ;
		public static Geom? operator+( Geom? a , Geom? b ) => a is Geom x && b is Geom y ? x+y : (Geom?)null ;
		public static Geom? operator-( Geom? a , Geom? b ) => a is Geom x && b is Geom y ? x-y : (Geom?)null ;
		public static Quant operator|( Geom a , Geom b ) => (a.Alt*b.Alt??0)+(a.G|b.G) ;
		public static Quant? operator|( Geom? a , Geom? b ) => a is Geom x && b is Geom y ? x|y : null as Quant? ;
		public static implicit operator Geom?( Point point ) => point?.IsGeo==true ? new Geom{Lon=point[Axis.Lon].Value,Lat=point[Axis.Lat].Value,Alt=point[Axis.Lon],Dat=point.Date} : (Geom?)null ;
		public static implicit operator Geom( (Quant lon,Quant lat,Quant? alt,DateTime? dat) point ) => new Geom(point.lon,point.lat,point.alt,point.dat) ;
		public override readonly string ToString() => $"({G},{Alt:0.0m},{Dat:yyyy-MM-dd.hh:mm:ss ddd})" ;
	}
	public interface Quantable : Aid.Gettable<uint,Quant?> , Aid.Gettable<Quant?> {}
	/// <summary>
	/// Equatable is used by GUI frameworks therefore they can't be used and overriden !
	/// </summary>
	public interface Pointable : Quantable , Aid.Accessible<uint,Quant?> , Aid.Accessible<Quant?> , Aid.Accessible<Mark,Quant?> { DateTime Date {get;} TimeSpan Time {get;} uint Dimension {get;} string Action {get;} Mark Mark {get;} Tagable Tag {get;} void Adapt( Pointable path ) ; }
	public interface Pathable : Pointable , Aid.Countable , Aid.Gettable<DateTime,Pointable> , Aid.Gettable<int,Pointable> { string Origin {get;} Path.Aspect Spectrum {get;} string Object {get;} string Subject {get;} string Locus {get;} string Refine {get;} string Detail {get;} }
	public static class Basis
	{
		#region Axis specifics
		static readonly List<string> axis = Enum.GetNames(typeof(Axis)).ToList() , marks = Enum.GetNames(typeof(Mark)).ToList() ;
		static readonly List<uint> vaxi = Enum.GetValues(typeof(Axis)).Cast<uint>().ToList() ; static readonly List<Mark> vama = Enum.GetValues(typeof(Mark)).Cast<Mark>().ToList() ;
		internal static uint? Axis( this string name , bool insure = false ) => axis.IndexOf(name) is int at && at>=0 ? vaxi[at] : insure ? (uint)axis.Set(a=>{a.Add(name);vaxi.Add((uint)vaxi.Count);}).Count-1 : default(uint?) ;
		internal static Axis? AsAxis( this string name ) => (Axis?)name.Axis() ;
		internal static Mark? Mark( this string name ) => marks.IndexOf(name) is int at && at>=0 ? vama[at] : default(Mark?) ;
		static readonly (Axis At,Axis To) Potentialim = (Act.Axis.Dist,Act.Axis.Top-1) ;
		public static readonly Axis[] Derivates = {Act.Axis.Dist,Act.Axis.Time} ;
		public static readonly uint[] Potenties = Potentials.Except(Derivates).Select(v=>(uint)v).ToArray() ;
		public static bool IsPotential( this Axis ax ) => Potentialim.At<=ax && ax<=Potentialim.To ;
		public static bool IsCentral( this Axis ax ) => Potentialim.At>ax ;
		public static IEnumerable<Axis> Potentials { get { for( var ax=Potentialim.At ; ax<=Potentialim.To ; ++ax ) yield return ax ; yield return Act.Axis.Time ; } }
		public static IEnumerable<Axis> Absoltutes { get { for( var ax=(Axis)0 ; ax<Act.Axis.Top ; ++ax ) if( ax<Potentialim.At || Potentialim.To<ax ) yield return ax ; yield return Act.Axis.Date ; } }
		public static IEnumerable<Mark> Marks => vama.Where(m=>m<=Act.Mark.Hyp) ;
		public static IEnumerable<Mark> Segmentables => vama.Where(m=>m>Act.Mark.No&&m<=Act.Mark.Hyp) ;
		internal static Quant ActLim( this Axis axis , string activity ) => 50 ;
		public static readonly Quant YearDays = (new DateTime(2,1,1)-new DateTime(1,1,1)).TotalDays ;
		public static class Device
		{
			public static class Skierg { public static readonly string Code = typeof(Skierg).Logo().RightFrom('.',true) ; public const Quant Draw = 2.8 ; }
			public static class Bio { public static readonly string Code = typeof(Bio).Logo().RightFrom('.',true) ; public const Quant Draw = 2.8 ; }
		}
		public readonly static IDictionary<string,(Quant Grade,Quant Flow,Quant Drag)> Energing = new Dictionary<string,(Quant Grade,Quant Flow,Quant Drag)>
		{
			["SKIING_CROSS_COUNTRY"]=(.03,0.01,.14) , ["ROLLER_SKIING"]=(.01,0,.14) ,
		} ;
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
		internal static Quant? Quotient( this Quant x , Quant? y ) => x / y.Nil() ;
		#endregion

		#region Euclid metrics
		/// <param name="vect"> Vector of distance from at point .</param>
		/// <param name="axis"> Axis to calculate .</param>
		/// <param name="at"> Point at absolute coordinates to relate to .</param>
		public static int GradeAccu = 7 , VeloAccu = 1 ;
		public static Quant Sqr( this Quant value ) => value*value ;
		public static Quant? Sqr( this Quant? value ) => value.use(Sqr) ;
		/// <summary>
		/// Square of given axis of vector at point of sphere .
		/// </summary>
		static Quant? Sqrm( this Point vect , Axis axis , Point at ) { Quant? value = vect[axis]??0 ; if( axis==Act.Axis.Lat ) value *= Degmet ; if( axis==Act.Axis.Lon ) value *= Londeg(at[Act.Axis.Lat]) ; return value*value ; }
		static readonly Quant Degmet = 111321.5 ;
		public static readonly (Quant Force,Quant Power) Gravity = (9.823,6) ;
		static Quant? Londeg( Quant? latdeg ) => latdeg.Rad().use(Math.Cos) * Degmet ;
		static Quant? Rad( this Quant? deg ) => deg/180*Math.PI ;
		static Quant? Polar( this Point vect , Point at ) => vect.Sqrm(Act.Axis.Lon,at)+vect.Sqrm(Act.Axis.Lat,at) ; // Polar 2D square of size of vector at point of sphere .
		internal static Quant? Euclid( this Point vect , Point at ) => (vect.Polar(at)+vect.Sqrm(Act.Axis.Alt,at)).use(Math.Sqrt) ; // Complete 3D size of vector at point of sphere .
		internal static Quant? Sphere( this Point vect , Point at ) => vect.Polar(at).use(Math.Sqrt) ; // Polar 2D size of vector at point of sphere .
		internal static Quant? Grade( this Point vect , Point at ) => vect.Get(p=>p[Act.Axis.Alt]/p.Sphere(at).Nil()) ; // Grade as tangent of ascent angle .
#if true // grade average by count
		internal static Quant? Grade( this Path path , int at , Point offset ) { var count = 0 ; Quant? grade = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(g=>grade+=g)!=null ) ++count ; return count>0 ? grade/count : null ; }
#else	// grade average by distance
		internal static Quant? Grade( this Path path , int at , Point offset ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(v=>val+=v*path[i].Sphere(offset))!=null ) vol+=path[i].Sphere(offset).Value ; return vol>0 ? val/vol : null ; }
#endif
		internal static Quant? Devia( this Geos? x , Geos? y ) => (~(x+y)|x-y).Quotient(+(x+y)) * Degmet ;
		internal static Quant? Veloa( this Path path , int at ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-VeloAccu,0) ; i<Math.Min(path.Count,at+VeloAccu+1) ; ++i ) if( path[i].Speed.Set(v=>val+=v*path[i].Time.TotalSeconds)!=null ) vol+=path[i].Time.TotalSeconds ; return (vol>0?val/vol:null) ; }
		internal static Quant? Vibre( this Path path , int at ) => path[at].Speed / path.Veloa(at) ;
		#endregion

		public static Quant Mass = Profile.Default.Mass , Tranq = Profile.Default.Tranq ;

		#region Propagation
		public static Quant? Propagation( this Quant time , (Quant time,Quant potential) a , (Quant time,Quant potential) b , Quant? tranq = null ) => a.time!=b.time && a.potential!=0 && b.potential!=0 && a.time*b.time>0 && time*a.time>0 ?
			(a.time/a.potential/Math.Tanh(time/(tranq??Tranq))+(b.time/b.potential-a.time/a.potential)*Math.Log(time/a.time)/Math.Log(b.time/a.time)) : null ;
		public static Quant? Copropagation( this Quant potential , (Quant time,Quant potential) a , (Quant time,Quant potential) b , Quant? tranq = null ) => a.time!=b.time && a.potential!=0 && b.potential!=0 && a.time>0 && b.time>0 && potential>0 ?
			a.time/a.potential*Math.Pow(potential/a.potential,(b.time/b.potential-a.time/a.potential)/Math.Log(b.potential/a.potential)/(a.time/a.potential))*Math.Tanh(potential/(tranq??Tranq)) : null ;
		#endregion
		#region Propagators
		public static Quant? Propagation( this (Quant? potential,bool inner) rad , (Quant time,Quant potential) a , (Quant time,Quant potential) b , Quant? tranq = null ) => rad.inner ? rad.potential?.Propagation(a,b,tranq) : rad.potential?.Copropagation(a,b,tranq) ;
		public static Quant? Propagation( this (Quant? potential,bool inner) rad , (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b , Quant? tranq = null ) => rad.inner ? rad.potential?.Propagation(a,b,tranq) : rad.potential?.Copropagation(a,b,tranq) ;
		public static Quant? Propagation( this (Quant? potential,bool inner) rad , (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b , Quant? tranq = null ) => 1/rad.Propagation((a.time,a.potential),(b.time,b.potential),tranq) ;
		public static Quant? Propagation( this Quant time , (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b , Quant? tranq = null ) => time.Propagation((a.time.TotalSeconds,a.potential),(b.time.TotalSeconds,b.potential),tranq) ;
		public static Quant? Propagation( this Quant time , (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b , Quant? tranq = null ) => 1/time.Propagation((a.time,a.potential),(b.time,b.potential),tranq) ;
		public static Quant? Copropagation( this Quant potential , (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b , Quant? tranq = null ) => potential.Copropagation((a.time.TotalSeconds,a.potential),(b.time.TotalSeconds,b.potential),tranq) ;
		public static Quant? Copropagation( this Quant potential , (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b , Quant? tranq = null ) => 1/potential.Copropagation((a.time,a.potential),(b.time,b.potential),tranq) ;
		#endregion

		#region Transformators
		public static Quant? PowerPace( this Quant? power , Quant grade = 0 , Quant drag = 0 , Quant flow = 0 , Quant? mass = null )
		{
			if( power is not Quant p ) return null ; var g = grade*Gravity.Force*(mass??Mass) ; var d = drag ; var f = flow ; if( p==0 && d*g>=0 ) return null ;
			return p==0 ? 1/Math.Sqrt(Math.Abs(g/3*d)) : 1/(d*g<0?p+Math.Sign(p)*Math.Sqrt(Math.Abs(g/3*d)):p).Radix(u=>(g+(f+d*u)*u)*u-p,u=>g+(2*f+3*d*u)*u).Nil() ;
		}
		public static Quant? PacePower( this Quant? pace , Quant grade = 0 , Quant drag = 0 , Quant flow = 0 , Quant grane = 0 , Quant? mass = null )
		=> pace==0 ? null : (mass??Mass).Get(m=>grade*m*Gravity.Power+(grade,grane).GradeGrane().Get(g=>g*m*Gravity.Force+g.Recuperation()*(flow+drag/pace)/pace)/pace) ;
		static Quant Recuperation( this Quant grade ) => grade<0 ? 1-Math.Tanh(grade*GravityRecuperation).Sqr() : 1 ;
		public static Quant GravityRecuperation = 200 , AirResistance = .4 ;
		static Quant GradeGrane( this (Quant grade,Quant grane) g ) => g.grade.nil(v=>Math.Abs(v)>1) is Quant a ? a+g.grane*Math.Sqrt(1-a.Sqr()) : g.grane ;
		public static Quant Cube( this Quant value ) => value*value*value ;
		#endregion

		#region Tags
		/// <summary> Extracts tags from one string . </summary>
		/// <param name="value"> String to extract from . </param>
		/// <param name="leaf"> If true , first two tags are extracted as nulls , which are <see cref="Point.Object"/> and <see cref="Point.Subjct"/> , which are forced to be drived from owner if point <see cref="Point.IsLeaf"/> . </param>
		/// <returns> Extracted tags as non-null enumerable . </returns>
		public static IEnumerable<string> ExtractTags( this string value , bool leaf = false ) => value?.TrimStart().StartsBy("?")==true ?
			value.RightFromFirst('?').Separate(';','&').Get(elem=>Tagger.Names.Get(n=>n.Select(e=>elem.Arg(e)).Concat(elem.Except(e=>e.LeftFrom('=')??string.Empty,n)))) :
			value.Separate(' ',braces:null).Get(t=>leaf?Enumerable.Repeat<string>(null,2).Concat(t.TagsLimed(2)):t.TagsLimed())  ?? Enumerable.Empty<string>() ;
		/// <summary> Limits tags count to predefined count defined by <see cref="Taglet"/> if the space separated tags are positioned as lasts and they contain <see cref="Tagger.Aclutinator"/> string . </summary>
		static IEnumerable<string> TagsLimed( this string[] tags , int skip = 0 )
		{
			int lim = (int)Taglet.Detail+1-skip ;
			return tags.Length>lim && tags.Length-tags.Count(t=>t.Contains(Tagger.Aclutinator))+1==lim ? tags.Take((int)Taglet.Detail-skip).Append(tags.Skip((int)Taglet.Detail-skip).Stringy(' ')) : tags ;
		}
		#endregion

		internal static IEnumerable<KeyValuePair<string,(uint At,string Form,bool Potent)>> Iterer( this Metax metax , uint @base = 0 ) => metax.Get(m=>m.Iterator(@base)) ?? Enumerable.Empty<KeyValuePair<string,(uint At,string Form,bool Potent)>>() ;
		internal static uint Suprem( this IDictionary<string,(uint At,string Form,bool Potent)> map ) => map.Count<=0?0:map.Max(a=>a.Value.At)+1 ;
		public static Quant TotalSeconds( this DateTime date ) => (date-DateTime.MinValue).TotalSeconds ;
		public static bool Equals( this Quant x , Quant y ) => x==y || Math.Abs(x-y)/(Math.Abs(x)+Math.Abs(y))<=QuantEpsilon ;
		public static bool Equals( this Quant? x , Quant? y ) => x==y || x is Quant a && y is Quant b && Equals(a,b) ;
		const Quant QuantEpsilon = 1e-10 ;

		public struct Binding
		{
			public string Path , Name , Format , Align ; public Func<object,object> Converter ;
			public readonly string Form => Align.No() ? Format : Format.No() ? $"{{0,{Align}}}" : $"{{0,{Align}:{Format}}}" ;
			public readonly string Reform => Align.No()&&!Format.No() ? $"{{0:{Format}}}" : Form ;
			public static implicit operator Binding( string value ) => new Binding(value) ;
			public Binding( string value )
			{
				if( value?.TrimStart().StartsBy("(")==true ) { var cvt = value.LeftFromScoped(true,'/',',',':') ; Converter = cvt.Compile<Func<object,object>>(use:"Aid.Forming") ; Path = null ; value = value.RightFromFirst(cvt) ; } else { Path = value.LeftFrom(true,':',',','/') ; Converter = null ; }
				Name = value.LeftFrom(true,':',',').RightFromFirst('/',true) ; Format = value.RightFromFirst(':') ; Align = value.LeftFrom(':').RightFrom(',') ;
			}
			public readonly string Of( object value ) => Reform.Form( Converter is Func<object,object> c ? c(value) : value ) ;
		}

		internal static string Serialize( this Mark mark ) => $"{(mark.HasFlag(Act.Mark.Stop)?"Stop":null)}{(mark.HasFlag(Act.Mark.Lap)?"Lap":null)}{(mark.HasFlag(Act.Mark.Act)?"Act":null)}{(mark.HasFlag(Act.Mark.Ato)?"Ato":null)}{(mark.HasFlag(Act.Mark.Sub)?"Sub":null)}{(mark.HasFlag(Act.Mark.Sup)?"Sup":null)}{(mark.HasFlag(Act.Mark.Hyp)?"Hyp":null)}{(mark.HasFlag(Act.Mark.Aim)?"Aim":null)}{(mark.HasFlag(Act.Mark.Own)?"Own":null)}" ;
		internal static Mark Deserialize( this string mark ) => (mark?.Contains("Stop")==true?Act.Mark.Stop:Act.Mark.No)|(mark?.Contains("Lap")==true?Act.Mark.Lap:Act.Mark.No)|(mark?.Contains("Act")==true?Act.Mark.Act:Act.Mark.No)|(mark?.Contains("Ato")==true?Act.Mark.Ato:Act.Mark.No)|(mark?.Contains("Sub")==true?Act.Mark.Sub:Act.Mark.No)|(mark?.Contains("Sup")==true?Act.Mark.Sup:Act.Mark.No)|(mark?.Contains("Hyp")==true?Act.Mark.Hyp:Act.Mark.No)|(mark?.Contains("Aim")==true?Act.Mark.Aim:Act.Mark.No)|(mark?.Contains("Own")==true?Act.Mark.Own:Act.Mark.No) ;
		internal static void MoveOriginTo( this string origin , string target )
		{
			var tap = System.IO.Path.GetDirectoryName(target) ; var orip = System.IO.Path.GetDirectoryName(origin) ;
			if( System.IO.Path.GetFileNameWithoutExtension(origin) is not string orik || System.IO.Path.GetFileNameWithoutExtension(target) is not string tagik ) return ;
			foreach( var file in System.IO.Directory.GetFiles(orip,$"{orik}.*") ) System.IO.File.Move(file,tap.Path(System.IO.Path.GetFileName(file).Replace(orik,tagik))) ;
			if( orik.Contains("logbook-workout") && System.IO.Path.ChangeExtension(origin.Replace("logbook-workout","result"),".csv") is string det && System.IO.File.Exists(det) ) System.IO.File.Move(det,tap.Pathex(tagik.Replace("logbook-workout","result"),".csv")) ;
			else if( orik.Contains("result") && System.IO.Path.ChangeExtension(origin.Replace("result","logbook-workout"),".tcx") is string alt && System.IO.File.Exists(alt) ) System.IO.File.Move(alt,tap.Pathex(tagik.Replace("result","logbook-workout"),".tcx")) ;
		}
	}
	public class Metax : IEquatable<Metax> , IEnumerable<KeyValuePair<string,(uint At,string Form,bool Potent)>>
	{
		internal uint Base ; internal Metax Heir ;
		readonly IDictionary<string,(uint At,string Form,bool Potent)> Map = new Dictionary<string,(uint At,string Form,bool Ponent)>() ;
		public IEnumerable<uint> Potenties => this.Where(a=>a.Value.Potent).Select(a=>a.Value.At).Concat(Base>0?Basis.Potenties:Basis.Potenties.Except(this.Select(a=>a.Value.At))) ;
		public uint this[ string ax ] => Heir?.Map.On(ax)?.At+Dim ?? Map.On(ax)?.At+Base ?? (uint)Axis.Lim ;
		public (string Name,string Form,bool Potent) this[ uint ax ] { get => this.SingleOrNil(m=>m.Value.At==ax).get(v=>(v.Key,v.Value.Form,v.Value.Potent))??default ; set => Map[value.Name] = (ax,value.Form,value.Potent) ; }
		public (string Name,string Form,bool Potent) this[ Axis ax ] { get => this[(uint)ax] ; set => this[(uint)ax] = value ; }
		public void Reset( Aspect.Traits traits ) => traits.Each(Reset) ;
		void Reset( Aspect.Traitlet t ) { var map = Heir?.Map??Map ; var i = map.On(t.Spec)?.At??map.Suprem() ; map[t.Spec]=(i,t.Bond,t.IsPotential) ; }
		public bool Equals( Metax other ) => other is Metax m && Base==m.Base && this.SequenceEquate(m) ;
		internal IEnumerable<KeyValuePair<string,(uint At,string Form,bool Potent)>> Iterator( uint @base ) => Map.Select(a=>new KeyValuePair<string,(uint At,string Form,bool Potent)>(a.Key,(a.Value.At+@base,a.Value.Form,a.Value.Potent))) ;
		public IEnumerator<KeyValuePair<string,(uint At,string Form,bool Potent)>> GetEnumerator() => (Iterator(Base).Concat(Heir.Iterer(Dim))).GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public uint Dimension => Dim+(Heir?.Sup??0) ; uint Dim => Base+Sup ; uint Sup => Map.Suprem() ;
		#region De/Serialization
		public static explicit operator Metax( string text ) => text.Null(t=>t.Void()).Get(t=>new Metax(t)) ;
		public static explicit operator string( Metax the ) => the.Get(t=>$"{t.Base}{Serialization.Separator}{t.Map.Union(t.Heir.Iterer(t.Sup),a=>a.Key).Select(e=>$"{e.Key}{Serialization.Infix}{e.Value.At}{Serialization.Infix}{e.Value.Form}{Serialization.Postfix}{(e.Value.Potent?"+":null)}").Stringy(Serialization.Separator)}") ;
		static class Serialization { public const string Separator = " \x1 Axe \x2 " , Infix = "\x1:\x2" , Postfix = "\x1;\x2" ; }
		#endregion
		Metax( string text ) : this(text.LeftFrom(Serialization.Separator,all:true).Parse<uint>()??0)
		{
			var error = false ;
			foreach( var e in text.RightFromFirst(Serialization.Separator).SeparateTrim(Serialization.Separator,false) ) try
			{
				Map.Add(e.LeftFrom(Serialization.Infix),e.RightFromFirst(Serialization.Infix).get(v=>(v.LeftFrom(Serialization.Infix).Parse<uint>()??0,v.RightFrom(Serialization.Infix).LeftFrom(Serialization.Postfix,all:true).Null(f=>f.No()),v.RightFrom(Serialization.Infix).RightFrom(Serialization.Postfix)=="+"))??default) ;
			}
			catch { System.Diagnostics.Trace.TraceError(e) ; error = true ; }
			if( error ) System.Diagnostics.Trace.TraceError(text) ;
		}
		public Metax( uint zero = 0 ) => Base = zero ;
		public IEnumerable<string> Bonds => this.Select(e=>$"[{e.Value.At}]/{e.Key}{e.Value.Form}") ;
	}
	namespace Pre
	{
		public abstract class Point : DynamicObject , Pointable
		{
			#region Construct
			Point() => Quantity = new Quant?[Dimension] ;
			public Point( DateTime date ) : this() { using var _=Incognit ; Date = date ; }
			public Point( Point point ) : this() { using var _=Incognit ; Date = point.Date ; Quantity = point.Quantity.ToArray() ; Mark = point.Mark ; Spec = point.Spec ; Action = point.Action ; }
			#endregion

			#region Setup
			protected virtual void From( Pointable point ) { Time = point.Time ; for( uint i=0 ; i<point.Dimension ; ++i ) this[i] = point[i] ; }
			public virtual void Adapt( Pointable point ) { if( point==null ) return ; From(point) ; Date = point.Date ; Action = point.Action ; Mark = point.Mark ; (point as Point).Set(p=>Metax=p.Metax) ; }
			/// <summary>
			/// Resets relative fields which are dependant on context . Those will be set newly . 
			/// </summary>
			protected internal virtual void Depose() { Time = default ; Post = default ; }
			#endregion

			#region State
			/// <summary>
			/// During init faze property chnges are not persisted .
			/// </summary>
			protected abstract Aid.Closure Incognit {get;}
			/// <summary>
			/// Quanitity data vector .
			/// </summary>
			Quant?[] Quantity ;
			/// <summary>
			/// Position relative to segments types .
			/// </summary>
			(Quant? Lap,Quant? Stop,Quant? Act,Quant? Ato,Quant? Sub,Quant? Sup,Quant? Hyp,int? No) Post ;
			/// <summary>
			/// Referential date of object .
			/// </summary>
			public virtual DateTime Date { get => date ; set { if( date==value ) return ; if( spec==Despect ) spec = null ; date = value ; sign = null ; } } DateTime date ;
			/// <summary>
			/// Relative time of object .
			/// </summary>
			public virtual TimeSpan Time { get => time ; set { if( time==value ) return ; if( spec==Despect ) spec = null ; time = value ; sign = null ; } } TimeSpan time ;
			/// <summary>
			/// Position within owner .
			/// </summary>
			public virtual int? No { get => Post.No ; set { if( No==value ) return ; Post.No = value ; } }
			/// <summary>
			/// Signature of the point .
			/// </summary>
			public virtual string Sign => sign ??= Signature ; string sign ; protected virtual string Signature => $"{Date.nil()}{Time.nil().Get(t=>$"+{t:hh\\:mm\\:ss}")}" ;
			/// <summary>
			/// Assotiative text .
			/// </summary>
			public virtual string Spec { get => spec ??= Despect ; set { if( value!=spec ) spec = value ; } } string spec ;
			protected string Despect => Despec(Action) ; protected virtual string Despec( string act ) => $"{act??Action} {Signature}" ;
			/// <summary>
			/// Action specification .
			/// </summary>
			public virtual string Action { get => action ; set { if( action==value ) return ; var a = action ; action = value ; if( spec==Despec(a) ) Spec = Despect ; } } string action ;
			/// <summary>
			/// Kind of demarkaition .
			/// </summary>
			public virtual Mark Mark {get;set;} public Mark? Marklet { get => Mark.nil() ; set { if( value is Mark mark ) Mark = mark ; } }
			/// <summary>
			/// Shows which marking couters are set .
			/// </summary>
			public Mark Marker { get { var rez = Mark.No ; foreach( var mark in Basis.Segmentables ) if( this[mark]!=null ) rez |= mark ; return rez ; } }
			/// <summary>
			/// Metadata of axes . 
			/// </summary>
			public virtual Metax Metax {get;set;}
			#endregion

			#region Trait
			public abstract uint Dimension {get;}
			public Quant? this[ uint axis ]
			{
				get => axis==(uint)Axis.Time ? Time.TotalSeconds : axis==(uint)Axis.Date ? Date.TotalSeconds() : axis==(uint)Axis.Lap ? Post.Lap : axis==(uint)Axis.Stop ? Post.Stop : axis==(uint)Axis.Act ? Post.Act : axis==(uint)Axis.Ato ? Post.Ato : axis==(uint)Axis.Sub ? Post.Sub : axis==(uint)Axis.Sup ? Post.Sup : axis==(uint)Axis.Hyp ? Post.Hyp : axis==(uint)Axis.No ? No : Quantity.At((int)axis) ;
				set { if( axis>=Quantity.Length && value!=null && axis<(uint)Axis.Lim ) Quantity.Set(q=>q.CopyTo(Quantity=new Quant?[Math.Max(axis+1,Metax?.Dimension??0)],0)) ; if( axis<Quantity.Length ) Quantity[axis] = value ; }
			}
			public Quant? this[ Mark mark ]
			{
				get => mark switch { Mark.No => No , Mark.Lap => Post.Lap , Mark.Stop => Post.Stop , Mark.Act => Post.Act , Mark.Ato => Post.Ato , Mark.Sub => Post.Sub , Mark.Sup => Post.Sup , Mark.Hyp => Post.Hyp , _ => null } ;
				set { switch( mark ) { case Mark.No : No = value.use(v=>(int)v) ; break ; case Mark.Lap : Post.Lap = value ; break ; case Mark.Stop : Post.Stop = value ; break ; case Mark.Act : Post.Act = value ; break ; case Mark.Ato : Post.Ato = value ; break ; case Mark.Sub : Post.Sub = value ; break ; case Mark.Sup : Post.Sup = value ; break ; case Mark.Hyp : Post.Hyp = value ; break ; } }
			}
			public Quant? this[ string axis ]
			{
				get => axis==null ? null : Metax?[axis] is uint ax ? ax<(uint)Axis.Lim ? this[ax] : axis.Mark() is Mark mr ? this[mr] : default(Quant?) : axis.Mark() is Mark ma ? this[ma] : axis.Axis() is uint a ? this[a] : default(Quant?) ;
				set { if( axis==null ) return ; if( Metax?[axis] is uint ax ) if( ax<(uint)Axis.Lim ) this[ax] = value ; else this[axis.Mark().Value] = value ; else if( axis.Mark() is Mark mark ) this[mark] = value ; else this[axis.Axis(true).Value] = value ; }
			}
			public override bool TrySetMember( SetMemberBinder binder , object value ) { this[binder.Name] = (Quant?)value ; return base.TrySetMember( binder, value ) ; }
			public override bool TryGetMember( GetMemberBinder binder , out object result ) { result = this[binder.Name] ; return true ; }
			public static implicit operator Quant?[]( Point point ) => point?.Quantity ;
			#endregion

			#region Info
			public override string ToString() => $"{Action} {Sign} {Exposion} {Trace}" ;
			public virtual string Quantities => $"{((int)Dimension).Steps().Select(i=>Quantity[i].Get(q=>$"{(Axis)i}={q:0.00}")).Stringy(' ')}" ;
			public virtual string Exposion => null ;
			public virtual string Trace => null ;
			public abstract Tagable Tag {get;}
			#endregion

			#region Comparison
			public virtual bool Equals( Pointable other ) => other is Point p && date==p.date && time==p.time /*&& Spec==p.Spec*/ && action==p.action && Mark==p.Mark && Quantity.SequenceEquate(p.Quantity,Basis.Equals) ;
			public virtual bool EqualsRestricted( Pointable other ) => other is Point p && date==p.date /*&& Spec==p.Spec*/ && action==p.action && Mark==p.Mark && Quantity.Skip((int)Axis.Top).SequenceEquate(p.Quantity.Skip((int)Axis.Top),Basis.Equals) ;
			#endregion

			#region de/Serialization
			protected Point( string text )
			{
				var qs = text.LeftFrom(Serialization.Quant).Separate(',') ; qs[0].Parse<DateTime>("yyyy-MM-dd HH:mm:ss.ff").Use(v=>date=v) ; qs[1].Parse<TimeSpan>().Use(v=>time=v) ; Quantity = qs.Skip(2).Select(q=>q.Parse<Quant>()).ToArray() ;
				var ss = text.RightFrom(Serialization.Quant).LeftFromLast(Serialization.Act) ; ss.LeftFrom(',').Deserialize().nil().Use(v=>Mark=v) ;
				ss = ss.RightFromFirst(',') ; ss.LeftFrom(Serialization.Act).Null(v=>v.No()).Set(v=>spec=v) ; ss.RightFrom(Serialization.Act).Null(v=>v.No()).Set(v=>action=v) ;
			}  
			public static explicit operator string( Point p ) => $"{p.Date:yyyy-MM-dd HH:mm:ss.ff},{p.Time},{p.Quantity.Stringy(',')}{Serialization.Quant}{p.Marklet?.Serialize()},{(p.Spec!=p.Despect?p.Spec:null)}{Serialization.Act}{p.Action}{Serialization.Act}" ;
			public static explicit operator Point( string text ) => text?.Contains('\n')==true ? (Path)text : (Act.Point)text ;
			protected static class Serialization { public const string Quant = " \x1 Quant \x2 " , Act = " \x1 Act \x2 " , Tag = " \x1 Tag \x2 " ; }
			#endregion
		}
	}
}
