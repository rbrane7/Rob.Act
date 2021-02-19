using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Aid.Extension;
using Aid.Collections;

namespace Rob.Act
{
	using System.Collections ;
	using System.ComponentModel ;
	using Quant = Double ;
	using Region = IEnumerable<int> ;
	using Aid.Math ;
	using Aid;

	public interface Axable : Aid.Gettable<int,Quant?> , Aid.Gettable<double,Quant?> , Aid.Countable<Quant?> { string Spec {get;} }
	public class Axe : Axable , INotifyPropertyChanged
	{
		public const string Extern = LambdaContext.Act.Extern ;
		public readonly static Support No = new Support(null){resolver=i=>null as Quant?} , One = new Support(null){resolver=i=>1} ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Axe() : this(null,null) {} // Default constructor must be present to enable DataGrid implicit Add .
		public Axe( Func<int,Quant?> resolver = null , Axe source = null ) { this.resolver = resolver ; aspect = source?.Aspect ; aspects = source?.Aspects??default ; rex = source?.rex??default ; selectlet = source?.selectlet ; selector = source?.selector ; multi = source?.multi??default ; }
		public Axe( Axe source , IEnumerable<Aspectable> primary = null , IEnumerable<Aspectable> secondary = null )
		{
			var dax = source?.Deref(primary)??source?.Deref(secondary)??source ;
			spec = dax?.spec ; aspect = source?.aspect ; resolvelet = dax?.resolvelet ; resolver = dax?.resolver ; multi = dax?.multi??default ; bond = source?.bond.Null(v=>v.No())??dax?.bond ;
			var ses = selectlet.No() ? dax : source ; rex = ses?.rex??default ; selectlet = ses?.selectlet ; selector = ses?.selector ;
			if( source?.distribulet.No()!=false && source?.quantlet.No()!=false ) source = dax ; distribulet = source?.distribulet ; distributor = source?.distributor ; quantlet = source?.quantlet ; Quantizer = source?.quantile?.Quantizer ;
		}
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public string Binder { get => bond ; set { if( value==bond ) return ; bond = value ; propertyChanged.On(this,"Binder") ; } } string bond ;
		public Aspectable Source { set { if( Selector==null && DefaultAspect==null ) Aspect = value ; else Resource.Source = value ; } }
		public Aspectable[] Sources { set { if( Selector==null && DefaultAspects==null ) Aspects = new Aspectables(value) ; else Resource.Sources = value ; } }
		public bool Multi { get => multi ; set { if( value==multi ) return ; multi = value ; Aspect = null ; propertyChanged.On(this,"Multi,Aspects") ; } } bool multi ;
		public bool Regular => !Multi || Selector!=null ;
		public bool Asrex { get => rex ; set { if( value==rex ) return ; rex = value ; Selectlet = selectlet ; propertyChanged.On(this,"Asrex,Aspects") ; } } bool rex ;
		Axe Deref( IEnumerable<Aspectable> aspects ) => IsRef ? Spec.RightFrom(Extern,all:true).Get(s=>(Spec.LeftFromLast(Extern)is string asp?aspects?.Where(a=>asp==a.Spec):aspects)?.SelectMany(a=>a).One(x=>x.Spec==s&&!x.IsRef)) : null ;
		public Axe DeRef => Deref(Act.Aspect.Set)??this ; public bool IsRef => (resolver==null||resolver==No.resolver) && resolvelet.No() ;
		protected virtual Aspectable DefaultAspect => Multi ? null : Selection?.SingleOrNo() ;
		protected virtual Aspectable[] DefaultAspects => Multi ? Selection?.ToArray() : null ;
		/// <summary> Cant' be null . </summary>
		protected internal Resourcable Resource => Aspect ?? Aspects as Resourcable ;
		public IEnumerable<Aspectable> Resources => (Regular?Multi?aspects:aspect?.Times():null) ?? Enumerable.Empty<Aspectable>() ;
		protected virtual Aspectables Aspects { get => aspects.No ? aspects = new Aspectables(DefaultAspects) : aspects ; set { aspects = value ; Resolver = null ; propertyChanged.On(this,"Aspects,Aspect") ; } } Aspectables aspects ;
		public virtual Aspectable Aspect { get => aspect ??= DefaultAspect ; set { if( aspect==value ) return ; aspect = value ; Resolver = null ; propertyChanged.On(this,"Aspect") ; } } protected Aspectable aspect ;
		public virtual int Count => Counter?.Invoke() ?? Resource.Points.Count ;
		protected virtual Func<int> Counter { get => counter ?? Resolver.Get(_=>counter) ; set => counter = value ; } Func<int> counter ;
		internal Aspectable Own ;
		IEnumerable<Aspectable> Selection => Selector?.Invoke(Aspectables.The.All?.Invoke()) ;
		Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> Selector { get => selector ; set { if( selector==value ) return ; selector = value ; Aspect = null ; propertyChanged.On(this,"Selector") ; } } Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> selector ;
		public string Aspectlet { get => selectlet ?? Aspect?.Spec ; set { if( value==Aspectlet ) return ; Selectlet = value ; propertyChanged.On(this,"Aspectlet") ; } } string selectlet ;
		string Selectlet { set { selectlet = value.Null(s=>s.No()) ; var aspectlet = Asrex ? value.Null(s=>s.No()).Get(v=>new Regex(v)) : null ; Selector = aspectlet==null ? selectlet.Compile<Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>>>() : s=>s.Where(a=>aspectlet.Match(a.Spec).Success) ; } }
		public Quant? this[ int at ] => Resolve(at) ;
		/// <summary> Calculates value of this axe at exact <paramref name="at"/> positin . Calculation uses linear interpolation for intermediary positions . </summary>
		/// <param name="at"> Exact position where to calculate the axe value . </param>
		/// <returns> Interpolated value of axe . </returns>
		public Quant? this[ double at ] { get { var f = Math.Floor(at) ; var c = Math.Ceiling(at) ; return c==f ? Resolve((int)at) : (Resolve((int)f)*(c-at)+Resolve((int)c)*(at-f)) ; } }
		public int? AtOf( Quant? value ) => value.Get(v=>this.Best(q=>q is Quant u?Math.Abs(u-v):Quant.MaxValue)?.at) ;
		public Quantile.Measure Measure( Axe on ) => new Quantile.Measure(this,on) ;
		public Quant this[ Quant at , Axe ax ] { get { if( ax==null ) return this.Count(q=>q>=at) ; Quant rez = 0 ; for( int i=0 , count=Count ; i<count ; ++i ) if( Resolve(i)>=at ) rez += ax[i+1]-ax[i]??0 ; return rez ; } }
		//Aid.Collections.BinSetNative<Quant> Cash => cash ??( cash = new Aid.Collections.BinArray<(Quant,int)>.Map<Quant>(Count.Steps().Select(i=>(this[i],i))) ) ; Aid.Collections.BinSetNative<Quant> cash ;
		/// <summary> Value of <see cref="Axe"/> at position <paramref name="at"/> . </summary>
		/// <param name="at"> Position to evaluate at . </param>
		/// <returns> Value at position . </returns>
		/// <remarks> This function is not to be overriden as it would violate constructive chaining of <see cref="Axe"/> . </remarks>
		internal Quant? Resolve( int at ) => Resolver?.Invoke(at) ;
		public Axe Solver => Resolver.Get(_=>coaxe) ;
		Axe Coaxe { get { try { return coaxe = Multi?Aspects.Get(a=>Resolvelet.Compile<Func<Contexts,Axe>>(use:"Rob.Act").Of(new Contexts{Base=a,This=Own, The=this})):Aspect.Get(a=>Resolvelet.Compile<Func<Context,Axe>>(use:"Rob.Act").Of(new Context{Base=a,This=Own, The=this})) ; } catch( LambdaContext.Exception ) { throw ; } catch( System.Exception e ) { throw new InvalidOperationException($"Problem resolving {Spec} !",e) ; } finally { coaxe.Set(c=>Counter=c.Counter) ; } } } Axe coaxe ;
		/// <summary> Never null . If nul than always throws . </summary>
		protected Func<int,Quant?> Resolver { private get => resolver ??= (Coaxe??No).Resolver ; set { if( resolver==value ) return ; resolver = value ; propertyChanged.On(this,"Resolver") ; } } Func<int,Quant?> resolver ;
		public string Resolvelet { get => resolvelet ; set { if( value==resolvelet ) return ; resolvelet = value ; Resolver = null ; propertyChanged.On(this,"Resolvelet") ; } } string resolvelet ;
		public IEnumerator<Quant?> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		#region Quantile
		Func<Axe,IEnumerable<Quant>> Distributor { get => distributor ??( distributor = distribulet.Compile<Func<Axe,IEnumerable<Quant>>>() ?? (a=>null) ) ; set { if( distributor==value ) return ; distributor = value ; quantile.Set(q=> Quantile = new Quantile(this,q.Quantizer) ) ; propertyChanged.On(this,"Distributor") ; } } Func<Axe,IEnumerable<Quant>> distributor ;
		public IEnumerable<Quant> Distribution => Distributor?.Invoke(this) ?? DefaultDistribution ;
		protected virtual IEnumerable<Quant> DefaultDistribution => this.Refine().ToArray().Null(d=>d.Length<=0) ;
		public string Distribulet { get => distribulet ; set { if( value==distribulet ) return ; distribulet = value ; Distributor = null ; propertyChanged.On(this,"Distribulet") ; } } string distribulet ;
		public Quantile Quantile { get => quantile ??( quantile = new Quantile(this) ) ; private set => propertyChanged.On(this,"Quantile", quantile = value ) ; } Quantile quantile ;
		public Func<Quantile,IEnumerable<Quant>> Quantizer { get => Quantile.Quantizer ; set { if( value!=quantile?.Quantizer ) propertyChanged.On(this,"Quantizer", Quantile = new Quantile(this,value) ) ; } }
		public string Quantlet { get => quantlet ; set => propertyChanged.On(this,"Quantlet",Quantizer=(quantlet=value).Compile<Func<Quantile,IEnumerable<Quant>>>()) ; } string quantlet ;
		#endregion
		#region Operations
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Support this[ Region fragment ] => On(fragment) ;
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Axe this[ Lap lap ] => By(lap) ;
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Axe this[ Axe axe ] => (axe?.Solver as Lap.Axe??axe) is Lap.Axe lap ? By(lap.Arg) : this ;
		/// <summary> Function on axe . </summary>
		public Axe this[ Func<Quant,Quant> y ] => this.Fun(y) ;
		public static Axe operator++( Axe x ) => x==null ? No : new Axe( i=>x.Resolve(i+1) , x ) ;
		public static Axe operator--( Axe x ) => x==null ? No : new Axe( i=>x.Resolve(i-1) , x ) ;
		public static Quant? operator+( Axe x ) => x?.Sum() ;
		public static Axe operator-( Axe x ) => x==null ? No : new Axe( i=>-x.Resolve(i) , x ) ;
		public static Axe operator^( Axe x , Axe y ) => x==null ? No : x.Drift(y) ;
		public static Axe operator^( Axe x , Quant y ) => x==null ? No : new Axe( i => x.Resolve(i) is Quant a ? Math.Pow(a,y) : null as Quant? , x ) ;
		public static Axe operator^( Quant x , Axe y ) => y==null ? No : new Axe( i => y.Resolve(i) is Quant a ? Math.Pow(x,a) : null as Quant? , y ) ;
		public static Axe operator^( Axe x , bool _ ) => x==null ? No : new Axe( i => x.Resolve(i) is Quant a ? a>0?Math.Log(a):null as Quant? : null as Quant? , x ) ;
		public static Axe operator^( bool _ , Axe y ) => y==null ? No : new Axe( i => y.Resolve(i) is Quant a ? Math.Exp(a) : null as Quant? , y ) ;
		//public static Axe operator^( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i => x.Resolve(i) is Quant a && y.Resolve(i) is Quant b ? Math.Pow(a,b) : null as Quant? , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator>>( Axe x , int lev ) => x==null ? No : lev<0 ? x<<-lev : lev==0 ? x : new Axe( i=>x.Resolve(i)-x.Resolve(i-1) , x )>>lev-1 ;
		public static Axe operator<<( Axe x , int lev ) => x==null ? No : lev<0 ? x>>-lev : lev==0 ? x : new Axe( i=>i.Steps().Sum(x.Resolve) , x )<<lev-1 ;
		public static Axe operator%( Axe x , bool _ ) => x==null ? No : new Axe( i=>x.Dif(i) , x ) ;
		public static Axe operator%( Axe x , Mark lap ) => x==null ? No : new Axe( i=>x.Dif(i,lap) , x ) ;
		public static Axe operator%( Axe x , int dif ) => x==null ? No : new Axe( i=>x.Dif(i,dif) , x ) ;
		public static Axe operator%( Axe x , Quant dif ) => new Lap.Axe(x,dif) ;
		public static Axe operator%( Axe x , float mod ) => x==null ? No : new Axe( i=>x.Resolve(i)%mod , x ) ;
		public static Axe operator%( Axe x , Region mod ) => x==null ? No : x.Floe(mod) ;
		public static Axe operator%( Axe x , Support y ) => x==null ? No : x.Floe(y.Fragment) ;
		public static Axe operator%( Axe x , Axe y ) => x==null ? No : x.Rift(y) ;
		public static Axe operator*( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)*y.Resolve(i) , x ) ;
		public static Axe operator*( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)*y , x ) ;
		public static Axe operator*( Quant x , Axe y ) => y==null ? No : new Axe( i=>x*y.Resolve(i) , y ) ;
		public static Axe operator/( Axe x , Axe y ) => x==null||y==null ? No : (y as Lap.Axe??y.Solver as Lap.Axe).Get(l=>x/l.Arg) ?? new Axe( i=>x.Resolve(i)/y.Resolve(i).Nil() , x ) ;
		public static Axe operator/( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)/y.nil() , x ) ;
		public static Axe operator/( Quant x , Axe y ) => y==null ? No : new Axe( i=>x/y.Resolve(i).Nil() , y ) ;
		public static Axe operator/( bool _ , Axe y ) => y==null ? No : new Axe( i=>1/y.Resolve(i).Nil() , y ) ;
		public static Axe operator/( Axe x , Lap dif ) => x==null ? No : new Axe( i => dif[i] is double d ? x.Dif(i,d) : null , x ) ;
		public static Axe operator+( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)+y.Resolve(i) , x ) ;
		public static Axe operator+( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)+y , x ) ;
		public static Axe operator+( Quant x , Axe y ) => y==null ? No : new Axe( i=>x+y.Resolve(i) , y ) ;
		public static Axe operator-( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)-y.Resolve(i) , x ) ;
		public static Axe operator-( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)-y , x ) ;
		public static Axe operator-( Quant x , Axe y ) => y==null ? No : new Axe( i=>x-y.Resolve(i) , y ) ;
		public static Region operator >( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]>val) ;
		public static Region operator<( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]<val) ;
		public static Region operator<( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i+1],v,false))) ;
		public static Region operator>( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i-1],v,true))) ;
		public static Region operator<=( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i+1],v))) ;
		public static Region operator>=( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i-1],v))) ;
		public static Region operator>=( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]>=val) ;
		public static Region operator<=( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]<=val) ;
		bool Affines( int at , Quant val , bool smooth = true ) => Affines(this[at],this[at-1],val,smooth?default(bool?):true)||Affines(this[at],this[at+1],val,smooth?default(bool?):false) ;
		static bool Affines( Quant? at , Quant? to , Quant val , bool? smooth = null ) => at==val?smooth??true:to==val||at==null||to==null?smooth==null?false:smooth.Value?at>val:at<val:at>val==to<val ;
		public static Region operator==( Axe x , Quant val ) => x?.Count.Steps().Where(i=>x.Affines(i,val)) ;
		public static Region operator!=( Axe x , Quant val ) => x?.Count.Steps().Where(i=>x.Affines(i,val,false)) ;
		public static Region operator>( Quant? val , Axe x ) => x<val ;
		public static Region operator<( Quant? val , Axe x ) => x>val ;
		public static Region operator>=( Quant? val , Axe x ) => x<=val ;
		public static Region operator<=( Quant? val , Axe x ) => x>=val ;
		public static Region operator==( Quant val , Axe x ) => x==val ;
		public static Region operator!=( Quant val , Axe x ) => x!=val ;
		public static Region operator>( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]>y[i]) ;
		public static Region operator<( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]<y[i]) ;
		public static Region operator>=( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]>=y[i]) ;
		public static Region operator<=( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]<=y[i]) ;
		public static Axe operator&( Axe x , Axe y ) => x.Centre(y) ;
		public static Axe operator&( Axe x , Quant y ) => x.Nil(v=>v>y) ;
		public static Axe operator&( Quant x , Axe y ) => y.Nil(v=>v<x) ;
		public static Axe operator&( Axe x , Func<Quant,bool> y ) => x.Nil(v=>!y(v)) ;
		public static Axe operator&( Axe x , Func<Quant,Quant> y ) => x.Fun(y) ;
		public static Axe operator|( Axe x , Axe y ) => x==null ? y : y==null ? x : new Axe( i => x[i]*y[i]??x[i]??y[i] , x ) ;
		public static implicit operator Axe( Func<int,Quant?> resolver ) => resolver.Get(r=>new Axe(r)) ;
		public static implicit operator Axe( Quant q ) => new Axe( i=>q ) ;
		public static implicit operator Axe( int q ) => new Axe( i=>q ) ;
		public static implicit operator Quant?( Axe a ) => +a ;
		public Axe Round => new Axe( i=>Resolve(i).use(Math.Round) , this ) ;
		public Axe Floor => new Axe( i=>Resolve(i).use(Math.Floor) , this ) ;
		public Axe Skip( int count ) => new Axe( i=>Resolve(count+i) , this ) ;
		public Axe Wait( int count ) => new Axe( i=>Resolve(i<count?0:i-count) , this ) ;
		public Axe Take( int count ) => new Axe( i=>i<count?Resolve(i):null , this ) ;
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Support On( Region fragment ) => fragment.Get(f=>new HashSet<int>(f)).Get(f=>new Support(f,i=>f.Contains(i)?Resolve(i):null,this)) ?? No ;
		/// <summary> Axe of values relative to beginning of continual subset the point belongs to . </summary>
		/// <param name="fragment"> Fragment of continual subsets . </param>
		/// <returns> Axe which values are ofsetted to preceding continual predesessing value with respect to <paramref name="fragment"/> . </returns>
		public Support Floe( Region fragment ) => fragment?.ToArray().Get(f=>new Support( f , i => Array.IndexOf(f,i) is int at && at>=0 ? Resolve(i)-Resolve(f[at.LastContinualPredecessorIn(f)]) : null , this )) ?? No ;
		/// <summary> Refines this axe to <paramref name="lap"/> distance axe . </summary>
		/// <param name="lap"> Lap representing index distance between point of this axe . </param>
		/// <returns> Axe of exact <paramref name="lap"/> distanced points of this axe values . </returns>
		public Axe By( Lap lap ) => new Lap.Axe(this,lap) ;
		/// <summary> Creates axe of drift of this axe on given <paramref name="upon"/> . </summary>
		/// <param name="upon"> Axe to calculate drift on . </param>
		/// <param name="at"> Position where to get drift at . </param>
		/// <param name="dis"> Distance to calculate drift for . </param>
		/// <returns> Axe of drift of this axe <paramref name="at"/> poositon for <paramref name="dis"/>tance <paramref name="upon"/> axis . </returns>
		public Axe Drift( Axe upon , Quant quo = 0 ) => upon==null ? No : new Axe( i=>(quo*i).Get(at=>Drift(upon,(int)at,(int)((i-at)/2))) , this ) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , Mark lap = Mark.Lap ) => Resolve(at)-(Resolve(Own?.Raw?[lap,at-1]??0)??0) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , int dif ) => dif==0 ? Dif(at) : (Resolve(at+dif)-Resolve(at))*Math.Sign(dif) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Ave( int at , int dif ) => dif.Steps(at).Aggregate((Quant?)0D,(a,i)=>Resolve(i)) ;
		/// <summary>
		/// Calculates value difference of this axe between value <paramref name="at"/> positin and position differing exactly by real <paramref name="dif"/> . 
		/// Calculation uses linear interpolation for intermediary positions . 
		/// </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Exact real index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , double dif ) { var a = at+dif ; var f = Math.Floor(a) ; var c = Math.Ceiling(a) ; return c==f ? Dif(at,(int)dif) : (Resolve((int)f)*(c-a)+Resolve((int)c)*(a-f)-Resolve(at))*Math.Sign(dif) ; }
		Quant? Quo( Axe upon , int at , int dif ) => Dif(at,dif)/upon.Dif(at,dif).Nil() ;
		/// <summary> Calculates drift of this axe on given <paramref name="upon"/> . </summary>
		/// <param name="upon"> Axe to calculate drift on . </param>
		/// <param name="at"> Position where to get drift at . </param>
		/// <param name="dis"> Distance to calculate drift for . </param>
		/// <returns> Value of drift <paramref name="at"/> poositon for <paramref name="dis"/>tance <paramref name="upon"/> axis . </returns>
		Quant? Drift( Axe upon , int at , int dis ) => Quo(upon,at,dis)/Quo(upon,at+dis,dis).Nil() ;
		public Axe Rift( Axe upon , uint quo = 9 ) => upon==null ? No : new Axe( i=>Drift(upon,i,((Count-i)>>1)-1) , this ) ;
		/// <summary> <see cref="Rob.Act.Lap"/> for given parameter <paramref name="dif"/> and this Axe . </summary>
		public Lap Lap( Quant dif ) => new Lap(this,dif) ;
		/// <summary> <see cref="Rob.Act.Lap"/> for given parameter <paramref name="dif"/> and this Axe . </summary>
		public Axe By( Quant dif ) => By(Lap(dif)) ;
		public Axe Nil( Predicate<Quant> nil ) => new Axe( i=>Resolve(i).Nil(nil) , this ) ;
		public Axe Fun( Func<Quant,Quant> fun ) => new Axe( i=>Resolve(i).use(fun) , this ) ;
		public Axe PacePower( Quant grade = 0 , Quant? resi = null , Quant flow = 0 , Quant grane = 0 ) => new Axe( i=>Resolve(i).PacePower(grade,(Aspect as Aspect)?.Resistance(resi)??0,flow,grane,Aspect?.Raw?.Profile?.Mass) , this ) ;
		public Axe PowerPace( Quant grade = 0 , Quant? resi = null , Quant flow = 0 , Quant grane = 0 ) => new Axe( i=>Resolve(i).PowerPace(grade,resi??Aspect?.Raw?.Resister??0,flow,Aspect?.Raw?.Profile?.Mass) , this ) ;
		public Axe Centre( Axe mesure ) => this*mesure/+mesure ;
		#endregion
		#region De/Serialization
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Axe( string text ) => text.Separate( Serialization.Separator,braces:null).Get(t=>new Axe{Spec=t.At(0),Multi=t.At(1)==Serialization.Multier,Resolvelet=t.At(2),Selectlet=t.At(4),Distribulet=t.At(5),Quantlet=t.At(6),Binder=t.At(7),Asrex=t.At(8)==Serialization.Rex}) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Axe aspect ) => aspect.Get(a=>string.Join( Serialization.Separator,a.spec,a.multi? Serialization.Multier:string.Empty,a.resolvelet,null,a.selectlet,a.distribulet,a.quantlet,a.Binder,a.rex? Serialization.Rex:string.Empty)) ;
		static class Serialization { public const string Separator = " \x1 Axlet \x2 " ; public const string Multier = "*" , Rex = "rex"; }
		#endregion
		public struct Context : Contextable
		{
			public Aspectable Base , This ; public Axe The ;
			[LambdaContext.Dominant] public Axe this[ string key ] => This?[key] is Axe a && a!=The ? a : Base?[key] ;
			public Support this[ Region fragment ] => One[fragment] ;
			public Support this[ Mark mark , Region fragment ] => fragment is Region f ? new Marker(mark,f,This) : No ;
			public Path Raw => Base?.Raw ;
			public Aspect.Traits Trait => This?.Trait ;
			public Axe Perf( Axe lap ) => lap is Lap.Axe a ? Perf(a.Arg) : No ;
			public Axe Perf( Lap lap ) => (Base as Path.Aspect)?.perf(lap) ?? No ;
			public Axe Perf( int dif = 0 ) => (Base as Path.Aspect)?.perf(dif) ?? No ;
		}
		public struct Contexts : Contextables
		{
			public Aspectables Base ; public Aspectable This ; public Axe The ;
			[LambdaContext.Dominant] public Axe this[ string key ] => This?[key] is Axe a && a!=The ? a : null ;
			public Aspectable this[ int key ] => Base[key] ;
			public Path Raw( int at = 0 ) => Base[at].Raw ;
			public Support this[ Region fragment ] => One[fragment] ;
			public Support this[ Mark mark , Region fragment ] => fragment is Region f ? new Marker(mark,f,This) : No ;
		}
		public class Support : Support<Region> { public Region Fragment => Arg ; internal Support( Region fragment , Func<int,Quant?> resolver = null , Axe source = null ) : base(fragment,resolver,source) {} }
		public class Support<Param> : Axe { public readonly Param Arg ; internal Support( Param arg , Func<int,Quant?> resolver = null , Axe source = null ) : base(resolver,source) => Arg = arg ; }
		/// <summary>
		/// Solver of automatic <see cref="Mark"/> placement . 
		/// </summary>
		public class Marker : Support
		{
			Mark Mark ; HashSet<int> Frag => Fragment as HashSet<int> ; Point Ori( int at ) => Aspect?.Raw?[at] ;
			internal Marker( Mark mark , Region fragment , Aspectable aspect = null ) : base(new HashSet<int>(fragment)) { Mark = mark ; Aspect = aspect ; Resolver = Resolve ; }
			new Quant? Resolve( int at ) => Could?.Invoke(Mark,at)??Can(Mark,at) ? Put(at) : Ori(at)?[Mark] ;
			Quant? Put( int at ) { var p = Ori(at) ; if( p!=null ) p.Mark |= Mark ; return p?[Mark] ; }
			bool Can( Mark mark , int at ) => Can(at) && !Can(at-1) ;
			bool Can( int at ) => Frag.Contains(at)!=Frag.Contains(at-1) ;
			/// <summary>
			/// Defines strategy to use for definition of particular <see cref="Mark"/> placement . 
			/// </summary>
			public static Func<Mark,int,bool> Could ;
		} 
	}
	public class Quantile : Aid.Gettable<int,Quant> , Aid.Countable<Quant>
	{
		static readonly Quant[] Empty = new Quant[0] ; static Quant Zero = 0.0017 ;
		public Axe Ax => Axe ?? Context?.Ax ; readonly Quantile Context ; readonly Axe Axe ; internal Func<Quantile,IEnumerable<Quant>> Quantizer ;
		Quant[] Distribution { get { if( distribution==null ) try { distribution = Source ?? Empty ; distribution = Quantizer?.Invoke(this)?.ToArray() ?? distribution ; } catch( System.Exception e ) { System.Diagnostics.Trace.TraceWarning(e.Stringy()) ; } return distribution ; } } Quant[] distribution ;
		Quant[] Source => _Source as Quant[] ??( _Source = _Source?.ToArray() ) as Quant[] ; Quant[] Basis => _Basis as Quant[] ??( _Basis = _Basis?.ToArray() ) as Quant[] ; IEnumerable<Quant> _Source , _Basis ;
		Quantile( Axe context , IEnumerable<Quant> source , Func<Quantile,IEnumerable<Quant>> quantizer = null ) { Axe = context ; _Source = source ; Quantizer = quantizer ; }
		public Quantile( Axe context , Func<Quantile,IEnumerable<Quant>> quantizer = null , IEnumerable<Quant> distribution = null , Axe on = null ) : this(context,distribution??context?.Distribution,quantizer) => _Source = (_Basis=_Source).Get(s=>{var M=context.Measure(on);return s.Select(d=>M[d]);}) ;
		Quantile( Quantile context , Func<Quantile,IEnumerable<Quant>> quantizer ) : this(null,context.Distribution,quantizer) => Context = context ;
		public Quant this[ int key ] => Distribution.At(key) ;
		public Quant this[ Quant level ] { get { var b = Basis ; if( level<b?[0] ) return this[0] ; for( var i=0 ; i<b?.Length-1 ; ++i ) if( b[i]<=level && b[i+1]>=level ) return this[i] ; return this[Count-1] ; } }
		public Quant Tres( Quant level ) { var s = Source ; var b = Basis ; if( s==null || b==null || level>s?[0] ) return b?[0]??0 ; for( var i=0 ; i<s.Length-1 ; ++i ) if( s[i]>=level && s[i+1]<=level ) return b[i] ; return b[b.Length-1] ; }
		public int Count => Distribution?.Length ?? 0 ;
		int AtExtreme { get { Quant ex = 0 , cd ; var j = 0 ; var s = Source ; var b = Basis ; for( var i = 0 ; i<Count-1 ; ++i ) if( (cd=Math.Abs((s[i]-s[i+1])/(b[i]-b[i+1])))>ex ) { ex = cd ; j = i ; } return j ; } }
		public Duo Extreme => AtExtreme.Do(j=>new Duo{X=this[j].nil(),Y=Basis.at(j)}) ;
		public Duo Central { get { Quant? cd = 0 ; var s = Source ; var b = Basis ; for( var i=0 ; i<Count-1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ; cd /= (s.at(0)-s.at(Count-1)).Nil().use(Math.Abs) ; var j = 0 ; for(; j<Count-1 ; ++j ) if( b[j]<=cd&&b[j+1]>=cd || b[j]>=cd&&b[j]<=cd ) break ; return new Duo{X=this[j].nil(),Y=cd} ; } }
		public Duo Centre { get { var s = Source ; var b = Basis ; var atex = AtExtreme ; var zero = Zero*(s.at(0)-s.at(Count-1)).use(Math.Abs) ; var i = atex ; for(; i>=0 && i<Count-1 ; --i ) if( Math.Abs(s[i]-s[i+1])<=zero ) break ; Quant? cd = 0 ; for( atex = i = i<0?0:i ; i<Count-1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ; cd /= (s.at(atex)-s.at(Count-1)).Nil().use(Math.Abs) ; return new Duo{X=this[atex].nil(),Y=cd} ; } }
		public Duo Centrum { get { var s = Source ; var b = Basis ; var atex = AtExtreme ; var zero = Zero*(s.at(0)-s.at(Count-1)).use(Math.Abs) ; var i = atex ; for(; i>=0 && i<Count-1 ; --i ) if( Math.Abs(s[i]-s[i+1])<=zero ) break ; var at0 = i<0?0:i ; for( i = atex ; i<Count-1 ; ++i ) if( Math.Abs(s[i]-s[i+1])<=zero ) break ; var at1 = i ; Quant? cd = 0 ; for( i = at0 ; i<Count-1&&i<=at1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ; cd /= (s.at(at0)-s.at(at1)).Nil().use(Math.Abs) ; return new Duo{X=this[at0].nil(),Y=cd} ; } }
		public Duo Center => Centre|Centrum ;
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
		public struct Duo { public Quant? X , Y ; public static Duo operator+( Duo a , Duo b ) => new Duo{ X = a.X+b.X , Y = a.Y+b.Y } ; public static Duo operator/( Duo a , Quant b ) => new Duo{ X = a.X/b.nil() , Y = a.Y/b.nil() } ; public static Duo operator|( Duo a , Duo b ) => (a+b)/2 ; }
		public struct Measure
		{
			readonly Axable Of , On ; Proxable<Quant,Quant> Cash ;
			public Measure( Axable of , Axable on = null ) { Of = of ; On = on ; Cash = null ; } 
			public Quant this[ Quant at ] => (Cash??(Cash=New()))[at,1] ;
			Quant M( int i ) => On is Axable m ? m[i+1]-m[i]??0 : 1 ;
			Proxable<Quant,Quant> New()
			{
				if( Of==null ) return null ;
				var value = new Bin.Maplet<Quant,Quant>{Nil=()=>0} ; for( int i = 0 , c = Math.Max(Of.Count,On?.Count??0) ; i<c ; ++i ) if( Of[i] is Quant val ) value[val] += M(i) ;
				Quant pre = 0 ; foreach( var entry in value.Entries(false) ) { entry.Value += pre ; pre = entry.Value ; }
				return value ;
			}
		}
	}
	/// <summary>
	/// Container of real relative difference indexes for given axe and given difference paramener . 
	/// </summary>
	public struct Lap
	{
		readonly double[] Content ;
		/// <summary>
		/// Constructs container od differences of poisitions proportional to <paramref name="dif"/> by axe value . 
		/// </summary>
		/// <param name="context"> Axe to construct differences by . </param>
		/// <param name="dif"> Difference parameter which by <paramref name="context"/> value exactly corresponds to index difference from position in axe <paramref name="context"/> . </param>
		public Lap( Act.Axe context , Quant dif )
		{
			// Calculation of absolute equidifferenced distribution .
			var retent = new List<double>() ; Quant? oy = null ; for( int c=context?.Count??0 , i=0 ; i<c ; ++i ) if( context[i] is Quant ay )
			if( oy==null ) { oy = ay ; retent.Add(i) ; } else if( context[i-1] is Quant ly && (ay-ly).nil() is double dy ) for( int j=1 , n=(int)((ay-oy)/dif) ; j<=n ; ++j ) (i-1+((oy+=dif)-ly)/dy).Use(retent.Add) ;
			Absolution = retent.ToArray() ;
			// Calculation of diferential distribution .
			var content = new List<int>() ; var dir = Math.Sign(dif) ; dif = Math.Abs(dif) ; if( context!=null )
			if( dir>0 ) for( int c=context.Count , i=0 ; dif>0 && i<c ; content.Add(i-content.Count) ) for( var v = context.Resolve(content.Count) ; i<c && (context.Resolve(i)-v).use(Math.Abs)<dif ; ++i ) ;
			else for( int c=context.Count , i=0 , j=0 ; dif>0 && i<c ; ++j ) for( var v = context.Resolve(j) ; i<c && (context.Resolve(i)-v).use(Math.Abs)<dif ; content.Add(j-++i) ) ;
			var k = 0 ; Content = content.Select(i=>i+( context?[k++] is Quant a && context?[k+i-1] is Quant x && context?[k+i] is Quant y && x!=y ? Math.Abs(Math.Abs(x-a)-dif)/Math.Abs(x-y) : 0 )).ToArray() ;
		}
		/// <summary>
		/// Relative index real (noninteger) difference which must be add to <paramref name="at"/> to obtain exact position , 
		/// where the axe difference from <paramref name="at"/> position is exactly those given by constructor dif argument . 
		/// </summary>
		/// <param name="at"> Index to get pear distanced exactly by <see cref="Lap"/> construct parameter . </param>
		public double? this[ int at ] => Content.at(at) ;
		/// <summary>
		/// Relative index real (noninteger) difference which must be add to <paramref name="at"/> to obtain exact position , 
		/// where the axe difference from <paramref name="at"/> position is exactly those given by constructor dif argument . 
		/// </summary>
		/// <param name="at"> Index to get pear distanced exactly by <see cref="Lap"/> construct parameter . </param>
		public double? this[ double at ] { get { var f = Math.Floor(at) ; var c = Math.Ceiling(at) ; return c==f ? Absolution.at((int)at) : Absolution.at((int)f)*(c-at)+Absolution.at((int)c)*(at-f) ; } }
		/// <summary>
		/// Real exact Positions equidistantly distributed respecting creation parameter difference . 
		/// </summary>
		public double[] Absolution {get;}
		/// <summary>
		/// Specific axe to support Lap via Axe . 
		/// </summary>
		public class Axe : Act.Axe.Support<Lap>
		{
			internal Axe( Act.Axe context , Lap lap ) : base(lap,i=>lap.Absolution.at(i)is double a?context[a]:null,context) => Counter = ()=>lap.Absolution.Length ;
			internal Axe( Act.Axe context , Quant dif ) : this(context,new Lap(context,dif)) {}
			public static implicit operator Lap( Axe a ) => a.Arg ;
		}
	}
	public partial class Path
	{
		public class Axe : Act.Axe , Accessible<int,Quant?>
		{
			new internal Path Context ; uint ax ; Axis axis ; Mark mark ;
			public virtual uint Ax
			{
				get => ax ;
				set
				{
					ax = value ; var meta = Context.Metaxe(value) ;
					base.Spec = value.Get(v=>meta.Name??(v<(uint)Axis.Top||v>(uint)Axis.Lim?((Axis)v).Stringy():null)) ;
					if( !meta.Form.No() ) Binder = meta.Form ;
					Resolver = at=>Context?[at]?[Ax] ;
				}
			}
			public Axis Axis { get => axis==Axis.Time ? Axis.Time : axis==Axis.Date ? Axis.Date : (Axis)ax ; set => Ax = (uint)( axis = value ) ; }
			public Mark Mark { get => mark ; set => Axis = ( mark = value )==Mark.Lap ? Axis.Lap : value==Mark.Stop ? Axis.Stop : value==Mark.Act ? Axis.Act : value==Mark.Ato ? Axis.Ato : value==Mark.Sub ? Axis.Sub : value==Mark.Sup ? Axis.Sup : value==Mark.Hyp ? Axis.Hyp : value==Mark.No ? Axis.No : throw new InvalidEnumArgumentException($"Mark invalid {value} !") ; }
			//public Axis Axis { get => axis ?? Axis4(ax) ; set => Ax = (uint)( axis = value ) ; } Axis? axis ;
			//public virtual uint Ax { get => axis==Axis.Time ? Context.Dimension : axis==Axis.Date ? Context.Dimension+1 : ax ; set { base.Spec = ( ax = value ).Get(v=>v<(uint)Axis.Time?(axis=(Axis)v).Stringy():v<Context.Dimension?Context.Metax?[v].Name:(axis=v==Context.Dimension?Axis.Time:Axis.Date).ToString()) ; Resolver = at=>Context?[at]?[Ax] ; } } uint ax ;
			public override string Spec { get => base.Spec ; set { if( value!=null ) if( value.Axis() is uint v && v<Context.Dimensions ) Ax = v ; else Axis = (Axis)v ; base.Spec = value ; } }
			public Axe( Path context ) => Context = context ;
			public override int Count => Context?.Count ?? 0 ;
			protected override Aspectable DefaultAspect => Context?.Spectrum ;
			bool Intensive => Axis==Axis.Time || Axis==Axis.Date || Axis==Axis.Bit || Axis==Axis.Beat ;
			public new Quant? this[ int at ] { get => Context[at][Axis] ; set => Context[at][Axis] = value ; }
			Quant? Accessible<int,Quant?>.this[ int at ] { get => base[at] ; set => this[at] = value ; }
			#region Operations
			public Act.Axe Propagation( (Quant time,Quant potential) a , (Quant time,Quant potential) b , Quant? tranq = null ) => new Act.Axe( i=>(Resolve(i),Intensive).Propagation(a,b,tranq??Context.Profile?.Tranq) , this ) ;
			public Act.Axe Propagation( (TimeSpan time,Quant potential) a , (TimeSpan time,Quant potential) b , Quant? tranq = null ) => new Act.Axe( i=>(Resolve(i),Intensive).Propagation(a,b,tranq??Context.Profile?.Tranq) , this ) ;
			public Act.Axe Propagation( (Quant potential,TimeSpan time) a , (Quant potential,TimeSpan time) b , Quant? tranq = null ) => new Act.Axe( i=>(Resolve(i),Intensive).Propagation(a,b,tranq??Context.Profile?.Tranq) , this ) ;
			public Act.Axe Propagation( params (Quant time,Quant potential)[] a ) => a?.Length>1 ? a.Duplets().Get( d => new Act.Axe( i=>d.Average(p=>(Resolve(i),Intensive).Propagation(p.A,p.B,Context.Profile?.Tranq)) , this ) ) : No ;
			public Act.Axe Propagation( params (TimeSpan time,Quant potential)[] a ) => a?.Length>1 ? a.Duplets().Get( d => new Act.Axe( i=>d.Average(p=>(Resolve(i),Intensive).Propagation(p.A,p.B,Context.Profile?.Tranq)) , this ) ) : No ;
			public Act.Axe Propagation( params (Quant potential,TimeSpan time)[] a ) => a?.Length>1 ? a.Duplets().Get( d => new Act.Axe( i=>d.Average(p=>(Resolve(i),Intensive).Propagation(p.A,p.B,Context.Profile?.Tranq)) , this ) ) : No ;
			public Act.Axe Propagation( params (Quant time,Quant potential,Quant weight)[] a ) => a?.Length>1 ? a.Duplets().Get( d => new Act.Axe( i=>d.Centre(p=>(Resolve(i),Intensive).Propagation((p.A.time,p.A.potential),(p.B.time,p.B.potential),Context.Profile?.Tranq),p=>p.A.weight*p.B.weight) , this ) ) : No ;
			public Act.Axe Propagation( params (TimeSpan time,Quant potential,Quant weight)[] a ) => a?.Length>1 ? a.Duplets().Get( d => new Act.Axe( i=>d.Centre(p=>(Resolve(i),Intensive).Propagation((p.A.time,p.A.potential),(p.B.time,p.B.potential),Context.Profile?.Tranq),p=>p.A.weight*p.B.weight) , this ) ) : No ;
			public Act.Axe Propagation( params (Quant potential,TimeSpan time,Quant weight)[] a ) => a?.Length>1 ? a.Duplets().Get( d => new Act.Axe( i=>d.Centre(p=>(Resolve(i),Intensive).Propagation((p.A.time,p.A.potential),(p.B.time,p.B.potential),Context.Profile?.Tranq),p=>p.A.weight*p.B.weight) , this ) ) : No ;
			#endregion
		}
		public static Axable operator&( Path path , string name ) => path.Spectrum[name] ;
		public static Axable operator&( Path path , Axis name ) => path.Spectrum[name,false] ;
	}
	public static class AxeOperations
	{
		public static Axe Drift( this int dis , Axe x , Axe y , int? dif = null ) => (dif??dis).Get(d=>d.quo(x,y)).Get(a=>a/a.Skip(dis)) ;
		public static Axe quo( this int dif , Axe x , Axe y ) => dif==0 ? x/y : (x%dif)/(y%dif) ;
		public static Axe quo( this Lap dif , Axe x , Axe y ) => (x/dif)/(y/dif) ;
		public static Axe quo( this Axe dif , Axe x , Axe y ) => dif is Lap.Axe a ? a.Arg.quo(x,y) : Axe.No ;
		public static Axe d( this int dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this Lap dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this Axe dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static IEnumerable<Quant> Refine( this IEnumerable<Quant?> source ) => source?.OfType<Quant>().Distinct().OrderBy(q=>q) ;
		/// <summary>
		/// Seeks last continualpredecessor (subsequent) in <paramref name="file"/> of <paramref name="at"/> position . 
		/// If we consider file as set of continual number subsets , then this function seeks begining of continual subcet which is around the position <paramref name="at"/> . 
		/// </summary>
		/// <param name="at"> Position as <paramref name="file"/> file . </param>
		/// <param name="file"> File of points representing continual (subsequent numbers) sets . </param>
		/// <returns> Index in <paramref name="file"/> where continual subset begins . </returns>
		internal static int LastContinualPredecessorIn( this int at , params int[] file ) { if( at<file.Length ) while( at>0 ) if( file[at-1]+1<file[at] ) return at ; else --at ; return at ; }
		public static Axe Plus( this Axe x , Axe y ) => x==null ? y : y==null ? x : new Axe( i=>(x[i]??0)+(y[i]??0) , x ) ;
	}
}
