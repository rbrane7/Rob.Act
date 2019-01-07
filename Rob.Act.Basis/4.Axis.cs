using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Aid.Extension;

namespace Rob.Act
{
	using System.Collections ;
	using System.ComponentModel ;
	using Quant = Double ;
	public interface Axable : Aid.Gettable<int,Quant?> , Aid.Countable<Quant?> { Func<int,Quant?> Resolver { get; set; } Func<Aspectable,int> Counter { get; set; } string Spec { get; } }
	public class Axe : Axable , INotifyPropertyChanged
	{
		public static Func<IEnumerable<Aspectable>> Aspecter ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Axe( Func<int,Quant?> resolver = null ) => Resolver = resolver ;
		public Axe( Axe source ) { spec = source?.spec ; aspectlet = source?.aspectlet ; aspect = source?.aspect ; resolvelet = source?.resolvelet ; resolver = source?.resolver ; selectlet = source?.selectlet ; selector = source?.selector ; countlet = source?.countlet ; counter = source?.counter ; }
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public Aspectable Source { set { if( DefaultAspect==null ) Aspect = value ; else Resource.Source = value ; } }
		public bool Multi { get => multi ; set { if( value==multi ) return ; multi = value ; Aspect = null ; propertyChanged.On(this,"Multi,Aspects") ; } } bool multi ;
		protected virtual Aspectable DefaultAspect => Multi ? null : Selector?.Invoke(Aspecter?.Invoke()).SingleOrNo() ;
		protected Resourcable Resource => Aspect ?? Aspects as Resourcable ;
		protected virtual Aspectables Aspects => new Aspectables(Selector?.Invoke(Aspecter?.Invoke()).ToArray()) ;
		public virtual Aspectable Aspect { get => aspect ?? ( aspect = DefaultAspect ) ; set { if( aspect==value ) return ; aspect = value ; Resolver = null ; propertyChanged.On(this,"Aspect") ; } } protected Aspectable aspect ;
		public string Aspectlet { get => aspectlet?.ToString() ?? Aspect?.Spec ; set { if( value==Aspectlet ) return ; aspectlet = value.Null(s=>s.No()).Get(v=>new Regex(v)) ; if( aspectlet==null ) Selector = null ; else Selector = s=>s.Where(a=>aspectlet.Match(a.Spec).Success) ; propertyChanged.On(this,"Aspectlet") ; } } Regex aspectlet ;
		public Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> Selector { get => selector ; set { if( selector==value ) return ; selector = value ; Aspect = null ; propertyChanged.On(this,"Selector") ; } } Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> selector ;
		public string Selectlet { get => selectlet ; set { if( selectlet==value ) return ; Selector = value.Compile<Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>>>() ; selectlet = value.Null(s=>s.No()) ; propertyChanged.On(this,"Selectlet") ; } } string selectlet ;
		public Quant? this[ int at ] => (uint)at<Count ? Resolve(at) : null as Quant? ;
		protected internal virtual Quant? Resolve( int at ) => Resolver?.Invoke(at) ;
		public Func<int,Quant?> Resolver { get => resolver ?? ( resolver = (Multi?Resolvelet.Compile<Func<Aspectables,Axe>>().Of(Aspects):Resolvelet.Compile<Func<Aspectable,Axe>>().Of(Aspect)) is Axe a ? a.Resolve : new Func<int,Quant?>(i=>null as Quant?) ) ; set { if( resolver==value ) return ; resolver = value ; propertyChanged.On(this,"Resolver") ; } } Func<int,Quant?> resolver ;
		public string Resolvelet { get => resolvelet ; set { if( value==resolvelet ) return ; resolvelet = value ; Resolver = null ; propertyChanged.On(this,"Resolvelet") ; } } string resolvelet ;
		public virtual int Count => Counter is Func<Aspectable,int> c ? c(Aspect) : DefaultCount ;
		protected virtual int DefaultCount => Resource?.Points.Count ?? 0 ;
		public Func<Aspectable,int> Counter { get => counter ?? ( counter = countlet.Compile<Func<Aspectable,int>>() ) ; set { if( counter==value ) return ; counter = value ; propertyChanged.On(this,"Counter") ; } } Func<Aspectable,int> counter ;
		public string Countlet { get => countlet ; set { if( value==countlet ) return ; countlet = value ; Counter = null ; propertyChanged.On(this,"Countlet") ; } } string countlet ;
		public IEnumerator<Quant?> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		#region ICollection
		object ICollection.SyncRoot => throw new NotImplementedException() ;
		bool ICollection.IsSynchronized => throw new NotImplementedException() ;
		void ICollection.CopyTo( Array array , int index ) => throw new NotImplementedException() ;
		#endregion
		#region Operations
		public static Axe operator+( Axe x , Axe y ) => new Axe( i=>x.Resolve(i)+y.Resolve(i) ) ;
		public static Axe operator-( Axe x , Axe y ) => new Axe( i=>x.Resolve(i)-y.Resolve(i) ) ;
		public static Axe operator*( Axe x , Axe y ) => new Axe( i=>x.Resolve(i)*y.Resolve(i) ) ;
		public static Axe operator/( Axe x , Axe y ) => new Axe( i=>x.Resolve(i)/y.Resolve(i).Nil() ) ;
		public static Axe operator^( Axe x , Axe y ) => new Axe( i => x.Resolve(i) is Quant a && y.Resolve(i) is Quant b ? Math.Pow(a,b) : null as Quant? ) ;
		public static Axe operator+( Axe x , Quant y ) => new Axe( i=>x.Resolve(i)+y ) ;
		public static Axe operator-( Axe x , Quant y ) => new Axe( i=>x.Resolve(i)-y ) ;
		public static Axe operator*( Axe x , Quant y ) => new Axe( i=>x.Resolve(i)*y ) ;
		public static Axe operator/( Axe x , Quant y ) => new Axe( i=>x.Resolve(i)/y.nil() ) ;
		public static Axe operator^( Axe x , Quant y ) => new Axe( i => x.Resolve(i) is Quant a ? Math.Pow(a,y) : null as Quant? ) ;
		public static Axe operator++( Axe x ) => new Axe( i=>x.Resolve(i+1) ) ;
		public static Axe operator--( Axe x ) => new Axe( i=>x.Resolve(i-1) ) ;
		public static Axe operator>>( Axe x , int lev ) => lev<0 ? x<<-lev : lev==0 ? x : new Axe( i=>x.Resolve(i)-x.Resolve(i-1) )>>lev-1 ;
		public static Axe operator<<( Axe x , int lev ) => lev<0 ? x>>-lev : lev==0 ? x : new Axe( i=>i.Steps().Sum(x.Resolve) )>>lev-1 ;
		public static implicit operator Axe( Func<int,Quant?> resolver ) => resolver.Get(r=>new Axe(r)) ;
		public static implicit operator Axe( Quant q ) => new Axe( i=>q ) ;
		public static implicit operator Axe( int q ) => new Axe( i=>q ) ;
		public Axe Round => new Axe( i=>Resolve(i).use(r=>Math.Round(r)) ) ;
		public Axe From( int from ) => new Axe( i=>Resolve(from+i) ) ;
		#endregion
	}
	public partial class Path
	{
		public class Axe : Act.Axe
		{
			readonly Path Context ;
			public virtual Axis Axis { get => axis ; set { base.Spec = ( axis = value ).Stringy() ; Resolver = at=>Context.At(at)?[Axis] ; } } Axis axis ;
			public override string Spec { get => base.Spec ; set { if( value!=null ) Axis = value.Axis() ; base.Spec = value ; } }
			public Axe( Path context ) => Context = context ;
			protected override int DefaultCount => Context.Count ;
			protected override Aspectable DefaultAspect => Context.Spectrum ;
		}
		public static Axable operator&( Path path , string name ) => path.Spectrum[name] ;
		public static Axable operator&( Path path , Axis name ) => path.Spectrum[name] ;
	}
}
