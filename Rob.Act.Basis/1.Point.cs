﻿using System;
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
	public enum Taglet { Object , Drag , Subject , Locus , Refine , Draw }
	public interface Tagable : IEquatable<Tagable> , IEnumerable<string> { void Add( string item ) ; string this[ int key ] { get ; set ; } string this[ Taglet tag ] { get ; set ; } string this[ string key ] { get ; set ; } int Count { get ; } void Clear() ; void Adopt( Tagable tags ) ; string Uri { get ; } }
	public class Tagger : List<string> , Tagable
	{
		public static readonly string[] Names = Enum.GetNames(typeof(Taglet)) ;
		readonly Action<string> Notifier ;
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
		#region Construction
		public Point( DateTime date ) : base(date) {}
		public Point( Point point ) : base(point) {}
		public override void Adopt( Pointable point ) { base.Adopt(point) ; if( (point as Point)?.Tags!=null || Tags!=null ) Tag.Adopt(point.Tag) ; }
		#endregion

		#region State
		/// <summary>
		/// Assotiative text .
		/// </summary>
		public override string Spec { set { if( value!=Spec ) SpecChanged( base.Spec = value ) ; } }
		protected override string Despec( string act ) => Tags is string t ? $"{base.Despec(act)} {t}" : base.Despec(act) ;
		protected virtual void SpecChanged( string value ) => propertyChanged.On(this,"Spec") ;
		/// <summary>
		/// Ascent of the path .
		/// </summary>
		public Bipole? Asc { get; set; }
		/// <summary>
		/// Deviation of the path .
		/// </summary>
		public Bipole? Dev { get; set; }
		#endregion

		#region Tags
		public override Tagable Tag => tags ?? System.Threading.Interlocked.CompareExchange(ref tags,new Tagger(p=>{tag=null;if(Spec==Despect)Spec=null;propertyChanged.On(this,p??"Tags");}),null) ?? tags ; Tagger tags ;
		public string Tags { get => tag ?? ( tag = tags.Stringy() ) ; set { if( value==tag ) return ; tag = null ; (Tag as Tagger)[value.ExtractTags()] = true ; propertyChanged.On(this,"Subject,Object,Locus,Refine") ; } } string tag ;
		public string Subject { get => tags?[Taglet.Subject] ; set { if( value?.Length>0 ) Tag[Taglet.Subject] = value ; else tags.Set(t=>t[Taglet.Subject]=value) ; } }
		public string Object { get => tags?[Taglet.Object] ; set { if( value?.Length>0 ) Tag[Taglet.Object] = value ; else tags.Set(t=>t[Taglet.Object]=value) ; } }
		public string Locus { get => tags?[Taglet.Locus] ; set { if( value?.Length>0 ) Tag[Taglet.Locus] = value ; else tags.Set(t=>t[Taglet.Locus]=value) ; } }
		public string Refine { get => tags?[Taglet.Refine] ; set { if( value?.Length>0 ) Tag[Taglet.Refine] = value ; else tags.Set(t=>t[Taglet.Refine]=value) ; } }
		public string Dragstr { get => tags?[Taglet.Drag] ; set { if( value?.Length>0 ) Tag[Taglet.Drag] = value ; else tags.Set(t=>t[Taglet.Drag]=value) ; } }
		public string Drawstr { get => tags?[Taglet.Draw] ; set { if( value?.Length>0 ) Tag[Taglet.Draw] = value ; else tags.Set(t=>t[Taglet.Draw]=value) ; } }
		#endregion

		#region Vector
		public static Point Zero( DateTime date ) => new Point(date){ Time = TimeSpan.Zero }.Set( p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = 0 ; } ) ;
		public override uint Dimension => (uint?)((Quant?[])this)?.Length ?? (uint)Axis.Time ;
		public new Quant? this[ uint axis ] { get => base[axis] ; set => base[axis] = value ; } // because of WFP bug proprty of Binding.Path property resolution on inedexers
		public virtual Quant? this[ Axis axis ] { get => axis==Axis.Time ? Time.TotalSeconds : axis==Axis.Date ? Date.TotalSeconds() : this[(uint)axis] ; set { if( axis<Axis.Time ) this[(uint)axis] = value ; else if( axis>Axis.Date ) this[(uint)axis-2] = value ; else if( value is Quant q ) if( axis==Axis.Time ) Time = TimeSpan.FromSeconds(q) ; else Date = DateTime.MinValue.AddSeconds(q) ; } }
		#endregion

		#region Trait
		public Quant? Alti { get => this[Axis.Alt] ; set => this[Axis.Alt] = value ; }
		public Quant? Dist { get => this[Axis.Dist] ; set => this[Axis.Dist] = value ; }
		public Quant? Energy { get => this[Axis.Energy] ; set => this[Axis.Energy] = value ; }
		public Quant? Beat { get => this[Axis.Beat] ; set => this[Axis.Beat] = value ; }
		public Quant? Bit { get => this[Axis.Bit] ; set => this[Axis.Bit] = value ; }
		public Quant? Effort { get => this[Axis.Effort] ; set => this[Axis.Effort] = value ; }
		public Quant? Drag { get => this[Axis.Drag] ; set => this[Axis.Drag] = value ; }
		public Quant? Draw { get => this[Axis.Draw] ; set => this[Axis.Draw] = value ; }
		#endregion

		#region Quotient
		public Quant? Distance => Dist / Transfer ;
		public Quant? Speed => Distance.Quotient(Time.TotalSeconds) ;
		public Quant? Pace => Time.TotalSeconds / Distance ;
		public Quant? Power => Energy.Quotient(Time.TotalSeconds) ;
		public Quant? Force => Energy.Quotient(Distance) ;
		public Quant? Beatage => Energy.Quotient(Beat) ;
		public Quant? Bitage => Energy.Quotient(Bit) ;
		public Quant? Beatrate => Beat.Quotient(Time.TotalMinutes) ;
		public Quant? Bitrate => Bit.Quotient(Time.TotalMinutes) ;
		public Quant? Draglet { get => Drag.Quotient(Bit) ; set => Drag = value*Bit ; }
		public Quant? Drawlet { get => Draw.Quotient(Bit) ; set => Draw = value*Bit ; }
		public Bipole? Gradelet => Asc / Dist ;
		public Bipole? Bendlet => Dev / Dist ;
		#endregion

		#region Query
		public bool IsGeo => this[Axis.Lon]!=null || this[Axis.Lat]!=null ;
		public Quant Transfer => Math.Pow(Draglet??1,1D/3D) ;
		public Quant Resister => (Draw??1)*(Drag??1) ;
		public override string Exposion => "{0}={1}bW".Comb("{0}/{1}".Comb(Power.Get(p=>$"{Math.Round(p)}W"),Beatrate.Get(b=>$"{Math.Round(b)}`b")),Beatage.use(Math.Round))+$" {Speed*3.6:0.00}km/h" ;
		public override string Trace => $"{Resister.Get(v=>$"Resist={v:0.00}")} {Asc.Get(v=>$"Ascent={v:0}m")} {Gradelet.Get(v=>$"Grade={v:.000}")} {Dev.Get(v=>$"Devia={v:0}m")} {Bendlet.Get(v=>$"Bend={v:.000}")} {Quantities} {Mark.nil(m=>m==Mark.No)}" ;
		#endregion

		#region Operation
		public static Point operator|( Point prime , Quant?[] quantities ) => prime.Set(p=>{ for( uint i=0 ; i<quantities?.Length ; ++i ) if( p[i]==null ) p[i] = quantities[i] ; }) ;
		public static Point operator|( Point point , IEnumerable<Point> points ) => point | point.Date.Give(points) ;
		public static implicit operator Quant?[]( Point point ) => point as Pre.Point ;
		public static Point operator/( Point point , Axis axis ) => point / (uint)axis ;
		public static Point operator/( Point point , uint axis ) => point.Set(p=>p[axis]=null) ;
		public static Point operator/( Point point , string axis ) => point / (point.Metax?[axis]??axis.Axis(point.Dimension)) ;
		public static Point operator-( Point point , Point offset ) => new Point(new DateTime(point.Date.Ticks+offset.Date.Ticks>>1)){ Time = point.Date-offset.Date }.Set( p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = point[i]-offset[i] ; if( p.IsGeo ) p.Dist = p.Euclid(offset) ; } ) ;
		public static Point operator+( Point accu , Point diff ) => accu.Set( p => diff.Set( d => { p.Time += d.Time ; for( uint i=0 ; i<p.Dimension ; ++i ) p[i] += d[i] ; } ) ) ;
		public Geos? Geo => this ;
		#endregion

		#region GUI
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
		#endregion

		#region Comparison
		public override bool Equals( Pointable other ) => other is Point p && base.Equals(p) && tags?.Equals(p?.tags)!=false ;
		public override bool EqualsRestricted( Pointable other ) => other is Point p && base.EqualsRestricted(p) && tags?.Equals(p?.tags)!=false ;
		#endregion

		#region De/Serialization
		protected Point( string text ) : base(text) { tags = (Tagger)text.RightFrom(Serialization.Act).LeftFromLast(Serialization.Tag) ; Metax = (Metax)text.RightFrom(Serialization.Tag) ; }
		public static explicit operator Point( string text ) => text?.Contains(Path.Serialization.Separator)==true ? (Path)text : text.Get(t=>new Point(t)) ;
		public static explicit operator string( Point p ) => $"{(string)(p as Pre.Point)}{(string)p.tags}{Serialization.Tag}" ;
		#endregion
	}
	public static class PointExtension
	{
		public static Quant? Draglet( this string value ) => value.Parse<int>()/100D ;
		public static string Dragstr( this Quant? value ) => (value*100)?.ToString() ;
		public static Quant? Drawlet( this string value ) => value.Parse<int>()/100D ;
		public static string Drawstr( this Quant? value ) => (value*100)?.ToString() ;
	}
}
