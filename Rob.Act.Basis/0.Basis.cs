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
	[Flags] public enum Mark { No=0 , Stop=1 , Lap=2 , Act=4 }
	[Flags] public enum Oper { Merge=0 , Combi=1 , Trim=2 , Smooth=4 , Relat=8 }
	public enum Axis : uint { Lon,Longitude=Lon , Lat,Latitude=Lat , Alt,Altitude=Alt , Dist,Distance=Dist , Drag , Flow , Beat , Bit , Energy , Grade , Time , Date }
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
	/// <summary>
	/// Equatable is used by GUI frameworks therefore they can't be used and overriden !
	/// </summary>
	public interface Pointable : Quantable , Aid.Accessible<uint,Quant?> , Aid.Accessible<Quant?> { DateTime Date { get; } TimeSpan Time { get; } uint Dimension { get; } string Action { get; } Mark Mark { get; } Tagable Tag { get; } void Adopt( Pointable path ) ; }
	public interface Pathable : Pointable , Aid.Countable , Aid.Gettable<DateTime,Pointable> , Aid.Gettable<int,Pointable> { string Origin { get; } Path.Aspect Spectrum { get; } string Object { get; } string Subject { get; } string Locus { get; } string Refine { get; } }
	public static class Basis
	{
		#region Axis specifics
		static readonly List<string> axis = Enum.GetNames(typeof(Axis)).ToList() ; static readonly List<uint> vaxi = Enum.GetValues(typeof(Axis)).Cast<uint>().ToList() ;
		internal static uint Axis( this string name , uint dim ) => vaxi.At(axis.IndexOf(name)).nil(i=>i<0).Get(i=>i==(uint)Act.Axis.Time?dim:i==(uint)Act.Axis.Date?dim+1:i) ?? (uint)axis.Set(a=>{a.Add(name);vaxi.Add((uint)vaxi.Count);}).Count-1 ;
		internal static Quant ActLim( this Axis axis , string activity ) => 50 ;
		public static class Device
		{
			public static class Skierg { public const string Code = "Skierg" ; public const Quant Draw = 2.8 ; }
		}
		public readonly static IDictionary<string,(Quant Grade,Quant Flow,Quant Drag)> Energing = new Dictionary<string,(Quant Grade,Quant Flow,Quant Drag)>
		{
			["SKIING_CROSS_COUNTRY"]=(.03,0,.14) , ["ROLLER_SKIING"]=(.01,0,.14) ,
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
		public static int GradeAccu = Configer.AppSettings["Grade.Accumulation"].Parse(7) , VeloAccu = Configer.AppSettings["Speed.Accumulation"].Parse(1) ;
		static Quant Sqr( this Quant value ) => value*value ;
		static Quant? Sqr( this Quant? value ) => value.use(Sqr) ;
		/// <summary>
		/// Square of given axis of vector at point of sphere .
		/// </summary>
		static Quant? Sqrm( this Point vect , Axis axis , Point at ) { Quant? value = vect[axis]??0 ; if( axis==Act.Axis.Lat ) value *= Degmet ; if( axis==Act.Axis.Lon ) value *= Londeg(at[Act.Axis.Lat]) ; return value*value ; }
		static readonly Quant Degmet = 111321.5 ;
		public const Quant Gravity = 9.823 ;
		static Quant? Londeg( Quant? latdeg ) => latdeg.Rad().use(Math.Cos) * Degmet ;
		static Quant? Rad( this Quant? deg ) => deg/180*Math.PI ;
		static Quant? Polar( this Point vect , Point at ) => vect.Sqrm(Act.Axis.Lon,at)+vect.Sqrm(Act.Axis.Lat,at) ; // Polar 2D square of size of vector at point of sphere .
		internal static Quant? Euclid( this Point vect , Point at ) => (vect.Polar(at)+vect.Sqrm(Act.Axis.Alt,at)).use(Math.Sqrt) ; // Complete 3D size of vector at point of sphere .
		internal static Quant? Sphere( this Point vect , Point at ) => vect.Polar(at).use(Math.Sqrt) ; // Polar 2D size of vector at point of sphere .
		internal static Quant? Grade( this Point vect , Point at ) => vect.Get(p=>p[Act.Axis.Alt]/p.Sphere(at).Nil(d=>d==0)) ; // Grade as tangent of ascent angle .
#if true // grade average by count
		internal static Quant? Grade( this Path path , int at , Point offset ) { var count = 0 ; Quant? grade = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(g=>grade+=g)!=null ) ++count ; return count>0 ? grade/count : null ; }
#else	// grade average by distance
		internal static Quant? Grade( this Path path , int at , Point offset ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(v=>val+=v*path[i].Sphere(offset))!=null ) vol+=path[i].Sphere(offset).Value ; return vol>0 ? val/vol : null ; }
