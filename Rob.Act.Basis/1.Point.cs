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
	public enum Taglet { Object , Subject , Locus , Refine , Grade , Flow , Drag , Detail }
	public interface Tagable : IEquatable<Tagable> , IEnumerable<string> { void Add( string item ) ; string this[ int key ] {get;set;} string this[ Taglet tag ] {get;set;} string this[ string key ] {get;set;} int Count {get;} void Clear() ; void Adopt( Tagable tags ) ; string Uri {get;} }
	public class Tagger : List<string> , Tagable
	{
		public static readonly string[] Names = Enum.GetNames(typeof(Taglet)) ;
		public static readonly string Aclutinator = System.Uri.SchemeDelimiter ;
		internal Action<string> Notifier {private get;set;}
		internal Tagger( Action<string> notifier = null ) => Notifier = notifier ;
		public new string this[ int key ] { get => (uint)key<Count ? base[key] : null ; set { if( this[key]==value ) return ; if( value!=null ) InsureCapacity(key) ; if( (uint)key<Count );else return ; base[key] = value ; Notifier?.Invoke(null) ; } }
		public string this[ Taglet key ] { get => this[(int)key] ; set { if( this[key]==value ) return ; this[(int)key] = value ; Notifier?.Invoke(key.ToString()) ; } }
		public string this[ string key ] { get => key.Parse<Taglet>() is Taglet t ? this[t] : MatchIndex(key) is int i ? this[i] : null ; set { if( key.Parse<Taglet>() is Taglet t ) { this[t] = value ; return ; } if( MatchIndex(key) is int i ) this[i] = value ; else Add(value) ; Notifier?.Invoke(key) ; } }
		public bool this[ params string[] tag ] { get => this[ tag as IEnumerable<string> ] ; set => this[ tag as IEnumerable<string> ] = value ; }
		public bool this[ IEnumerable<string> tag ] { get => this[tag] = false ; set { if( value ) Clear() ; AddRange(tag) ; var drag = this[1] ; if( Serialization.IsDraglike(drag) || drag.No()&&Serialization.IsJectlike(this[0])&&Serialization.IsJectlike(this[2]) ) { RemoveAt(1) ; this[Taglet.Drag] = drag.Null() ; } Notifier?.Invoke(null) ; } }
		public new void Add( string tag ) { if( tag==null ) return ; base.Add(tag) ; Notifier?.Invoke(null) ; }
		public void Adopt( Tagable tags ) { Clear() ; tags.Set(AddRange) ; }
		int? MatchIndex( string key ) => MatchIndexes(key).singleOrNil() ;
		IEnumerable<int> MatchIndexes( string key ) => key.Get(k=>new Regex(k).Get(r=>this.IndexesWhere(r.IsMatch))) ;
		void InsureCapacity( int capacity ) { while( Count<=capacity ) base.Add(null) ; }
		public override string ToString() => ToString(false) ;
		public string ToString( bool leaf = false ) => (leaf?this.Skip(2):this).Stringy(' ') ;
		public string Uri => Count.Steps().Select(i=>i<Names.Length?$"{Names[i]}={this[i]}":this[i]).Stringy('&','?') ;
		public bool Equals( Tagable other ) => other is Tagger t && this.SequenceEquate(t,Equals) ;
		static bool Equals( string x , string y ) => x.Null(v=>v.No())==y.Null(v=>v.No()) ;
		#region De/Serialization
		public static explicit operator string( Tagger the ) => the.Stringy(Serialization.Separator) ;
		public static explicit operator Tagger( string text ) => text.Null(v=>v.Void()).Get(t=>new Tagger(t)) ;
		Tagger( string text ) => this[text.Separate(Serialization.Separator,braces:null)] = false ;
		class Serialization
		{
			public const string Separator = " \x1 Tag \x2 " ;
			internal static bool IsDraglike( string tag ) => tag?.All(l=>char.IsDigit(l)||l=='.'||l=='+'||l=='-'||l=='^')==true ;
			internal static bool IsJectlike( string tag ) => tag?.Any(l=>char.IsLetter(l))==true ;
		}
		#endregion
	}
	public class Point : Pre.Point , Accessible<Axis,Quant?> , INotifyPropertyChanged , Medium.Sharable
	{
		public static Quant Vicinability = 3 ;
		protected internal Pathable Owner { get => owner ; set { owner = value ; Path.Medium?.Interact(this) ; } } Pathable owner ;
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
		/// During init faze property chnges are not persisted .
		/// </summary>
		protected Closure Incognite => new Closure(()=>{(Owner as Path).Set(p=>p.Initing=true);Initing=true;},()=>{(Owner as Path).Set(p=>p.Initing=false);Initing=false;}) ;
		/// <summary>
		/// Set to initialization/initialized mode .
		/// </summary>
		public bool Initing { get => initing>0 ; set { if( value ) ++initing ; else --initing ; } }
		/// <summary>
		/// Assotiative text .
		/// </summary>
		public override string Spec { set { if( value!=Spec ) SpecChanged( base.Spec = value ) ; } }
		public override string Action { get => base.Action ; set { if( value==Action ) return ; base.Action = value ; Changed("Action") ; } }
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
		/// <summary>
		/// Is this point a leaf one ? The one owned by <see cref="Path"/> but not the derived <see cref="Path"/> . 
		/// This property is intended to distinguish points , Leafs , which strictly inherit <see cref="Subject"/> and <see cref="Object"/> properties from <see cref="Owner"/> . 
		/// </summary>
		public bool IsLeaf => DistinguishLeaf && (Owner as Path)?.Derived==false ;
		/// <summary>
		/// Are leaf points distinguished form non-leaf ones , <see cref="IsLeaf"/> property . 
		/// </summary>
		public static bool DistinguishLeaf ;
		#endregion

		#region Tags
		void TagChanged( string p ) { tag = null ; if( Spec==Despect ) Spec = null ; Changed(p??"Tags") ; }
		public override Tagable Tag => tags ?? System.Threading.Interlocked.CompareExchange(ref tags,new Tagger(TagChanged),null) ?? tags ; Tagger tags ;
		public string Tags { get => tag ??= tags?.ToString(IsLeaf) ; set { if( value==tag ) return ; tag = null ; (Tag as Tagger)[value.ExtractTags(IsLeaf)] = true ; Changed("Subject,Object,Locus,Refine") ; } } string tag ;
		public string Subject { get => tags?[Taglet.Subject].Null()??Owner?.Subject ; set { if( value?.Length>0 ) Tag[Taglet.Subject] = value ; else tags.Set(t=>t[Taglet.Subject]=value) ; } }
		public string Object { get => tags?[Taglet.Object].Null()??Owner?.Object ; set { if( value?.Length>0 ) Tag[Taglet.Object] = value ; else tags.Set(t=>t[Taglet.Object]=value) ; } }
		public string Locus { get => tags?[Taglet.Locus].Null()??Owner?.Locus ; set { if( value?.Length>0 ) Tag[Taglet.Locus] = value ; else tags.Set(t=>t[Taglet.Locus]=value) ; } }
		public string Refine { get => tags?[Taglet.Refine].Null()??Owner?.Refine ; set { if( value?.Length>0 ) Tag[Taglet.Refine] = value ; else tags.Set(t=>t[Taglet.Refine]=value) ; } }
		public string Detail { get => tags?[Taglet.Detail].Null()??Owner?.Detail ; set { if( value?.Length>0 ) Tag[Taglet.Detail] = value ; else tags.Set(t=>t[Taglet.Detail]=value) ; } }
		public string Dragstr { get => tags?[Taglet.Drag].Null()??(Owner as Path)?.Dragstr ; set { if( value?.Length>0 ) Tag[Taglet.Drag] = value ; else tags.Set(t=>t[Taglet.Drag]=value) ; } }
		public string Gradstr { get => tags?[Taglet.Grade].Null()??(Owner as Path)?.Gradstr ; set { if( value?.Length>0 ) Tag[Taglet.Grade] = value ; else tags.Set(t=>t[Taglet.Grade]=value) ; } }
		public string Flowstr { get => tags?[Taglet.Flow].Null()??(Owner as Path)?.Flowstr ; set { if( value?.Length>0 ) Tag[Taglet.Flow] = value ; else tags.Set(t=>t[Taglet.Flow]=value) ; } }
		public string Restr { get => $"{Gradstr} {Flowstr} {Dragstr}" ; set { value.Separate(' ').Set(v=>{ using(Incognite){ Gradstr = v.At(0) ; Flowstr = v.At(1) ; Dragstr = v.At(2) ; } Energize() ; }) ; } }
		void Energize() { var dflt = Basis.Energing.On(Object) ; Reslet = (Gradstr.Gradlet(dflt?.Grade),Flowstr.Flowlet(dflt?.Flow),Dragstr.Draglet(dflt?.Drag)) ; }
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
		public override Mark Mark { get => base.Mark ; set { if( value==Mark ) return ; var dif = Mark^value ; base.Mark = value ; Changed("Mark") ; if( Owner is Path o && (o.Marker<value||dif>Mark.No) ) o.Remark(dif) ; } }
		public override DateTime Date { get => base.Date ; set { if( Date==value ) return ; base.Date = value ; Changed("Date") ; } }
		public override TimeSpan Time { get => base.Time ; set { if( Time==value ) return ; base.Time = value ; Changed("Time") ; } }
		public virtual Quant? Age => (Owner as Path)?.Age ;
		public virtual Quant? Fage => (Owner as Path)?.Fage ;
		/// <summary>
		/// Position within owner .
		/// </summary>
		public override int? No { get => base.No ; set { if( No==value ) return ; base.No = value ; Changed("No") ; } }
		public virtual byte? Vicination => (((this+1)?.Distance-Distance??Distance-(this-1)?.Distance)/Vicinability).use(v=>(byte)Math.Ceiling(v)) ; // (Distance/No/Vicinability).use(v=>(byte)Math.Ceiling(v)) ;
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
		public (Quant? Grad,Quant? Flow,Quant? Drag) Rest { get => (Grade,Flow,Drag) ; set { if( value==Rest ) return ; using(Incognite){ Grade = value.Grad ; Flow = value.Flow ; Drag = value.Drag ; } Changed("Rest") ; } }
		#endregion

		#region Quotient
		public virtual Quant? Distance { get => Dist/Transfer ; set => Dist = value*Transfer ; }
		public Quant? Speed => Distance.Quotient(Time.TotalSeconds) ;
		public Quant? Pace => Time.TotalSeconds.Quotient(Distance) ;
		public Quant? Power => Object==Basis.Device.Skierg.Code ? Time.TotalSeconds.Quotient(Dist).PacePower(drag:Basis.Device.Skierg.Draw) : Energy.Quotient(Time.TotalSeconds) ;
		public Quant? Force => Energy.Quotient(Distance) ;
		public virtual Quant? Beatage => Energy.Quotient(Beat) ;
		public virtual Quant? Bitage => Energy.Quotient(Bit) ;
		public virtual Quant? Beatrate => Beat.Quotient(Time.TotalSeconds) ;
		public virtual Quant? Bitrate => Bit.Quotient(Time.TotalSeconds) ;
		public Quant? Granlet { get => Grade.Quotient(Dist) ; set => Grade = value*Dist ; }
		public Quant? Draglet { get => Drag.Quotient(Dist) ; set => Drag = value*Dist ; }
		public Quant? Flowlet { get => Flow.Quotient(Dist) ; set => Flow = value*Dist ; }
		public Bipole? Gradlet => Ascent/Dist ;
		public Bipole? Bendlet => Deviation/Dist ;
		public (Quant? Grad,Quant? Flow,Quant? Drag) Reslet { get => (Granlet,Flowlet,Draglet) ; set { if( value==Reslet ) return ; using(Incognite){ Granlet = value.Grad ; Flowlet = value.Flow ; Draglet = value.Drag ; } Changed("Grade,Flow,Drag,Reslet") ; } }
		#endregion

		#region Query
		public bool IsGeo => this[Axis.Lon] is not null || this[Axis.Lat] is not null ;
		public bool IsGeos => this[Axis.Lon] is not null && this[Axis.Lat] is not null ;
		public Quant Transfer => Basis.Device.Skierg.Code==Object ? Math.Pow(Draglet??1,1D/3D) : 1 ;
		public Quant Resister => Object==Basis.Device.Skierg.Code ? Basis.Device.Skierg.Draw : Drag??Path.SubjectProfile.By(Subject)?.Resi??0 ;
		public override string Exposion => "{0}={1}bW".Comb("{0}/{1}".Comb(Power.Get(p=>$"{Math.Round(p)}W"),Beatrate.Get(b=>$"{Math.Round(b)}`b")),Beatage.use(Math.Round))+$" {Speed*3.6:0.00}km/h" ;
		public override string Trace => $"{Resister.Get(v=>$"Resist={v:0.00}")} {Ascent.Get(v=>$"Ascent={v:0}m")} {Gradlet.Get(v=>$"Grade={v:.000}")} {Deviation.Get(v=>$"Devia={v:0}m")} {Bendlet.Get(v=>$"Bend={v:.000}")} {Quantities} {Mark.nil(m=>m==Mark.No)}" ;
		#endregion

		#region Operation
		public static Point operator+( Point point , int dist ) => (point?.No).Get(n=>point.Owner?[n+dist]) as Point ;
		public static Point operator-( Point point , int dist ) => (point?.No).Get(n=>point.Owner?[n-dist]) as Point ;
		public static Point operator|( Point prime , Quant?[] quantities ) => prime.Set(p=>{ for( uint i=0 ; i<quantities?.Length ; ++i ) if( p[i]==null ) p[i] = quantities[i] ; }) ;
		public static Point operator|( Point point , IEnumerable<Point> points ) => point | point.Date.Give(points) ;
		public static implicit operator Quant?[]( Point point ) => point as Pre.Point ;
		public static Point operator/( Point point , Axis axis ) => point / (uint)axis ;
		public static Point operator/( Point point , uint axis ) => point.Set(p=>p[axis]=null) ;
		public static Point operator/( Point point , string axis ) => (point.Metax?[axis]??axis.Axis()) is uint ax ? point/ax : null ;
		public static Point operator-( Point point , Point offset ) => new Point(new DateTime(point.Date.Ticks+offset.Date.Ticks>>1)){ Time = point.Date-offset.Date }.Set(p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = point[i]-offset[i] ; if( p.IsGeo ) p.Dist = p.Euclid(offset) ; }) ;
		public static Point operator+( Point accu , Point diff ) => accu.Set( p => diff.Set( d => { p.Time += d.Time ; for( uint i=0 ; i<p.Dimension ; ++i ) p[i] += d[i] ; } ) ) ;
		public Geos? Geo => this ;
		public Geos? Aim => No is int at ? (Owner?[at+1] as Point)?.Geo is Geos aif ? aif-Geo : (Owner?[at-1] as Point)?.Geo is Geos aib ? Geo-aib : null : null ;
		#endregion

		#region Handling
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
		protected virtual void Changed( string property ) { propertyChanged.On(this,property) ; (this as Path).Null(p=>p.Initing)?.Edited() ; (Owner as Path).Null(p=>p.Initing)?.Edited() ; if( !Initing ) Path.Medium?.Interact(this) ; }
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
		public static Quant? Gradlet( this string value , Quant? dflt = null ) { var lev = value.RightFrom('^').Null(_=>dflt==null) ; return (lev??value).Parse<Quant>().Get(v=>lev==null||dflt==null?v:Math.Exp(v/10)*dflt) ; }
		public static string Gradstr( this Quant? value , Quant? dflt = null ) => value.Nil(v=>v.Equal(dflt)).use(v=>dflt.use(d=>Math.Log(v/d)*10)??v).Get(v=>dflt!=null?$"^{v:0.###}":v.ToString()) ;
		public static Quant? Flowlet( this string value , Quant? dflt = null ) { var lev = value.RightFrom('^').Null(_=>dflt==null) ; return (lev??value).Parse<Quant>().Get(v=>lev==null||dflt==null?v:Math.Exp(v)*dflt) ; }
		public static string Flowstr( this Quant? value , Quant? dflt = null ) => value.Nil(v=>v.Equal(dflt)).use(v=>dflt.use(d=>Math.Log(v/d))??v).Get(v=>dflt!=null?$"^{v:0.###}":v.ToString()) ;
		public static Quant? Draglet( this string value , Quant? dflt = null ) { var lev = value.RightFrom('^').Null(_=>dflt==null) ; return (lev??value).Parse<Quant>().Get(v=>lev==null||dflt==null?v:Math.Exp(v/3)*dflt) ; }
		public static string Dragstr( this Quant? value , Quant? dflt = null ) => value.Nil(v=>v.Equal(dflt)).use(v=>dflt.use(d=>Math.Log(v/d)*3)??v).Get(v=>dflt!=null?$"^{v:0.###}":v.ToString()) ;
		static bool Equal( this Quant x , Quant? y ) => x==y || y.use(v=>Math.Abs(x-v))<=0.001*(Math.Abs(x)+y.use(Math.Abs)) ;
	}
}
