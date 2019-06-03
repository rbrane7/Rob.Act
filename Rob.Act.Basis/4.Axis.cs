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
		public Axe( Axe source ) { spec = source?.spec ; aspectlet = source?.aspectlet ; aspect = source?.aspect ; resolvelet = source?.resolvelet ; resolver = source?.resolver ; selectlet = source?.selectlet ; selector = source?.selector ; countlet = source?.countlet ; counter = source?.counter ; distribulet = source?.distribulet ; distributor = source?.distributor ; Quantizer = source?.Quantizer ; multi = source?.multi??false ; }
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
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
		public Func<Axe,IEnumerable<Quant>> Distributor { get => distributor ?? ( distributor = distribulet.Compile<Func<Axe,IEnumerable<Quant>>>() ) ; set { if( distributor==value ) return ; distributor = value ; propertyChanged.On(this,"Distributor,Quantile") ; } } Func<Axe,IEnumerable<Quant>> distributor ;
		public IEnumerable<Quant> Distribution => Distributor?.Invoke(this) ?? DefaultDistribution ?? Enumerable.Empty<Quant>() ;
		protected virtual IEnumerable<Quant> DefaultDistribution { get { var pts = this.Where(q=>q!=null).Cast<Quant>().ToArray() ; if( pts.Length<=0 ) return null ; var min = pts.Min() ; var max = pts.Max() ; var stp = (max-min)/10 ; return min.Recur(q=>q+stp,q=>q<=max) ; } }
		public string Distribulet { get => distribulet ; set { if( value==distribulet ) return ; distribulet = value ; Distributor = null ; propertyChanged.On(this,"Distribulet") ; } } string distribulet ;
		public IEnumerator<Quant?> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public Quantile Quantile { get ; private set ; }
		public Func<Quantile,IEnumerable<Quant>> Quantizer { get => Quantile.Quantizer ; set => propertyChanged.On(this,"Quantizer,Quantile",Quantile=new Quantile(this,value)) ; }
		public string Quantlet { get => quantlet ; set => propertyChanged.On(this,"Quantlet",Quantizer=(quantlet=value).Compile<Func<Quantile,IEnumerable<Quant>>>()) ; } string quantlet ;
		#region ICollection
		object ICollection.SyncRoot => throw new NotImplementedException() ;
		bool ICollection.IsSynchronized => throw new NotImplementedException() ;
		void ICollection.CopyTo( Array array , int index ) => throw new NotImplementedException() ;
		#endregion
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
		Quant? Diff( int at , int dif ) => Resolve(at+dif)-Resolve(at) ;
		Quant? Quot( Axe upon , int at , int dif ) => Diff(at,dif)/upon.Diff(at,dif).Nil() ;
		Quant? Shift( Axe upon , int at , int dis ) => Quot(upon,at+dis,dis)/Quot(upon,at,dis).Nil() ;
		public Axe Rift( Axe upon , uint quo = 9 ) => upon==null ? No : new Axe( i=>Shift(upon,i,((Count-i)>>1)-1) , a=>(int)(Count*quo/(1D+quo)) ) ;
		#endregion
		#region De/Serialization
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Axe( string text ) => text.Separate(Serialization.Separator,braces:null).Get(t=>new Axe{Spec=t.At(0),Multi=t.At(1)==Serialization.Multier,Resolvelet=t.At(2),Countlet=t.At(3),Selectlet=t.At(4),Distribulet=t.At(5),Quantlet=t.At(6)}) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Axe aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.spec,a.multi?Serialization.Multier:string.Empty,a.resolvelet,a.countlet,a.selectlet,a.distribulet,a.quantlet)) ;
		static class Serialization { public const string Separator = " \x1 Axlet \x2 " ; public const string Multier = "*" ; }
		#endregion
	}
	public class Quantile : Aid.Gettable<int,Quant> , Aid.Countable<Quant> , Aid.Gettable<IEnumerable<Quant>,Quantile>
	{
		readonly Quantile Context ;
		public Axe Ax => Axe ?? Context?.Ax ; readonly Axe Axe ;
		internal Func<Quantile,IEnumerable<Quant>> Quantizer ;
		Quant[] Distribution { get { if( distribution==null ) { distribution = Source?.ToArray() ; distribution = Quantizer?.Invoke(this)?.ToArray() ?? distribution ; } return distribution ; } } Quant[] distribution ; readonly IEnumerable<Quant> Source ;
		Quantile( Axe context , IEnumerable<Quant> source , Func<Quantile,IEnumerable<Quant>> quantizer = null ) { Axe = context ; Source = source ; Quantizer = quantizer ; }
		public Quantile( Axe context , Func<Quantile,IEnumerable<Quant>> quantizer = null , IEnumerable<Quant> distribution = null ) : this(context,(distribution??context?.Distribution)?.Select(d=>context[d]),quantizer) {}
		Quantile( Quantile context , Func<Quantile,IEnumerable<Quant>> quantizer ) : this(null,context.Distribution,quantizer) => Context = context ;
		public double this[ int key ] => Distribution.At(key) ;
		public int Count => Distribution?.Length ?? 0 ;
		public Quantile this[ IEnumerable<Quant> distribution ] => Axe.Get(a=>new Quantile(a,Quantizer,distribution)) ?? new Quantile(Context[distribution],Quantizer) ;
		public IEnumerator<Quant> GetEnumerator() => (Distribution?.AsEnumerable()??Enumerable.Empty<Quant>()).GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		#region Blind
		public object SyncRoot => throw new NotSupportedException() ;
		public bool IsSynchronized => throw new NotSupportedException() ;
		public void CopyTo( Array array, int index ) => throw new NotSupportedException() ;
		#endregion
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
	}
	public partial class Path
	{
		public class Axe : Act.Axe
		{
			internal Path Context ;
			public virtual Axis Axis { get => axis ; set { base.Spec = ( axis = value ).Stringy() ; Resolver = at=>Context.At(at)?[Axis] ; } } Axis axis ;
			public override string Spec { get => base.Spec ; set { if( value!=null ) Axis = value.Axis() ; base.Spec = value ; } }
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
	}
}
