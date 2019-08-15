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
	public interface Axable : Aid.Gettable<int,Quant?> , Aid.Gettable<Quant,Quant> , Aid.Countable<Quant?> { Func<int,Quant?> Resolver { get; set; } Func<Aspectable,int> Counter { get; set; } Func<Axe,IEnumerable<Quant>> Distributor { get; set; } string Spec { get; } }
	public class Axe : Axable , INotifyPropertyChanged
	{
		public readonly static Axe No = new Axe() ;
		public static Func<IEnumerable<Aspectable>> Aspecter ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Axe() : this(null,null) {} // Default constructor must be present to enable DataGrid implicit Add .
		public Axe( Func<int,Quant?> resolver = null , Func<Aspectable,int> counter = null ) { this.resolver = resolver ; this.counter = counter ; Quantile = new Quantile(this) ; }
		public Axe( Axe source ) { spec = source?.spec ; aspectlet = source?.aspectlet ; aspect = source?.aspect ; resolvelet = source?.resolvelet ; resolver = source?.resolver ; selectlet = source?.selectlet ; selector = source?.selector ; countlet = source?.countlet ; counter = source?.counter ; distribulet = source?.distribulet ; distributor = source?.distributor ; Quantizer = source?.Quantizer ; multi = source?.multi??false ; bond = source?.bond ; }
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public string Binder { get => bond ; set { if( value==bond ) return ; bond = value ; propertyChanged.On(this,"Binder") ; } } string bond ;
		public Aspectable Source { set { if( DefaultAspect==null ) Aspect = value ; else Resource.Source = value ; } }
		public Aspectable[] Sources { set { if( DefaultAspect==null ) Aspects = new Aspectables(value) ; else Resource.Sources = value ; } }
		public bool Multi { get => multi ; set { if( value==multi ) return ; multi = value ; Resolver = null ; Aspect = null ; propertyChanged.On(this,"Multi,Aspects") ; } } bool multi ;
		protected virtual Aspectable DefaultAspect => Multi ? null : Selector?.Invoke(Aspecter?.Invoke()).SingleOrNo() ;
		protected Resourcable Resource => Aspect ?? Aspects as Resourcable ;
		protected virtual Aspectables Aspects { get => aspects.Count>0 ? aspects : ( aspects = new Aspectables(Selector?.Invoke(Aspecter?.Invoke()).ToArray()) ) ; set { aspects = value ; Resolver = null ; propertyChanged.On(this,"Aspects") ; } } Aspectables aspects ;
		public virtual Aspectable Aspect { get => aspect ?? ( aspect = DefaultAspect ) ; set { if( aspect==value ) return ; aspect = value ; Resolver = null ; propertyChanged.On(this,"Aspect") ; } } protected Aspectable aspect ;
		public string Aspectlet { get => aspectlet?.ToString() ?? Aspect?.Spec ; set { if( value==Aspectlet ) return ; aspectlet = value.Null(s=>s.No()).Get(v=>new Regex(v)) ; if( aspectlet==null ) Selector = null ; else Selector = s=>s.Where(a=>aspectlet.Match(a.Spec).Success) ; propertyChanged.On(this,"Aspectlet") ; } } Regex aspectlet ;
		public Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> Selector { get => selector ; set { if( selector==value ) return ; selector = value ; Aspect = null ; propertyChanged.On(this,"Selector") ; } } Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> selector ;
		public string Selectlet { get => selectlet ; set { if( selectlet==value ) return ; Selector = value.Compile<Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>>>() ; selectlet = value.Null(s=>s.No()) ; propertyChanged.On(this,"Selectlet") ; } } string selectlet ;
		public Quant this[ Quant at ] => this.Count(q=>q>=at) ;
		public Quant this[ Quant at , Axe ax ] { get { if( ax==null ) return this[at] ; Quant rez = 0 ; for( int i=0 , count=Count ; i<count ; ++i ) if( Resolve(i)>=at ) rez += ax[i+1]-ax[i]??0 ; return rez ; } }
		public Quant? this[ int at ] => (uint)at<Count ? Resolve(at) : null as Quant? ;
		protected internal virtual Quant? Resolve( int at ) => Resolver?.Invoke(at) ;
		Axe Coaxe => Multi?Aspects.Get(a=>Resolvelet.Compile<Func<Aspectables,Axe>>(use:"Rob.Act").Of(a)):Aspect.Get(a=>Resolvelet.Compile<Func<Aspectable,Axe>>(use:"Rob.Act").Of(a)) ;
		public Func<int,Quant?> Resolver { get => resolver ?? ( resolver = Coaxe.Set(x=>{if(counter==null)counter=x.counter;}) is Axe a ? i=>a[i] : new Func<int,Quant?>(i=>null as Quant?) ) ; set { if( resolver==value ) return ; resolver = value ; propertyChanged.On(this,"Resolver") ; } } Func<int,Quant?> resolver ;
		public string Resolvelet { get => resolvelet ; set { if( value==resolvelet ) return ; resolvelet = value ; Resolver = null ; propertyChanged.On(this,"Resolvelet") ; } } string resolvelet ;
		public virtual int Count => Counter is Func<Aspectable,int> c ? c(Aspect) : DefaultCount ;
		public bool Counts => counter!=null ;
		protected virtual int DefaultCount => Resource?.Points.Count ?? 0 ;
		public Func<Aspectable,int> Counter { get => counter ?? ( counter = countlet.Compile<Func<Aspectable,int>>()/*??Coaxe.Set(x=>{if(resolver==null)resolver=i=>x[i];})?.counter*/ ) ; set { if( counter==value ) return ; counter = value ; propertyChanged.On(this,"Counter") ; } } Func<Aspectable,int> counter ;
		public string Countlet { get => countlet ; set { if( value.Null(c=>c.No())==countlet ) return ; countlet = value ; Counter = null ; propertyChanged.On(this,"Countlet") ; } } string countlet ;
		public Func<Axe,IEnumerable<Quant>> Distributor { get => distributor ?? ( distributor = distribulet.Compile<Func<Axe,IEnumerable<Quant>>>() ?? (a=>null) ) ; set { if( distributor==value ) return ; distributor = value ; propertyChanged.On(this,"Distributor,Quantile") ; } } Func<Axe,IEnumerable<Quant>> distributor ;
		public IEnumerable<Quant> Distribution => Distributor?.Invoke(this) ?? DefaultDistribution ; //?? Enumerable.Empty<Quant>() ;
		protected virtual IEnumerable<Quant> DefaultDistribution => this.Refine().ToArray().Null(d=>d.Length<=0) ;
		public string Distribulet { get => distribulet ; set { if( value==distribulet ) return ; distribulet = value ; Distributor = null ; propertyChanged.On(this,"Distribulet") ; } } string distribulet ;
		public IEnumerator<Quant?> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public Quantile Quantile { get ; private set ; }
		public Func<Quantile,IEnumerable<Quant>> Quantizer { get => Quantile.Quantizer ; set => propertyChanged.On(this,"Quantizer,Quantile",Quantile=new Quantile(this,value)) ; }
		public string Quantlet { get => quantlet ; set => propertyChanged.On(this,"Quantlet",Quantizer=(quantlet=value).Compile<Func<Quantile,IEnumerable<Quant>>>()) ; } string quantlet ;
		#region Operations
		public static Quant? operator+( Axe x ) => x?.Sum ;
		public static Axe operator-( Axe x ) => x==null ? No : new Axe( i=>-x.Resolve(i) , a=>x.Count ) ;
		public static Axe operator+( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)+y.Resolve(i) , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator-( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)-y.Resolve(i) , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator*( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)*y.Resolve(i) , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator/( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)/y.Resolve(i).Nil() , a=>Math.Max(x.Count,y.Count) ) ;
		//public static Axe operator^( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i => x.Resolve(i) is Quant a && y.Resolve(i) is Quant b ? Math.Pow(a,b) : null as Quant? , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator+( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)+y , a=>x.Count ) ;
		public static Axe operator-( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)-y , a=>x.Count ) ;
		public static Axe operator*( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)*y , a=>x.Count ) ;
		public static Axe operator/( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)/y.nil() , a=>x.Count ) ;
		public static Axe operator+( Quant x , Axe y ) => y==null ? No : new Axe( i=>x+y.Resolve(i) , a=>y.Count ) ;
		public static Axe operator-( Quant x , Axe y ) => y==null ? No : new Axe( i=>x-y.Resolve(i) , a=>y.Count ) ;
		public static Axe operator*( Quant x , Axe y ) => y==null ? No : new Axe( i=>x*y.Resolve(i) , a=>y.Count ) ;
		public static Axe operator/( Quant x , Axe y ) => y==null ? No : new Axe( i=>x/y.Resolve(i).Nil() , a=>y.Count ) ;
		public static Axe operator/( bool x , Axe y ) => y==null ? No : new Axe( i=>1D/y.Resolve(i).Nil() , a=>y.Count ) ;
		public static Axe operator^( Axe x , Quant y ) => x==null ? No : new Axe( i => x.Resolve(i) is Quant a ? Math.Pow(a,y) : null as Quant? , a=>x.Count ) ;
		public static Axe operator^( Quant x , Axe y ) => y==null ? No : new Axe( i => y.Resolve(i) is Quant a ? Math.Pow(x,a) : null as Quant? , a=>y.Count ) ;
		public static Axe operator^( Axe x , bool y ) => x==null ? No : new Axe( i => x.Resolve(i) is Quant a ? a>0?Math.Log(a):null as Quant? : null as Quant? , a=>x.Count ) ;
		public static Axe operator^( bool x , Axe y ) => y==null ? No : new Axe( i => y.Resolve(i) is Quant a ? Math.Exp(a) : null as Quant? , a=>y.Count ) ;
		public static Axe operator|( Axe x , Axe y ) => x==null ? y : y==null ? x : new Axe( i => i<x.Count ? x.Resolve(i) : y.Resolve(i-x.Count) , a=>x.Count+y.Count ) ;
		public static Axe operator++( Axe x ) => x==null ? No : new Axe( i=>x.Resolve(i+1) , a=>x.Count ) ;
		public static Axe operator--( Axe x ) =>x==null ? No :  new Axe( i=>x.Resolve(i-1) , a=>x.Count ) ;
		public static Axe operator>>( Axe x , int lev ) => x==null ? No : lev<0 ? x<<-lev : lev==0 ? x : new Axe( i=>x.Resolve(i)-x.Resolve(i-1) , a=>x.Count )>>lev-1 ;
		public static Axe operator<<( Axe x , int lev ) => x==null ? No : lev<0 ? x>>-lev : lev==0 ? x : new Axe( i=>i.Steps().Sum(x.Resolve) , a=>x.Count )>>lev-1 ;
		public static Axe operator^( Axe x , Axe y ) => x==null ? No : x.Shift(y) ;
		public static Axe operator%( Axe x , int dif ) => x==null ? No : new Axe( i=>x.Diff(i,dif) , a=>x.Count-dif ) ;
		public static Lap operator%( Axe x , Quant dif ) => x.By(dif) ;
		public static Axe operator/( Axe x , Lap dif ) => x==null ? No : new Axe( i=>x.Diff(i,dif[i]) , a=>x.Count ) ;
		public static Axe operator%( Axe x , Axe y ) => x==null ? No : x.Rift(y) ;
		public static IEnumerable<int> operator>( Axe x , Quant val ) => x?.Count.Steps().Where(i=>x[i]>val) ;
		public static IEnumerable<int> operator<( Axe x , Quant val ) => x?.Count.Steps().Where(i=>x[i]<val) ;
		public static IEnumerable<int> operator>=( Axe x , Quant val ) => x?.Count.Steps().Where(i=>x[i]>=val) ;
		public static IEnumerable<int> operator<=( Axe x , Quant val ) => x?.Count.Steps().Where(i=>x[i]<=val) ;
		public static IEnumerable<int> operator>( Quant val , Axe x ) => x<val ;
		public static IEnumerable<int> operator<( Quant val , Axe x ) => x>val ;
		public static IEnumerable<int> operator>=( Quant val , Axe x ) => x<=val ;
		public static IEnumerable<int> operator<=( Quant val , Axe x ) => x>=val ;
		public static IEnumerable<int> operator>( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]>y[i]) ;
		public static IEnumerable<int> operator<( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]<y[i]) ;
		public static IEnumerable<int> operator>=( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]>=y[i]) ;
		public static IEnumerable<int> operator<=( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]<=y[i]) ;
		public static implicit operator Axe( Func<int,Quant?> resolver ) => resolver.Get(r=>new Axe(r)) ;
		public static implicit operator Axe( Quant q ) => new Axe( i=>q , a=>a?.Points.Count??1 ) ;
		public static implicit operator Axe( int q ) => new Axe( i=>q , a=>a?.Points.Count??1 ) ;
		public Axe Round => new Axe( i=>Resolve(i).use(Math.Round) , a=>Count ) ;
		public Quant? Sum => this.Sum() ;
		public Axe Skip( int count ) => new Axe( i=>Resolve(count+i) , a=>Math.Max(0,Count-count) ) ;
		public Axe Wait( int count ) => new Axe( i=>Resolve(i<count?0:i-count) , a=>Math.Max(0,Count-count) ) ;
		public Axe Take( int count ) => new Axe( Resolve , a=>Math.Min(count,Count) ) ;
		public Axe For( IEnumerable<int> fragment ) => fragment?.ToArray().Get(f=>new Axe(i=>Resolve(f[i]),a=>f?.Length??0)) ?? No ;
		public Axe this[ IEnumerable<int> fragment ] => For(fragment) ;
		public Axe Shift( Axe upon , Quant quo = 0 ) => upon==null ? No : new Axe( i=>(quo*i).Get(at=>Shift(upon,(int)at,(int)((i-at)/2))) , a=>Count ) ;
		public Axe Drift( Axe upon , Quant quo = 0 ) => upon.Shift(this,quo) ;
		Quant? Diff( int at , int dif ) => (Resolve(at+dif)-Resolve(at))*Math.Sign(dif) ;
		Quant? Quot( Axe upon , int at , int dif ) => Diff(at,dif)/upon.Diff(at,dif).Nil() ;
		Quant? Shift( Axe upon , int at , int dis ) => Quot(upon,at+dis,dis)/Quot(upon,at,dis).Nil() ;
		public Axe Rift( Axe upon , uint quo = 9 ) => upon==null ? No : new Axe( i=>Shift(upon,i,((Count-i)>>1)-1) , a=>(int)(Count*quo/(1D+quo)) ) ;
		public Lap Lap( Quant dif ) => new Lap(this,dif) ;
		public Lap By( Quant dif ) => Lap(dif) ;
		#endregion
		#region De/Serialization
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Axe( string text ) => text.Separate(Serialization.Separator,braces:null).Get(t=>new Axe{Spec=t.At(0),Multi=t.At(1)==Serialization.Multier,Resolvelet=t.At(2),Countlet=t.At(3),Selectlet=t.At(4),Distribulet=t.At(5),Quantlet=t.At(6),Binder=t.At(7)}) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Axe aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.spec,a.multi?Serialization.Multier:string.Empty,a.resolvelet,a.countlet,a.selectlet,a.distribulet,a.quantlet,a.Binder)) ;
		static class Serialization { public const string Separator = " \x1 Axlet \x2 " ; public const string Multier = "*" ; }
		#endregion
	}
	public class Quantile : Aid.Gettable<int,Quant> , Aid.Countable<Quant>
	{
		static Quant[] EmptyDis = new Quant[0] ;
		public Axe Ax => Axe ?? Context?.Ax ; readonly Quantile Context ; readonly Axe Axe ;
		internal Func<Quantile,IEnumerable<Quant>> Quantizer ;
		Quant[] Distribution { get { if( distribution==null ) try { distribution = Source ?? EmptyDis ; distribution = Quantizer?.Invoke(this)?.ToArray() ?? distribution ; } catch( System.Exception e ) { System.Diagnostics.Trace.TraceWarning(e.Stringy()) ; } return distribution ; } } Quant[] distribution ;
		Quant[] Source => _Source as Quant[] ?? ( _Source = _Source?.ToArray() ) as Quant[] ; Quant[] Basis => _Basis as Quant[] ?? ( _Basis = _Basis?.ToArray() ) as Quant[] ; IEnumerable<Quant> _Source , _Basis ;
		Quantile( Axe context , IEnumerable<Quant> source , Func<Quantile,IEnumerable<Quant>> quantizer = null ) { Axe = context ; _Source = source ; Quantizer = quantizer ; }
		public Quantile( Axe context , Func<Quantile,IEnumerable<Quant>> quantizer = null , IEnumerable<Quant> distribution = null , Axe on = null ) : this(context,distribution??context?.Distribution,quantizer) => _Source = (_Basis=_Source)?.Select(d=>context[d,on]) ;
		Quantile( Quantile context , Func<Quantile,IEnumerable<Quant>> quantizer ) : this(null,context.Distribution,quantizer) => Context = context ;
		public Quant this[ int key ] => Distribution.At(key) ;
		public Quant this[ Quant level ] { get { var b = Basis ; if( level<b?[0] ) return this[0] ; for( var i=0 ; i<b?.Length-1 ; ++i ) if( b[i]<=level && b[i+1]>=level ) return this[i] ; return this[Count-1] ; } }
		public Quant Tres( Quant level ) { var s = Source ; var b = Basis ; if( s==null || b==null || level>s?[0] ) return b?[0]??0 ; for( var i=0 ; i<s.Length-1 ; ++i ) if( s[i]>=level && s[i+1]<=level ) return b[i] ; return b[b.Length-1] ; }
		public int Count => Distribution?.Length ?? 0 ;
		public Duo Centre { get { Quant ex = 0 , cd = 0 ; var j = 0 ; var s = Source ; var b = Basis ; for( var i = 0 ; i<Count-1 ; ++i ) if( (cd=Math.Abs((s[i]-s[i+1])/(b[i]-b[i+1])))>ex ) { ex = cd ; j = i ; } return new Duo{X=this[j],Y=b.At(j)} ; } }
		public Quantile this[ Func<Quantile,IEnumerable<Quant>> quantizer , IEnumerable<Quant> distribution , Axe on = null , bool free = false ] => Axe.Get(a=>new Quantile(a,quantizer,distribution??(free?a.Refine():a.Distribution),on)) ?? new Quantile(Context[distribution,on,free],quantizer) ;
		public Quantile this[ IEnumerable<Quant> distribution , Axe on = null , bool free = false ] => this[free?null:Quantizer,distribution,on,free] ;
		public Quantile this[ Axe on , bool free = false ] => this[null,on,free] ;
		public IEnumerator<Quant> GetEnumerator() => (Distribution?.AsEnumerable()??Enumerable.Empty<Quant>()).GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		#region Operations
		public static Quantile operator>>( Quantile source , int level ) => level<=0 ? source : new Quantile(source,q=>q.Get(d=>(d.Count-1).Steps(1).Select(i=>d[i]-d[i-1])))>>level-1 ;
		public static Quantile operator-( Quantile source ) => new Quantile(source,q=>q.Select(v=>-v)) ;
		public static Quantile operator*( Quantile source , Quant value ) => new Quantile(source,q=>q.Select(v=>v*value)) ;
		public static Quantile operator/( Quantile source , Quant value ) => new Quantile(source,q=>q.Select(v=>v/value)) ;
		public static Quantile operator+( Quantile source , Quant value ) => new Quantile(source,q=>q.Select(v=>v+value)) ;
		public static Quantile operator-( Quantile source , Quant value ) => new Quantile(source,q=>q.Select(v=>v-value)) ;
		public static Quantile operator*( Quant value , Quantile source ) => new Quantile(source,q=>q.Select(v=>value*v)) ;
		public static Quantile operator/( Quant value , Quantile source ) => new Quantile(source,q=>q.Select(v=>value/v)) ;
		public static Quantile operator+( Quant value , Quantile source ) => new Quantile(source,q=>q.Select(v=>value+v)) ;
		public static Quantile operator-( Quant value , Quantile source ) => new Quantile(source,q=>q.Select(v=>value-v)) ;
		#endregion
		public struct Duo { public Quant X , Y ; }
	}
	public struct Lap : Aid.Gettable<int,int>
	{
		readonly int[] Content ;
		public Lap( Axe context , Quant dif )
		{
			var content = new List<int>() ; var dir = Math.Sign(dif) ; dif = Math.Abs(dif) ; if( context!=null )
			{
				if( dir>0 ) for( int c=context.Count , i=0 ; dif>0 && i<c ; content.Add(i-content.Count) ) for( var v = context.Resolve(content.Count) ; i<c && (context.Resolve(i)-v).use(Math.Abs)<dif ; ++i ) ;
				else for( int c=context.Count , i=0 , j=0 ; dif>0 && i<c ; ++j ) for( var v = context.Resolve(j) ; i<c && (context.Resolve(i)-v).use(Math.Abs)<dif ; content.Add(j-++i) ) ;
			}
			Content = content.ToArray() ;
		}
		public int this[ int key ] => Content.At(key) ;
	}
	public partial class Path
	{
		public class Axe : Act.Axe
		{
			internal Path Context ;
			public virtual Axis Axis { get => axis ; set { base.Spec = ( axis = value ).Stringy() ; Resolver = at=>Context?[at]?[Axis] ; } } Axis axis ;
			public override string Spec { get => base.Spec ; set { if( value!=null ) Axis = (Axis)value.Axis() ; base.Spec = value ; } }
			public Axe( Path context ) => Context = context ;
			public override int Count => DefaultCount ;
			protected override int DefaultCount => Context?.Count ?? 0 ;
			protected override Aspectable DefaultAspect => Context?.Spectrum ;
		}
		public static Axable operator&( Path path , string name ) => path.Spectrum[name] ;
		public static Axable operator&( Path path , Axis name ) => path.Spectrum[name] ;
	}
	public static class AxeOperations
	{
		public static Axe Shift( this int dis , Axe x , Axe y , int? dif = null ) => (dif??dis).Get(d=>d.Quo(x,y)).Get(a=>a.Skip(dis)/a) ;
		public static Axe Drift( this int dis , Axe x , Axe y , int? dif = null ) => Shift(dis,y,x,dif) ;
		public static Axe Quo( this int dif , Axe x , Axe y ) => (x%dif)/(y%dif) ;
		public static Axe Quo( this Lap dif , Axe x , Axe y ) => (x/dif)/(y/dif) ;
		public static IEnumerable<Quant> Refine( this IEnumerable<Quant?> source ) => source?.OfType<Quant>().Distinct().OrderBy(q=>q) ;
	}
}