#endif
		internal static Quant? Devia( this Geos? x , Geos? y ) => (~(x+y)|x-y).Quotient(+(x+y)) * Degmet ;
		internal static Quant? Veloa( this Path path , int at ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-VeloAccu,0) ; i<Math.Min(path.Count,at+VeloAccu+1) ; ++i ) if( path[i].Speed.Set(v=>val+=v*path[i].Time.TotalSeconds)!=null ) vol+=path[i].Time.TotalSeconds ; return (vol>0?val/vol:null) ; }
		internal static Quant? Vibre( this Path path , int at ) => path[at].Speed / path.Veloa(at) ;
		#endregion

		#region Curves
		public static Quant Mass = Profile.Default.Mass ;
		public static Quant? Propagation( this (Quant? potential,bool inner) rad , (Quant time,Quant potential) a , (Quant time,Quant potential) b ) => rad.inner ? rad.potential?.Propagation(a,b) : rad.potential?.Copropagation(a,b) ;
		public static Quant? Propagation( this (Quant? potential,bool inner) rad , (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b ) => rad.inner ? rad.potential?.Propagation(a,b) : rad.potential?.Copropagation(a,b) ;
		public static Quant? Propagation( this (Quant? potential,bool inner) rad , (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b ) => 1/rad.Propagation((a.time,a.potential),(b.time,b.potential)) ;
		public static Quant? Propagation( this Quant time , (Quant time,Quant potential) a , (Quant time,Quant potential) b )
		{
			if( a.time!=b.time && a.potential!=0 && b.potential!=0 && a.time>0 && b.time>0 && time>0 ); else return null ;
			var t = Math.Log(time/a.time)/Math.Log(b.time/a.time) ; return (a.time/a.potential+(b.time/b.potential-a.time/a.potential)*t).nil(v=>v<=0) ;
		}
		public static Quant? Copropagation( this Quant potential , (Quant time,Quant potential) a , (Quant time,Quant potential) b ) => a.time!=b.time && a.potential!=0 && b.potential!=0 && a.time>0 && b.time>0 && potential>0 ? (a.time/a.potential*Math.Pow(potential/a.potential,(b.time/b.potential-a.time/a.potential)/Math.Log(b.potential/a.potential)/(a.time/a.potential))) : null as Quant? ;
		public static Quant? Propagation( this Quant time , (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b ) => time.Propagation((a.time.TotalSeconds,a.potential),(b.time.TotalSeconds,b.potential)) ;
		public static Quant? Propagation( this Quant time , (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b ) => 1/time.Propagation((a.time,a.potential),(b.time,b.potential)) ;
		public static Quant? Copropagation( this Quant potential , (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b ) => potential.Copropagation((a.time.TotalSeconds,a.potential),(b.time.TotalSeconds,b.potential)) ;
		public static Quant? Copropagation( this Quant potential , (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b ) => 1/potential.Copropagation((a.time,a.potential),(b.time,b.potential)) ;
		public static Quant? PacePower( this Quant? pace , Quant grade = 0 , Quant drag = 0 , Quant flow = 0 , Quant? mass = null ) => pace!=0 ? (grade*Gravity*(mass??Mass)).Get(g=>g*Histeresis+(g+grade.Recuperation()*(flow+drag/pace)/pace)/pace) : null as Quant? ;
		public static Quant? PacePower( this Quant? pace , (Quant grade,Quant grane) g , Quant drag = 0 , Quant flow = 0 , Quant? mass = null ) => pace.PacePower(g.GradeGrane(),drag,flow,mass) ;
		public static Quant? PowerPace( this Quant? power , Quant grade = 0 , Quant drag = 0 , Quant flow = 0 , Quant? mass = null )
		{
			if( power is Quant p );else return null ; var g = grade*Gravity*(mass??Mass) ; var d = drag ; var f = flow ; if( p==0 && d*g>=0 ) return null ;
			return p==0 ? 1/Math.Sqrt(Math.Abs(g/3*d)) : 1/(d*g<0?p+Math.Sign(p)*Math.Sqrt(Math.Abs(g/3*d)):p).Radix(u=>(g+(f+d*u)*u)*u-p,u=>g+(2*f+3*d*u)*u).Nil() ;
		}
		static Quant Recuperation( this Quant grade ) => grade<0 ? 1-Math.Tanh(grade*GravityRecuperation).Sqr() : 1 ;
		public static Quant GravityRecuperation = 200 , AirResistance = .4 , Histeresis = 1 ;
		static Quant GradeGrane( this (Quant grade,Quant grane) g ) => g.grade.nil(v=>Math.Abs(v)>1) is Quant a ? a+g.grane*Math.Sqrt(1-a.Sqr()) : g.grane ;
		#endregion

		#region Tags
		public static IEnumerable<string> ExtractTags( this string value ) => value?.TrimStart().StartsBy("?")==true ? value.RightFromFirst('?').Separate(';','&').Get(elem=>typeof(Taglet).GetEnumNames().Get(n=>n.Select(e=>elem.Arg(e)).Concat(elem.Except(e=>e.LeftFrom('=')??string.Empty,n)))) : value.Separate(' ') ?? Enumerable.Empty<string>() ;
		#endregion

		internal static IEnumerable<KeyValuePair<string,(uint At,string Form)>> Iterer( this Metax metax , uint @base = 0 ) => metax.Get(m=>m.Iterator(@base)) ?? Enumerable.Empty<KeyValuePair<string,(uint At,string Form)>>() ;
		public static Quant TotalSeconds( this DateTime date ) => (date-DateTime.MinValue).TotalSeconds ;
		public static bool Equals( this Quant x , Quant y ) => x==y || Math.Abs(x-y)/(Math.Abs(x)+Math.Abs(y))<=QuantEpsilon ;
		public static bool Equals( this Quant? x , Quant? y ) => x==y || x is Quant a && y is Quant b && Equals(a,b) ;
		const Quant QuantEpsilon = 1e-10 ;

		public struct Binding
		{
			public string Path , Name , Format , Align ; public Func<object,object> Converter ;
			public string Form => Align.No() ? Format : Format.No() ? $"{{0,{Align}}}" : $"{{0,{Align}:{Format}}}" ;
			public string Reform => Align.No()&&!Format.No() ? $"{{0:{Format}}}" : Form ;
			public static implicit operator Binding( string value ) => new Binding(value) ;
			public Binding( string value )
			{
				if( value?.TrimStart().StartsBy("(")==true ) { var cvt = value.LeftFromScoped(true,'/',',',':') ; Converter = cvt.Compile<Func<object,object>>() ; Path = null ; value = value.RightFromFirst(cvt) ; } else { Path = value.LeftFrom(true,':',',','/') ; Converter = null ; }
				Name = value.LeftFrom(true,':',',').RightFromFirst('/',true) ; Format = value.RightFromFirst(':') ; Align = value.LeftFrom(':').RightFrom(',') ;
			}
			public string Of( object value ) => Reform.Form( Converter is Func<object,object> c ? c(value) : value ) ;
		}
	}
	public class Metax : IEquatable<Metax> , IEnumerable<KeyValuePair<string,(uint At,string Form)>>
	{
		internal uint Base ; internal Metax Basis ;
		readonly IDictionary<string,(uint At,string Form)> Map = new Dictionary<string,(uint At,string Form)>() ;
		public uint this[ string ax ] => Map.On(ax)?.At+Base ?? Basis?.Map.On(ax)?.At+Base+Top ?? uint.MaxValue ;
		public (string Name,string Form) this[ uint ax ] => this.SingleOrNil(m=>m.Value.At==ax).get(v=>(v.Key,v.Value.Form))??default ;
		public void Reset( Aspect.Traits traits ) { if( (Basis?.Map.Count??Map.Count)>0 ) return ; uint i = 0 ; traits.Each(t=>(Basis?.Map??Map)[t.Spec]=(i++,t.Bond)) ; }
		public bool Equals( Metax other ) => other is Metax m && Base==m.Base && this.SequenceEquate(m) ;
		internal IEnumerable<KeyValuePair<string,(uint At,string Form)>> Iterator( uint @base ) => Map.Select(a=>new KeyValuePair<string,(uint At,string Form)>(a.Key,(a.Value.At+@base,a.Value.Form))) ;
		public IEnumerator<KeyValuePair<string,(uint At,string Form)>> GetEnumerator() => (Iterator(Base).Concat(Basis.Iterer(Dim))).GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public uint Dimension => Dim+(Basis?.Top??0) ; uint Dim => Base+Top ; uint Top => (uint)Map.Count ;
		#region De/Serialization
		public static explicit operator Metax( string text ) => text.Null(t=>t.Void()).Get(t=>new Metax(t)) ;
		public static explicit operator string( Metax the ) => the.Get(t=>$"{t.Base}{Serialization.Separator}{t.Map.Concat(t.Basis.Iterer(t.Top)).Select(e=>$"{e.Key}{Serialization.Infix}{e.Value.At}{Serialization.Infix}{e.Value.Form}").Stringy(Serialization.Separator)}") ;
		static class Serialization { public const string Separator = " \x1 Axe \x2 " , Infix = "\x1:\x2" ; }
		#endregion
		Metax( string text ) : this(text.LeftFrom(Serialization.Separator,all:true).Parse<uint>()??0)
		{
			foreach( var e in text.RightFromFirst(Serialization.Separator).SeparateTrim(Serialization.Separator) )
				Map.Add(e.LeftFrom(Serialization.Infix),e.RightFromFirst(Serialization.Infix).get(v=>(v.LeftFrom(Serialization.Infix).Parse<uint>()??0,v.RightFrom(Serialization.Infix).Null(f=>f.No())))??default) ;
		}
		public Metax( uint zero = 0 ) => Base = zero ;
		public IEnumerable<string> Bonds => this.Select(e=>$"[{e.Value.At}]/{e.Key}{e.Value.Form}") ;
	}
	namespace Pre
	{
		public abstract class Point : DynamicObject , Pointable
		{
			#region Construction
			Point() => Quantity = new Quant?[Dimension] ;
			public Point( DateTime date ) : this() => Date = date ;
			public Point( Point point ) : this() { Date = point.Date ; Quantity = point.Quantity.ToArray() ; Mark = point.Mark ; Spec = point.Spec ; Action = point.Action ; }
			#endregion

			#region Setup
			protected virtual void From( Pointable point ) { Time = point.Time ; for( uint i=0 ; i<point.Dimension ; ++i ) this[i] = point[i] ; }
			public virtual void Adopt( Pointable point ) { if( point==null ) return ; From(point) ; Date = point.Date ; Action = point.Action ; Mark = point.Mark ; (point as Point).Set(p=>Metax=p.Metax) ; }
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
			public virtual string Sign => sign ?? ( sign = $"{Date}{Time.nil().Get(t=>$"+{t:hh\\:mm\\:ss}")}" ) ; string sign ;
			/// <summary>
			/// Assotiative text .
			/// </summary>
			public virtual string Spec { get => spec ?? ( spec = Despect ) ; set { if( value!=spec ) spec = value ; } } string spec ; protected string Despect => Despec(Action) ; protected virtual string Despec( string act ) => $"{act??Action} {Sign}" ;
			/// <summary>
			/// Action specification .
			/// </summary>
			public string Action { get => action ; set { if( action==value ) return ; var a = action ; action = value ; if( spec==Despec(a) ) Spec = null ; } } string action ;
			/// <summary>
			/// Kind of demarkaition .
			/// </summary>
			public Mark Mark { get; set; } public Mark? Marklet => Mark.nil() ;
			/// <summary>
			/// Metadata of axes .
			/// </summary>
			public virtual Metax Metax { get ; set ; }
			#endregion

			#region Trait
			public abstract uint Dimension { get ; }
			public Quant? this[ uint axis ] { get => axis==Dimension ? Time.TotalSeconds : axis==Dimension+1 ? Date.TotalSeconds() : Quantity.At((int)axis) ; set { if( axis>=Quantity.Length && value!=null && axis<uint.MaxValue ) Quantity.Set(q=>q.CopyTo(Quantity=new Quant?[Math.Max(axis+1,Metax?.Dimension??0)],0)) ; if( axis<Quantity.Length ) Quantity[axis] = value ; } }
			public Quant? this[ string axis ] { get => this[Metax?[axis]??axis.Axis(Dimension)] ; set => this[Metax?[axis]??axis.Axis(Dimension)] = value ; }
			public override bool TrySetMember( SetMemberBinder binder , object value ) { this[binder.Name] = (Quant?)value ; return base.TrySetMember( binder, value ) ; }
			public override bool TryGetMember( GetMemberBinder binder , out object result ) { result = this[binder.Name] ; return true ; }
			public static implicit operator Quant?[]( Point point ) => point?.Quantity ;
			#endregion

			#region Info
			public override string ToString() => $"{Action} {Sign} {Exposion} {Trace}" ;
			public virtual string Quantities => $"{((int)Dimension).Steps().Select(i=>Quantity[i].Get(q=>$"{(Axis)i}={q}")).Stringy(' ')}" ;
			public virtual string Exposion => null ;
			public virtual string Trace => null ;
			public abstract Tagable Tag { get ; }
			#endregion

			#region Comparison
			public virtual bool Equals( Pointable other ) => other is Point p && date==p.date && time==p.time /*&& Spec==p.Spec*/ && action==p.action && Mark==p.Mark && Quantity.SequenceEquate(p.Quantity,Basis.Equals) ;
			public virtual bool EqualsRestricted( Pointable other ) => other is Point p && date==p.date /*&& Spec==p.Spec*/ && action==p.action && Mark==p.Mark && Quantity.Skip((int)Axis.Time).SequenceEquate(p.Quantity.Skip((int)Axis.Time),Basis.Equals) ;
			#endregion

			#region de/Serialization
			protected Point( string text )
			{
				var qs = text.LeftFrom(Serialization.Quant).Separate(',') ; qs[0].Parse<DateTime>().Use(v=>date=v) ; qs[1].Parse<TimeSpan>().Use(v=>time=v) ; Quantity = qs.Skip(2).Select(q=>q.Parse<Quant>()).ToArray() ;
				var ss = text.RightFrom(Serialization.Quant).LeftFromLast(Serialization.Act) ; ss.LeftFrom(',').Parse<Mark>().Use(v=>Mark=v) ;
				ss = ss.RightFromFirst(',') ; ss.LeftFrom(Serialization.Act).Null(v=>v.No()).Set(v=>spec=v) ; ss.RightFrom(Serialization.Act).Null(v=>v.No()).Set(v=>action=v) ;
			}  
			public static explicit operator string( Point p ) => $"{p.Date},{p.Time},{p.Quantity.Stringy(',')}{Serialization.Quant}{p.Marklet},{(p.Spec!=p.Despect?p.Spec:null)}{Serialization.Act}{p.Action}{Serialization.Act}" ;
			public static explicit operator Point( string text ) => text?.Contains('\n')==true ? (Path)text : (Act.Point)text ;
			protected static class Serialization { public const string Quant = " \x1 Quant \x2 " , Act = " \x1 Act \x2 " , Tag = " \x1 Tag \x2 " ; }
			#endregion
		}
	}
}
