using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Aid.Extension;
using Aid;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Rob.Act
{
	using Quant = Double ;
	public enum Taglet { Object , Drag , Subject , Locus , Refine , Grade , Flow }
	public interface Tagable : IEquatable<Tagable> , IEnumerable<string> { void Add( string item ) ; string this[ int key ] {get;set;} string this[ Taglet tag ] {get;set;} string this[ string key ] {get;set;} int Count {get;} void Clear() ; void Adopt( Tagable tags ) ; string Uri {get;} }
	public class Tagger : List<string> , Tagable
	{
		public static readonly string[] Names = Enum.GetNames(typeof(Taglet)) ;
		internal Action<string> Notifier {private get;set;}
		internal Tagger( Action<string> notifier = null ) => Notifier = notifier ;
		public new string this[ int key ] { get => (uint)key<Count ? base[key] : null ; set { if( this[key]==value ) return ; if( value!=null ) InsureCapacity(key) ; if( (uint)key<Count );else return ; base[key] = value ; Notifier?.Invoke(null) ; } }
		public string this[ Taglet key ] { get => this[(int)key] ; set { if( this[key]==value ) return ; this[(int)key] = value ; Notifier?.Invoke(key.ToString()) ; } }
		public string this[ string key ] { get => key.Parse<Taglet>() is Taglet t ? this[t] : MatchIndex(key) is int i ? this[i] : null ; set { if( key.Parse<Taglet>() is Taglet t ) { this[t] = value ; return ; } if( MatchIndex(key) is int i ) this[i] = value ; else Add(value) ; Notifier?.Invoke(key) ; } }
		public bool this[ params string[] tag ] { get => this[ tag as IEnumerable<string> ] ; set => this[ tag as IEnumerable<string> ] = value ; }
		public bool this[ IEnumerable<string> tag ] { get => this[tag] = false ; set { if( value ) Clear() ; AddRange(tag) ; Notifier?.Invoke(null) ; } }
		public new void Add( string tag ) { if( tag==null ) return ; base.Add(tag) ; Notifier?.Invoke(null) ; }
		public void Adopt( Tagable tags ) { Clear() ; tags.Set(AddRange) ; }
		int? MatchIndex( string key ) => MatchIndexes(key).singleOrNil() ;
		IEnumerable<int> MatchIndexes( string key ) => key.Get(k=>new Regex(k).Get(r=>this.IndexesWhere(r.IsMatch))) ;
		void InsureCapacity( int capacity ) { while( Count<=capacity ) base.Add(null) ; }
		public override string ToString() => this.Stringy(' ') ;
		public string Uri => Count.Steps().Select(i=>i<Names.Length?$"{Names[i]}={this[i]}":this[i]).Stringy('&','?') ;
		public bool Equals( Tagable other ) => other is Tagger t && this.SequenceEquate(t,Equals) ;
		static bool Equals( string x , string y ) => x.Null(v=>v.No())==y.Null(v=>v.No()) ;
		#region De/Serialization
		public static explicit operator string( Tagger the ) => the.Stringy(Serialization.Separator) ;
		public static explicit operator Tagger( string text ) => text.Null(v=>v.Void()).Get(t=>new Tagger(t)) ;
		Tagger( string text ) => AddRange(text.Separate(Serialization.Separator)) ;
		class Serialization { public const string Separator = " \x1 Tag \x2 " ; }
		#endregion
	}
	public class Point : Pre.Point , Accessible<Axis,Quant?> , INotifyPropertyChanged
	{
		protected internal Pathable Owner ;
		#region Construction
		public Point( DateTime date , Pathable owner = null ) : base(date) => Owner = owner ;
		public Point( Point point , Pathable owner = null ) : base(point) => Owner = owner??point?.Owner ;
		public override void Adapt( Pointable point ) { base.Adapt(point) ; if( (point as Point)?.Tags!=null || Tags!=null ) Tag.Adopt(point.Tag) ; }
		protected internal override void Depose() { base.Depose() ; No = null ; Dist = null ; Ascent = Deviation = null ; Owner = null ; }
		#endregion

		#region State
		/// <summary>
		/// During init faze property chnges are not persisted .
		/// </summary>
		protected override Closure Incognit => new Closure(()=>++initing,()=>--initing) ; byte initing ;
		/// <summary>
		/// Set to initialization/initialized mode .
		/// </summary>
		public bool Initing { get => initing>0 ; set { if( value ) ++initing ; else --initing ; } }
		/// <summary>
		/// Assotiative text .
		/// </summary>
		public override string Spec { set { if( value!=Spec ) SpecChanged( base.Spec = value ) ; } }
		/// <summary>
		/// Accessor of binding .
		/// </summary>
		public object This { get => this ; set => (value as Action<object>)?.Invoke(this) ; }
		protected override string Despec( string act ) => Tags is string t ? $"{base.Despec(act)} {t}" : base.Despec(act) ;
		protected virtual void SpecChanged( string value ) => Changed("Spec") ;
		/// <summary>
		/// Ascent of the path .
		/// </summary>
		public Bipole? Ascent {get;set;}
		/// <summary>
		/// Deviation of the path .
		/// </summary>
		public Bipole? Deviation {get;set;}
		#endregion

		#region Tags
		void TagChanged( string p ) { tag = null ; if( Spec==Despect ) Spec = null ; Changed(p??"Tags") ; }
		public override Tagable Tag => tags ?? System.Threading.Interlocked.CompareExchange(ref tags,new Tagger(TagChanged),null) ?? tags ; Tagger tags ;
		public string Tags { get => tag ??= tags.Stringy() ; set { if( value==tag ) return ; tag = null ; (Tag as Tagger)[value.ExtractTags()] = true ; Changed("Subject,Object,Locus,Refine") ; } } string tag ;
		public string Subject { get => tags?[Taglet.Subject]??Owner?.Subject ; set { if( value?.Length>0 ) Tag[Taglet.Subject] = value ; else tags.Set(t=>t[Taglet.Subject]=value) ; } }
		public string Object { get => tags?[Taglet.Object]??Owner?.Object ; set { if( value?.Length>0 ) Tag[Taglet.Object] = value ; else tags.Set(t=>t[Taglet.Object]=value) ; } }
		public string Locus { get => tags?[Taglet.Locus]??Owner?.Locus ; set { if( value?.Length>0 ) Tag[Taglet.Locus] = value ; else tags.Set(t=>t[Taglet.Locus]=value) ; } }
		public string Refine { get => tags?[Taglet.Refine]??Owner?.Refine ; set { if( value?.Length>0 ) Tag[Taglet.Refine] = value ; else tags.Set(t=>t[Taglet.Refine]=value) ; } }
		public string Dragstr { get => tags?[Taglet.Drag]??(Owner as Path)?.Dragstr ; set { if( value?.Length>0 ) Tag[Taglet.Drag] = value ; else tags.Set(t=>t[Taglet.Drag]=value) ; } }
		public string Gradstr { get => tags?[Taglet.Grade]??(Owner as Path)?.Gradstr ; set { if( value?.Length>0 ) Tag[Taglet.Grade] = value ; else tags.Set(t=>t[Taglet.Grade]=value) ; } }
		public string Flowstr { get => tags?[Taglet.Flow]??(Owner as Path)?.Flowstr ; set { if( value?.Length>0 ) Tag[Taglet.Flow] = value ; else tags.Set(t=>t[Taglet.Flow]=value) ; } }
		#endregion

		#region Vector
		public static Point Zero( DateTime date ) => new Point(date){ Time = TimeSpan.Zero }.Set(p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = 0 ; }) ;
		public override uint Dimension => (uint?)((Quant?[])this)?.Length ?? (uint)Axis.Top ; // Dimension doesn't include Time and Date and At components , as they state is separate fields . Therefore Axis.Time limits index .
		public new Quant? this[ uint axis ] { get => base[axis] ; set { if( this[axis]==value ) return ; base[axis] = value ; Changed(Metax?[axis].Name??((Axis)axis).ToString()) ; } } // because of WFP bug proprty of Binding.Path property resolution on inedexers
		public virtual Quant? this[ Axis axis ]
		{
			get => axis==Axis.Time ? Time.TotalSeconds : axis==Axis.Date ? Date.TotalSeconds() : this[(uint)axis] ;
			set { if( axis<Axis.Lim ) this[(uint)axis] = value ; else if( value is Quant q ) if( axis==Axis.Time ) Time = TimeSpan.FromSeconds(q) ; else if( axis==Axis.Date ) Date = DateTime.MinValue.AddSeconds(q) ; /*else if( axis==Axis.At ) At = (int)q ;*/ }
		}
		public override Mark Mark { get => base.Mark ; set { if( value==Mark ) return ; base.Mark = value ; Changed("Mark") ; } }
		public override DateTime Date { get => base.Date ; set { if( Date==value ) return ; base.Date = value ; Changed("Date") ; } }
		public override TimeSpan Time { get => base.Time ; set { if( Time==value ) return ; base.Time=value ; Changed("Time") ; } }
		/// <summary>
		/// Position within owner .
		/// </summary>
		public override int? No { get => base.No ; set { if( No==value ) return ; base.No = value ; Changed("No") ; } }
		#endregion

		#region Trait
		public Quant? Alti { get => this[Axis.Alt] ; set => this[Axis.Alt] = value ; }
		public Quant? Dist { get => this[Axis.Dist] ; set => this[Axis.Dist] = value ; }
		public Quant? Energy { get => this[Axis.Energy] ; set => this[Axis.Energy] = value ; }
		public Quant? Beat { get => this[Axis.Beat] ; set => this[Axis.Beat] = value ; }
		public Quant? Bit { get => this[Axis.Bit] ; set => this[Axis.Bit] = value ; }
		public Quant? Grade { get => this[Axis.Grade] ; set => this[Axis.Grade] = value ; }
		public Quant? Drag { get => this[Axis.Drag] ; set => this[Axis.Drag] = value ; }
		public Quant? Flow { get => this[Axis.Flow] ; set => this[Axis.Flow] = value ; }
		public Quant? Fuel { get => this[Axis.Flow] ; set => this[Axis.Flow] = value ; }
		#endregion

		#region Quotient
		public virtual Quant? Distance { get => Dist/Transfer ; set => Dist = value*Transfer ; }
		public Quant? Speed => Distance.Quotient(Time.TotalSeconds) ;
		public Quant? Pace => Time.TotalSeconds/Distance ;
		public Quant? Power => Object==Basis.Device.Skierg.Code ? Time.TotalSeconds.Quotient(Dist).PacePower(drag:Basis.Device.Skierg.Draw) : Energy.Quotient(Time.TotalSeconds) ;
		public Quant? Force => Energy.Quotient(Distance) ;
		public Quant? Beatage => Energy.Quotient(Beat) ;
		public Quant? Bitage => Energy.Quotient(Bit) ;
		public Quant? Beatrate => Beat.Quotient(Time.TotalMinutes) ;
		public Quant? Bitrate => Bit.Quotient(Time.TotalMinutes) ;
		public Quant? Granelet { get => Grade.Quotient(Dist) ; set => Grade = value*Dist ; }
		public Quant? Draglet { get => Drag.Quotient(Dist) ; set => Drag = value*Dist ; }
		public Quant? Flowlet { get => Flow.Quotient(Dist) ; set => Flow = value*Dist ; }
		public Bipole? Gradelet => Ascent/Dist ;
		public Bipole? Bendlet => Deviation/Dist ;
		#endregion

		#region Query
		public bool IsGeo => this[Axis.Lon]!=null || this[Axis.Lat]!=null ;
		public Quant Transfer => Basis.Device.Skierg.Code==Object ? Math.Pow(Draglet??1,1D/3D) : 1 ;
		public Quant Resister => Object==Basis.Device.Skierg.Code ? Basis.Device.Skierg.Draw : Drag??Path.SubjectProfile.By(Subject)?.Resi??0 ;
		public override string Exposion => "{0}={1}bW".Comb("{0}/{1}".Comb(Power.Get(p=>$"{Math.Round(p)}W"),Beatrate.Get(b=>$"{Math.Round(b)}`b")),Beatage.use(Math.Round))+$" {Speed*3.6:0.00}km/h" ;
		public override string Trace => $"{Resister.Get(v=>$"Resist={v:0.00}")} {Ascent.Get(v=>$"Ascent={v:0}m")} {Gradelet.Get(v=>$"Grade={v:.000}")} {Deviation.Get(v=>$"Devia={v:0}m")} {Bendlet.Get(v=>$"Bend={v:.000}")} {Quantities} {Mark.nil(m=>m==Mark.No)}" ;
		#endregion

		#region Operation
		public static Point operator|( Point prime , Quant?[] quantities ) => prime.Set(p=>{ for( uint i=0 ; i<quantities?.Length ; ++i ) if( p[i]==null ) p[i] = quantities[i] ; }) ;
		public static Point operator|( Point point , IEnumerable<Point> points ) => point | point.Date.Give(points) ;
		public static implicit operator Quant?[]( Point point ) => point as Pre.Point ;
		public static Point operator/( Point point , Axis axis ) => point / (uint)axis ;
		public static Point operator/( Point point , uint axis ) => point.Set(p=>p[axis]=null) ;
		public static Point operator/( Point point , string axis ) => point / (point.Metax?[axis]??axis.Axis()) ;
		public static Point operator-( Point point , Point offset ) => new Point(new DateTime(point.Date.Ticks+offset.Date.Ticks>>1)){ Time = point.Date-offset.Date }.Set(p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = point[i]-offset[i] ; if( p.IsGeo ) p.Dist = p.Euclid(offset) ; }) ;
		public static Point operator+( Point accu , Point diff ) => accu.Set( p => diff.Set( d => { p.Time += d.Time ; for( uint i=0 ; i<p.Dimension ; ++i ) p[i] += d[i] ; } ) ) ;
		public Geos? Geo => this ;
		#endregion

		#region Handling
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
		protected virtual void Changed( string property ) { propertyChanged.On(this,property) ; (this as Path).Null(p=>p.Initing)?.Edited() ; (Owner as Path).Null(p=>p.Initing)?.Edited() ; }
		#endregion

		#region Equalization
		public override bool Equals( Pointable other ) => other is Point p && base.Equals(p) && tags?.Equals(p?.tags)!=false ;
		public override bool EqualsRestricted( Pointable other ) => other is Point p && base.EqualsRestricted(p) && tags?.Equals(p?.tags)!=false ;
		#endregion

		#region De/Serialization
		protected Point( string text ) : base(text) { ( tags = (Tagger)text.RightFrom(Serialization.Act).LeftFromLast(Serialization.Tag) ).Set(t=>t.Notifier=TagChanged) ; Metax = (Metax)text.RightFrom(Serialization.Tag) ; }
		public static explicit operator Point( string text ) => text?.Contains(Path.Serialization.Separator)==true ? (Path)text : text.Get(t=>new Point(t)) ;
		public static explicit operator string( Point p ) => $"{(string)(p as Pre.Point)}{(string)p.tags}{Serialization.Tag}" ;
		#endregion
	}
	public static class PointExtension
	{
		public static Quant? Draglet( this string value ) => value.Parse<Quant>() ;
		public static string Dragstr( this Quant? value ) => value?.ToString() ;
		public static Quant? Gradlet( this string value ) => value.Parse<Quant>() ;
		public static string Gradstr( this Quant? value ) => value?.ToString() ;
		public static Quant? Flowlet( this string value ) => value.Parse<Quant>() ;
		public static string Flowstr( this Quant? value ) => value?.ToString() ;
	}
}
