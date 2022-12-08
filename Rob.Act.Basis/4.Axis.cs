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

	public interface Axable : Gettable<int,Quant?> , Gettable<double,Quant?> , Countable<Quant?> { string Spec {get;} }
	public class Axe : Axable , INotifyPropertyChanged
	{
		public const string Extern = LambdaContext.Act.Extern ;
		public readonly static Support No = new(resolver:i=>null as Quant?) , One = new(resolver:i=>1) , Zero = new(resolver:i=>1) ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Axe() : this(null,null) {} // Default constructor must be present to enable DataGrid implicit Add .
		public Axe( Func<int,Quant?> resolver = null , Axe source = null , Func<int> counter = null )
		{
			this.resolver = resolver ; this.counter = counter ; aspect = source?.Aspect ; aspects = source?.Aspects??default ; rex = source?.rex??default ; selectlet = source?.selectlet ; selector = source?.selector ;
			multi = source?.multi??default ; delta = source?.delta??default ; meany = source?.meany??default ;
		}
		public Axe( Axe source , IEnumerable<Aspectable> primary = null , IEnumerable<Aspectable> secondary = null )
		{
			var dax = source?.Deref(primary)??source?.Deref(secondary)??source ; spec = dax?.spec ; aspect = source?.aspect ;
			resolvelet = dax?.resolvelet ; resolver = dax?.resolver ; multi = dax?.multi??default ; delta = dax?.delta??default ; meany = dax?.meany??default ; bond = source?.bond.Null(v=>v.No())??dax?.bond ;
			var ses = selectlet.No() ? dax : source ; rex = ses?.rex??default ; selectlet = ses?.selectlet ; selector = ses?.selector ; if( source?.distribulet.No()!=false && source?.quantlet.No()!=false ) source = dax ;
			distribulet = source?.distribulet ; distributor = source?.distributor ; quantlet = source?.quantlet ; Quantizer = source?.quantile?.Quantizer ;
		}
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public virtual string Base => Solver?.Base ;
		public string Binder { get => bond ; set { if( value==bond ) return ; bond = value ; propertyChanged.On(this,"Binder") ; } } string bond ;
		public Aspectable Source { set { if( Selector==null && DefaultAspect==null ) Aspect = value ; else Resource.Source = value ; } }
		public Aspectable[] Sources { set { if( Selector==null && DefaultAspects==null ) Aspects = new Aspectables(value) ; else Resource.Sources = value ; } }
		public bool Multi { get => multi ; set { if( value==multi ) return ; multi = value ; Aspect = null ; propertyChanged.On(this,"Multi,Aspects") ; } } bool multi ;
		public bool Delta { get => delta ; set { if( value==delta ) return ; delta = value ; /*Aspect = null ;*/ propertyChanged.On(this,"Delta") ; } } bool delta ;
		public bool Meany { get => delta||meany ; set { if( value==meany ) return ; meany = value ; /*Aspect = null ;*/ propertyChanged.On(this,"Meany") ; } } bool meany ;
		public bool Regular => Multi==false || Selector!=null ;
		public bool Asrex { get => rex ; set { if( value==rex ) return ; rex = value ; Selectlet = selectlet ; propertyChanged.On(this,"Asrex,Aspects") ; } } bool rex ;
		Axe Deref( IEnumerable<Aspectable> aspects ) => IsRef ? Spec.RightFrom(Extern,all:true).Get(s=>(Spec.LeftFromLast(Extern)is string asp?aspects?.Where(a=>asp==a.Spec):aspects)?.SelectMany(a=>a).SingleOrNo(x=>x.Spec==s&&!x.IsRef)) : null ;
		public Axe DeRef => Deref(Act.Aspect.Set)??this ; public bool IsRef => (resolver==null||resolver==No.resolver) && resolvelet.No() ;
		protected virtual Aspectable DefaultAspect => Multi!=false ? null : Selection?.SingleOrNo() ;
		protected virtual Aspectable[] DefaultAspects => Multi!=false ? Selection?.ToArray() : null ;
		/// <summary> Cant' be null . </summary>
		protected internal Resourcable Resource => Aspect ?? Aspects as Resourcable ;
		public IEnumerable<Aspectable> Resources => (Regular?Multi!=false?aspects:aspect?.Times():null) ?? Enumerable.Empty<Aspectable>() ;
		protected virtual Aspectables Aspects { get => aspects.No ? aspects = new Aspectables(DefaultAspects) : aspects ; set { aspects = value ; Resolver = null ; propertyChanged.On(this,"Aspects,Aspect") ; } } Aspectables aspects ;
		/// <summary> Aspect which this axe operates on . </summary>
		public virtual Aspectable Aspect { get => aspect ??= DefaultAspect ; set { if( aspect==value ) return ; aspect = value ; Resolver = null ; propertyChanged.On(this,"Aspect") ; } } protected Aspectable aspect ;
		public virtual int Count => Counter?.Invoke() ?? Resource.Points.Count ;
		protected virtual Func<int> Counter { get => counter ?? Resolver.Get(_=>counter) ; set => counter = value ; } Func<int> counter ;
		/// <summary> Own aspect of this axe , which it belongs to . </summary>
		internal Aspectable Own ;
		IEnumerable<Aspectable> Selection => Selector?.Invoke(Aspectables.The.All?.Invoke()) ;
		Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> Selector { get => selector ; set { if( selector==value ) return ; selector = value ; Aspect = null ; propertyChanged.On(this,"Selector") ; } } Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> selector ;
		public string Aspectlet { get => selectlet ?? Aspect?.Spec ; set { if( value==Aspectlet ) return ; Selectlet = value ; propertyChanged.On(this,"Aspectlet") ; } } string selectlet ;
		string Selectlet { set { selectlet = value.Null(s=>s.No()) ; var aspectlet = Asrex ? value.Null(s=>s.No()).Get(v=>new Regex(v)) : null ; Selector = aspectlet==null ? selectlet.Compile<Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>>>() : s=>s.Where(a=>aspectlet.Match(a.Spec).Success) ; } }
		public Quant? this[ int at ] => Resolve(at) ;
		/// <summary> Calculates value of this axe at exact <paramref name="at"/> positin . Calculation uses linear interpolation for intermediary positions . </summary>
		/// <param name="at"> Exact position where to calculate the axe value . </param>
		/// <returns> Interpolated value of axe . </returns>
		public Quant? this[ double at ] => Resolver.Inter(at) ; //{ get { var f = Math.Floor(at) ; var c = Math.Ceiling(at) ; return c==f ? Resolve((int)at) : (Resolve((int)f)*(c-at)+Resolve((int)c)*(at-f)) ; } }
		public int? AtOf( Quant? value ) => value.Get(v=>this.Best(q=>q is Quant u?Math.Abs(u-v):Quant.MaxValue)?.at) ;
		public Quantile.Measure Measure( Axe on ) => new(this,on) ;
		public Quant this[ Quant at , Axe ax ] { get { if( ax==null ) return this.Count(q=>q>=at) ; Quant rez = 0 ; for( int i=0 , count=Count ; i<count ; ++i ) if( Resolve(i)>=at ) rez += ax[i+1]-ax[i]??0 ; return rez ; } }
		//Aid.Collections.BinSetNative<Quant> Cash => cash ??( cash = new Aid.Collections.BinArray<(Quant,int)>.Map<Quant>(Count.Steps().Select(i=>(this[i],i))) ) ; Aid.Collections.BinSetNative<Quant> cash ;
		/// <summary> Value of <see cref="Axe"/> at position <paramref name="at"/> . </summary>
		/// <param name="at"> Position to evaluate at . </param>
		/// <returns> Value at position . </returns>
		/// <remarks> This function is not to be overriden as it would violate constructive chaining of <see cref="Axe"/> . </remarks>
		internal Quant? Resolve( int at ) => Resolver?.Invoke(at) ;
		public Axe Solver => Resolver.Get(_=>coaxe) ;
		Axe Coaxe { get {
			try
			{
				coaxes = null ; coaxe = Multi!=false ?
				Aspects.Get(a=>Resolvelet.Compile<Func<Contexts,Axe>>(use:"Rob.Act").Of(new Contexts{Base=a,This=Own,The=this})) :
				Aspect.Get(a=>Resolvelet.Compile<Func<Context,Axe>>(use:"Rob.Act").Of(new Context{Base=a,This=Own,The=this})) ;
				#if Delta4Multi
				if( Multi && Delta ) coaxes = Resolvelet.Compile<Func<Context,Axe>>(use:"Rob.Act").Get(f=>Aspects.Select(a=>f.Of(new Context{Base=a,This=Own,The=this})).ToArray()) ;
				#endif
				return coaxe ;
			}
			catch( LambdaContext.Exception ) { throw ; } catch( System.Exception e ) { throw new InvalidOperationException($"Problem resolving {Spec} !",e) ; }
			finally { coaxe.Set(c=>Counter=c.Counter) ; }
		} }
		Axe coaxe ; Axe[] coaxes ;
		/// <summary> Never null . If nul than always throws . </summary>
		protected Func<int,Quant?> Resolver { private get { if( resolver is null ) { var r = (Coaxe??No).Resolver ; if( Delta && coaxes is not null && r is not null ) resolver = i=>r(i)-coaxes.Average(a=>a[i]) ; else resolver = r ; } return resolver ; } set { if( resolver==value ) return ; resolver = value ; propertyChanged.On(this,"Resolver") ; } } Func<int,Quant?> resolver ;
		public string Resolvelet { get => resolvelet ; set { if( value==resolvelet ) return ; resolvelet = value ; Resolver = null ; propertyChanged.On(this,"Resolvelet") ; } } string resolvelet ;
		public IEnumerator<Quant?> GetEnumerator() => Evaluate(Count).GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public IEnumerable<Quant?> Evaluate( int count ) { for( int i=0 ; i<count ; ++i ) yield return this[i] ; }
		public IEnumerable<Quant?> On( Axe ax ) => Evaluate(ax?.Count??Count) ;
		#region Quantile
		Func<Axe,IEnumerable<Quant>> Distributor { get => distributor ??( distributor = distribulet.Compile<Func<Axe,IEnumerable<Quant>>>() ?? (a=>null) ) ; set { if( distributor==value ) return ; distributor = value ; quantile.Set(q=> Quantile = new Quantile(this,q.Quantizer) ) ; propertyChanged.On(this,"Distributor") ; } } Func<Axe,IEnumerable<Quant>> distributor ;
		public IEnumerable<Quant> Distribution => Distributor?.Invoke(this) ?? DefaultDistribution ;
		protected virtual IEnumerable<Quant> DefaultDistribution => this.Refine().ToArray().Null(d=>d.Length<=0) ;
		public string Distribulet { get => distribulet ; set { if( value==distribulet ) return ; distribulet = value ; Distributor = null ; propertyChanged.On(this,"Distribulet") ; } } string distribulet ;
		public Quantile Quantile { get => quantile ??= new Quantile(this) ; private set => propertyChanged.On(this,"Quantile", quantile = value ) ; } Quantile quantile ;
		public Func<Quantile,IEnumerable<Quant>> Quantizer { get => Quantile.Quantizer ; set { if( value!=quantile?.Quantizer ) propertyChanged.On(this,"Quantizer", Quantile = new Quantile(this,value) ) ; } }
		public string Quantlet { get => quantlet ; set => propertyChanged.On(this,"Quantlet",Quantizer=(quantlet=value).Compile<Func<Quantile,IEnumerable<Quant>>>()) ; } string quantlet ;
		#endregion
		#region Operations
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Support this[ Region fragment ] => On(fragment) ;
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Axe this[ Jot lap ] => By(lap) ;
		/// <summary> Restricts axe to given subset of points , null elsewhere . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Axe this[ Axe axe ] => (axe?.Solver as Jot.Axe??axe) is Jot.Axe lap ? By(lap.Arg) : this ;
		/// <summary> Function on axe . </summary>
		public Axe this[ Func<Quant,Quant> y ] => Fun(y) ;
		public static Axe operator++( Axe x ) => x==null ? No : x.Positive ;
		public static Axe operator--( Axe x ) => x==null ? No : x.Negative ;
		public static Quant? operator+( Axe x ) => x?.Sum() ;
		public static Axe operator-( Axe x ) => x==null ? No : new Axe( i => -x[i] , x ) ;
		public static Axe operator^( Axe x , Axe y ) => x is null || y is null ? No : new Axe( i => x[i] is Quant a && y[i] is Quant b ? Math.Pow(a,b) : null , x ) ;
		public static Axe operator^( Axe x , Quant y ) => x==null ? No : new Axe( i => x[i] is Quant a ? Math.Pow(a,y) : null , x ) ;
		public static Axe operator^( Quant x , Axe y ) => y==null ? No : new Axe( i => y[i] is Quant a ? Math.Pow(x,a) : null , y ) ;
		public static Axe operator^( Axe x , int y ) => x==null ? No : new Axe( i => x[i] is Quant a ? Math.Pow(a,y) : null , x ) ;
		public static Axe operator^( int x , Axe y ) => y==null ? No : new Axe( i => y[i] is Quant a ? Math.Pow(x,a) : null , y ) ;
		public static Axe operator^( Axe x , bool _ ) => x==null ? No : new Axe( i => x[i] is Quant a ? a>0?Math.Log(a):null : null , x ) ;
		public static Axe operator^( bool _ , Axe y ) => y==null ? No : new Axe( i => y[i] is Quant a ? Math.Exp(a) : null , y ) ;
		//public static Axe operator^( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i => x.Resolve(i) is Quant a && y.Resolve(i) is Quant b ? Math.Pow(a,b) : null as Quant? , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator>>( Axe x , int lev ) => x==null ? No : lev<0 ? x<<-lev : lev==0 ? x : new Axe( i=>x[i]-x[i-1] , x )>>lev-1 ;
		public static Axe operator<<( Axe x , int lev ) => x==null ? No : lev<0 ? x>>-lev : lev==0 ? x : new Axe( i=>i.Steps().Sum(x.Resolve) , x )<<lev-1 ;
		public static Axe operator%( Axe x , bool _ ) => x==null ? No : new Axe( i=>x.Dif(i) , x ) ;
		public static Axe operator%( Axe x , Mark lap ) => x==null ? No : new Axe( i=>x.Dif(i,lap) , x ) ;
		public static Axe operator%( Axe x , int dif ) => x==null ? No : new Axe( i=>x.Dif(i,dif) , x ) ;
		public static Axe operator%( Axe x , uint dif ) => x==null ? No : new Axe( i=>x.Dif(i,dif) , x ) ;
		public static Axe operator%( Axe x , Quant dif ) => new Jot.Axe(x,dif) ;
		public static Axe operator%( Axe x , decimal dif ) => new Jot.Axe(x,(Quant)dif/2,-(Quant)dif/2) ;
		public static Axe operator%( Axe x , float mod ) => x==null ? No : new Axe( i=>x[i]%mod , x ) ;
		public static Axe operator%( Axe x , byte mod ) => x==null ? One : new Axe( i=>x[i]%(mod+1) is Quant v ? v>mod-1&&v<=mod ? 1 : null : null , x ) ;
		public static Axe operator%( Axe x , Region mod ) => x==null ? No : x.Floe(mod) ;
		public static Axe operator%( Axe x , Support y ) => x==null ? No : x.Floe(y.Fragment) ;
		public static Axe operator%( Axe x , Axe y ) => x==null ? No : y is Support s ? x.Floe(s.Fragment) : y is null ? x : new Axe( i=>x[i]%y[i] , x ) ;
		public static Axe operator*( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x[i]*y[i] , x ) ;
		public static Axe operator*( Axe x , Quant y ) => x==null ? No : new Axe( i=>x[i]*y , x ) ;
		public static Axe operator*( Quant x , Axe y ) => y==null ? No : new Axe( i=>x*y[i] , y ) ;
		public static Axe operator/( Axe x , Axe y ) => x==null||y==null ? No : (y as Jot.Axe??y.Solver as Jot.Axe).Get(l=>x/l.Arg) ?? new Axe( i=>x[i]/y[i].Nil() , x ) ;
		public static Axe operator/( Axe x , Quant y ) => x==null ? No : new Axe( i=>x[i]/y.nil() , x ) ;
		public static Axe operator/( Quant x , Axe y ) => y==null ? No : new Axe( i=>x/y[i].Nil() , y ) ;
		public static Axe operator/( bool _ , Axe y ) => y==null ? No : new Axe( i=>1/y[i].Nil() , y ) ;
		public static Axe operator/( Axe x , Jot dif ) => x==null ? No : new Axe( i => dif[i] is double d ? dif.Dual ? dif[i,1] is double c ? x.Dif(i,d,c) : null : x.Dif(i,d) : null , x ) ;
		public static Axe operator+( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)+y.Resolve(i) , x ) ;
		public static Axe operator+( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)+y , x ) ;
		public static Axe operator+( Quant x , Axe y ) => y==null ? No : new Axe( i=>x+y.Resolve(i) , y ) ;
		public static Axe operator-( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)-y.Resolve(i) , x ) ;
		public static Axe operator-( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)-y , x ) ;
		public static Axe operator-( Quant x , Axe y ) => y==null ? No : new Axe( i=>x-y.Resolve(i) , y ) ;
		public static Region operator>( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]>val) ;
		public static Region operator<( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]<val) ;
		public static Region operator<( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i+1],v,false))) ;
		public static Region operator>( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i-1],v,true))) ;
		public static Region operator<=( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i+1],v))) ;
		public static Region operator>=( Axe x , IEnumerable<Quant> vals ) => x?.Count.Steps().Where(i=>vals.Any(v=>Affines(x[i],x[i-1],v))) ;
		public static Region operator>=( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]>=val) ;
		public static Region operator<=( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]<=val) ;
		bool Affines( int at , Quant val , bool smooth = true ) => Affines(this[at],this[at-1],val,smooth?default(bool?):true)||Affines(this[at],this[at+1],val,smooth?default(bool?):false) ;
		static bool Affines( Quant? at , Quant? to , Quant val , bool? smooth = null ) => at==val?smooth??true:to==val||at==null||to==null?smooth!=null&&(smooth.Value?at>val:at<val) : at>val==to<val ;
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
		public static Axe operator&( Axe x , Axe y ) => (y?.Solver as Jot.Axe??y) is Jot.Axe m ? m.Arg.cntr(x,m.Ctx) : x.Centre(y) ;
		public static Axe operator&( float y , Axe x ) => x is not null ? new Axe( i=>(x[i]-x[i-1]).use(Math.Abs)<=y?x[i]:null , x ) : No ;
		public static Axe operator&( Axe x , float y ) => x is not null ? new Axe( i=>(x[i]-(x[i-1]+x[i+1])/2).use(Math.Abs)<=y?x[i]:null , x ) : No ;
		public static Axe operator&( Axe x , Quant y ) => x.Nil(v=>v>y) ;
		public static Axe operator&( Quant x , Axe y ) => y.Nil(v=>v<x) ;
		public static Axe operator&( Axe x , Func<Quant,bool> y ) => x.Nil(v=>!y(v)) ;
		public static Axe operator&( Axe x , Func<Quant,Quant> y ) => x.Fun(y) ;
		public static Axe operator|( Axe x , Axe y ) => x==null ? y : y==null ? x : new Axe( i => x[i]*y[i]??x[i]??y[i] , x ) ;
		public static implicit operator Axe( Func<int,Quant?> resolver ) => resolver.Get(r=>new Axe(r)) ;
		public static implicit operator Axe( Quant q ) => new( i=>q ) ;
		public static implicit operator Axe( int q ) => new( i=>q ) ;
		public static implicit operator Quant?( Axe a ) => +a ;
		public Axe Round => new( i=>Resolve(i).use(Math.Round) , this ) ;
		public Axe Floor => new( i=>Resolve(i).use(Math.Floor) , this ) ;
		public Axe Ceil => new( i=>Resolve(i).use(Math.Ceiling) , this ) ;
		public Axe Positive => new( i=>Resolve(i).use(v=>Math.Max(0,v)) , this ) ;
		public Axe Negative => new( i=>Resolve(i).use(v=>Math.Min(0,v)) , this ) ;
		public Axe Skip( int count ) => new( i=>Resolve(count+i) , this ) ;
		public Axe Wait( int count ) => new( i=>Resolve(i<count?0:i-count) , this ) ;
		public Axe Take( int count ) => new( i=>i<count?Resolve(i):null , this ) ;
		public Region Exts( bool max , int? prox = default ) => Count.Steps().Where(i=>Exts(i,max,prox)) ;
		public Region Sups( int? prox = default ) => Exts(true,prox) ;
		public Region Infs( int? prox = default ) => Exts(false,prox) ;
		bool Exts( int at , bool max , int? prox )
		{
			if( this[at] is not Quant val ) return false ; var sign = prox.use(Math.Sign)??1 ;
			if( sign>0 ) for( int i=at-prox??0 , c=at+prox??Count ; i!=c ; ++i ) if( i==at ); else if( this[i] is Quant cov && ((max?val>cov:val<cov)||val==cov&&at<i) ); else return false ;
			if( sign<0 ) for( int i=at-prox??0 , c=at+prox??Count ; i!=c ; --i ) if( i==at ); else if( this[i] is Quant cov && ((max?val<cov:val>cov)||val==cov&&at<i) ) return false ;
			return true ;
		}
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
		public Axe By( Jot lap ) => new Jot.Axe(this,lap) ;
		/// <summary> Creates axe of drift of this axe on given <paramref name="upon"/> . </summary>
		/// <param name="upon"> Axe to calculate drift on . </param>
		/// <param name="quo"> Distance to calculate drift for . </param>
		/// <returns> Axe of drift of this axe <paramref name="at"/> poositon for <paramref name="dis"/>tance <paramref name="upon"/> axis . </returns>
		public Axe Drift( Axe upon , Quant quo = 0 ) => upon==null ? No : new Axe( i=>(quo*i).Get(at=>Drift(upon,(int)at,(int)((i-at)/2))) , this ) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , Mark? lap = null , bool dif = false ) => Resolve(at+(dif?1:0))-Resolve(Own?.Raw?[lap,at-1+(dif?1:0)]??0) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , int dif , bool shift = false ) => dif==0 ? Dif(at,dif:shift) : (Resolve(at+dif-(shift?1:0))-Resolve(at-(shift?1:0)))*Math.Sign(dif) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <param name="fif"> Final non-integer tail of difference . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Diff( int at , uint dif , double eif = default , double fif = default )
		{
		#if true
			(Quant val,Quant at)? min = default , max = default ;
			if( eif!=default ) if( this[at+eif] is Quant v ) min = max = (v,eif) ;
			for( var i = 0 ; i<=dif ; ++i ) if( Resolve(at+i) is Quant v ) { if( v>=min?.val );else min = (v,i) ; if( v<=max?.val );else max = (v,i) ; }
			if( fif!=default ) if( this[at+dif+fif] is Quant v ) { if( v>=min?.val );else min = (v,dif+eif) ; if( v<=max?.val );else max = (v,dif+eif) ; }
			return (max?.val-min?.val)*(max?.at-min?.at).use(Math.Sign) ?? ( min is null && max is null ? null : 0 ) ;
		#else
			Quant? dal = null , dam = null , lav = null ;
			for( var i = 0 ; i<=dif ; ++i ) if( Resolve(at+i) is Quant v )
				if( lav is null ) { lav = v ; if( eif!=default ) if( (this[at+eif]-lav)*Math.Sign(eif) is Quant ld ) dal = dam = ld ; } else
				{
					var d = v-lav.Value ; lav = v ;
					if( dal is null ) dal = d ; else if( d!=0 ) if( dal+d is Quant dan ) { if( dam is null || Math.Abs(dan)>Math.Abs(dam.Value) ) dam = dan ; dal = dan ; }
				}
			if( fif!=default ) if( (this[at+dif+fif]-lav)*Math.Sign(fif) is Quant ld ) { if( dal is null ) dal = ld ; else dal += ld ; if( dal.use(Math.Abs)>dam.use(Math.Abs) ) dam = dal ; }
			return dam ?? dal ;
		#endif
		}
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Diff( int at , int dif , double fif = default ) => dif<0 ? -Diff(at+dif,(uint)-dif,eif:fif) : Diff(at,(uint)dif,fif:fif) ;
		/// <summary>
		/// Calculates value difference of this axe between value <paramref name="at"/> positin and position differing exactly by real <paramref name="dif"/> . 
		/// Calculation uses linear interpolation for intermediary positions . 
		/// </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Exact real index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		/// <remarks> Optimized variant of <see cref="Dif(int, Quant, Quant)"/> for single limit relative to <see cref="at"/> position . </remarks>
		Quant? Diff( int at , double dif ) => Diff(at,(int)dif,dif.Frac()) ;
		/// <summary>
		/// Calculates value difference of this axe between value <paramref name="at"/> positin and position differing exactly by real <paramref name="dif"/> . 
		/// Calculation uses linear interpolation for intermediary positions . 
		/// </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Exact real index difference from position <paramref name="at"/> of upper limit . </param>
		/// <param name="cif"> Exact real index difference from position <paramref name="at"/> of lower limit . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Diff( int at , double dif , double cif ) => dif<cif ? Diff(at+(int)dif,(uint)(cif-dif),dif.Frac(),cif.Frac()) : Diff(at+(int)cif,(uint)(dif-cif),cif.Frac(),dif.Frac()) ;
		public Axe Extent( int dif ) => new( i=>Diff(i,dif) , this ) ;
		public Axe Extent( Jot dif ) => new( i => dif[i] is double d ? dif.Dual ? dif[i,1] is double c ? Diff(i,d,c) : null : Diff(i,d) : null , this ) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , uint dif ) => dif<=1 ? Dif(at,dif:dif>0) : (Resolve(at+(int)(dif>>1))-Resolve(at-(int)(dif>>1))) ;
		/// <summary> Calculates value difference of this axe between value <paramref name="at"/> positin and position differing by <paramref name="dif"/> . </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Ave( int at , int dif ) => dif.Steps(at).Aggregate(0D as Quant?,(a,i)=>Resolve(i)) ;
		/// <summary>
		/// Calculates value difference of this axe between value <paramref name="at"/> positin and position differing exactly by real <paramref name="dif"/> . 
		/// Calculation uses linear interpolation for intermediary positions . 
		/// </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Exact real index difference from position <paramref name="at"/> . </param>
		/// <returns> Difference value of axe . </returns>
		/// <remarks> Optimized variant of <see cref="Dif(int, Quant, Quant)"/> for single limit relative to <see cref="at"/> position . </remarks>
		Quant? Dif( int at , double dif ) => (this[at+dif]-this[at])*Math.Sign(dif) ;
		/// <summary>
		/// Calculates value difference of this axe between value <paramref name="at"/> positin and position differing exactly by real <paramref name="dif"/> . 
		/// Calculation uses linear interpolation for intermediary positions . 
		/// </summary>
		/// <param name="at"> Position where to calculate differce . </param>
		/// <param name="dif"> Exact real index difference from position <paramref name="at"/> of upper limit . </param>
		/// <param name="cif"> Exact real index difference from position <paramref name="at"/> of lower limit . </param>
		/// <returns> Difference value of axe . </returns>
		Quant? Dif( int at , double dif , double cif ) => (this[at+dif]-this[at+cif])*Math.Sign(dif-cif) ;
		Quant? Quo( Axe upon , int at , int dif ) => Dif(at,dif)/upon.Dif(at,dif).Nil() ;
		/// <summary> Calculates drift of this axe on given <paramref name="upon"/> . </summary>
		/// <param name="upon"> Axe to calculate drift on . </param>
		/// <param name="at"> Position where to get drift at . </param>
		/// <param name="dis"> Distance to calculate drift for . </param>
		/// <returns> Value of drift <paramref name="at"/> poositon for <paramref name="dis"/>tance <paramref name="upon"/> axis . </returns>
		Quant? Drift( Axe upon , int at , int dis ) => Quo(upon,at,dis)/Quo(upon,at+dis,dis).Nil() ;
		public Axe Rift( Axe upon , uint quo = 9 ) => upon==null ? No : new Axe( i=>Drift(upon,i,((Count-i)>>1)-1) , this ) ;
		/// <summary> <see cref="Act.Jot"/> for given parameter <paramref name="dif"/> and this Axe . </summary>
		public Axe By( Quant dif ) => By(new Jot(this,dif)) ;
		public Axe Nil( Predicate<Quant> nil ) => new( i=>Resolve(i).Nil(nil) , this ) ;
		public Axe Fun( Func<Quant,Quant> fun ) => new( i=>Resolve(i).use(fun) , this ) ;
		public Axe PacePower( Quant grade = 0 , Quant? resi = null , Quant flow = 0 , Quant grane = 0 ) => new Axe( i=>Resolve(i).PacePower(grade,(Aspect as Aspect)?.Resistance(resi)??0,flow,grane,Aspect?.Raw?.Profile?.Mass) , this ) ;
		public Axe PowerPace( Quant grade = 0 , Quant? resi = null , Quant flow = 0 , Quant grane = 0 ) => new Axe( i=>Resolve(i).PowerPace(grade,resi??Aspect?.Raw?.Resister??0,flow,Aspect?.Raw?.Profile?.Mass) , this ) ;
		public Axe Centre( Axe measure ) => this*measure/+measure ;
		public Axe Centre( Axe measure , int dif ) => dif.cntr(this,measure) ;
		public Axe Centre( Axe measure , Jot dif ) => dif.cntr(this,measure) ;
		public Axe Centre( Axe measure , Axe dif ) => dif.cntr(this,measure) ;
		#endregion
		#region De/Serialization
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Axe( string text ) => text.Separate( Serialization.Separator,braces:null).Get(t=>new Axe{Spec=t.At(0),Multi=t.At(1)==Serialization.Multier,Resolvelet=t.At(2),Delta=t.At(3).Consists(Serialization.Delter),Meany=t.At(3).Consists(Serialization.Meaner),Selectlet=t.At(4),Distribulet=t.At(5),Quantlet=t.At(6),Binder=t.At(7),Asrex=t.At(8)==Serialization.Rex}) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Axe aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.spec,a.multi?Serialization.Multier:null,a.resolvelet,$"{(a.Delta?Serialization.Delter:null)}{(a.Meany?Serialization.Meaner:null)}",a.selectlet,a.distribulet,a.quantlet,a.Binder,a.rex?Serialization.Rex:string.Empty)) ;
		static class Serialization { public const string Separator = " \x1 Axlet \x2 " , Multier = "*" , Delter = "∆" , Meaner = "⊚" , Rex = "rex" ; }
		#endregion
		public struct Context : Contextable
		{
			public Aspectable Base , This ; public Axe The ;
			/// <returns> Axe or null if unresolvable . Returning null is significant for fallback explicit processing . </returns>
			[LambdaContext.Dominant] public Axe this[ string key ] => This?[key] is Axe a && a!=The ? a : Base?[key] ;
			public Axe this[ Quant value ] => new Support(resolver:i=>value) ;
			public Axe this[ Func<int,Quant?> fun ] => new Support(resolver:fun) ;
			public Support this[ Region fragment ] => One[fragment] ;
			public Support this[ Mark mark , Region fragment ] => fragment is Region f ? new Marker(mark,f,This) : No ;
			public Path Raw => Base?.Raw ;
			public Aspect.Traits Trait => This?.Trait ;
			public Axe Perf( Axe lap ) => lap is Jot.Axe a ? Perf(a.Arg) : No ;
			public Axe Perf( Jot lap ) => (Base as Path.Aspect)?.perf(lap) ?? No ;
			public Axe Perf( int dif = 0 ) => (Base as Path.Aspect)?.perf(dif) ?? No ;
		}
		public struct Contexts : Contextables
		{
			public Aspectables Base ; public Aspectable This ; public Axe The ;
			/// <returns> Axe or null if unresolvable . Returning null is significant for fallback explicit processing . </returns>
			[LambdaContext.Dominant] public Axe this[ string key ] => This?[key] is Axe a && a!=The ? a : null ;
			public Axe this[ Quant value ] => new Support(resolver:i=>value) ;
			public Axe this[ Func<int,Quant?> fun ] => new Support(resolver:fun) ;
			public Aspectable this[ int key ] => Base[key] ;
			public Path Raw( int at = 0 ) => Base[at].Raw ;
			public Support this[ Region fragment ] => One[fragment] ;
			public Support this[ Mark mark , Region fragment ] => fragment is Region f ? new Marker(mark,f,This) : No ;
			public IEnumerator<Aspectable> GetEnumerator() => Base.GetEnumerator() ;
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		}
		public class Support : Support<Region> { public Region Fragment => Arg ; internal Support( Region fragment = default , Func<int,Quant?> resolver = null , Axe source = null ) : base(fragment,resolver,source) {} }
		public class Support<Param> : Axe { public readonly Param Arg ; public readonly Axe Ctx ; internal Support( Param arg , Func<int,Quant?> resolver = null , Axe source = null ) : base(resolver,source) { Arg = arg ; Ctx = source ; } }
		/// <summary> Solver of automatic <see cref="Mark"/> placement . </summary>
		public class Marker : Support
		{
			readonly Mark Mark ; HashSet<int> Frag => Fragment as HashSet<int> ; Point Ori( int at ) => Aspect?.Raw?[at] ;
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
	public class Quantile : Gettable<int,Quant> , Countable<Quant>
	{
		static readonly Quant[] Empty = new Quant[0] ; static readonly Quant Zero = 0.0017 ;
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
		public Duo Central
		{ get {
			Quant? cd = 0 ; var s = Source ; var b = Basis ; for( var i=0 ; i<Count-1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ;
			cd /= (s.at(0)-s.at(Count-1)).Nil().use(Math.Abs) ; var j = 0 ; for(; j<Count-1 ; ++j ) if( b[j]<=cd&&b[j+1]>=cd || b[j]>=cd&&b[j]<=cd ) break ;
			return new Duo{X=this[j].nil(),Y=cd} ;
		} }
		public Duo Centre
		{ get {
			var s = Source ; var b = Basis ; var atex = AtExtreme ; var zero = Zero*(s.at(0)-s.at(Count-1)).use(Math.Abs) ;
			var i = atex ; for(; i>=0 && i<Count-1 ; --i ) if( Math.Abs(s[i]-s[i+1])<=zero ) break ;
			Quant? cd = 0 ; for( atex = i = i<0?0:i ; i<Count-1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ;
			return new Duo{ X = this[atex].nil() , Y = cd / (s.at(atex)-s.at(Count-1)).Nil().use(Math.Abs) } ;
		} }
		public Duo Center
		{ get {
			var s = Source ; var b = Basis ; var atex = AtExtreme ; var zero = Zero*(s.at(0)-s.at(Count-1)).use(Math.Abs) ;
			var i = atex ; for(; i>=0 && i<Count-1 ; --i ) if( Math.Abs(s[i]-s[i+1])<=zero ) break ;
			var at0 = i<0?0:i ; for( i = atex ; i<Count-1 ; ++i ) if( Math.Abs(s[i]-s[i+1])<=zero ) break ;
			var at1 = i ; Quant? cd = 0 ; for( i = at0 ; i<Count-1&&i<=at1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ;
			return new Duo{ X = this[at0].nil() , Y = cd / (s.at(at0)-s.at(at1)).Nil().use(Math.Abs) } ;
		} }
		public Duo Centrum => Centre|Center ;
		public Quantile this[ Func<Quantile,IEnumerable<Quant>> quantizer , IEnumerable<Quant> distribution , Axe on = null , bool free = false ]
		=> Axe.Get(a=>new Quantile(a,quantizer,distribution??(free?a.Refine():a.Distribution),on)) ?? new Quantile(Context[distribution,on,free],quantizer) ;
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
		public struct Duo
		{
			public Quant? X , Y ;
			public static Duo operator|( Duo a , Duo b ) => new(){ X = Decide(a.X,b.X) , Y = Decide(a.Y,b.Y) } ;
			static Quant? Decide( Quant? a , Quant? b ) => a is null && b is null ? null : a is not null && b is not null ? (a+b)/2 : a??b ;
		}
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
	/// <summary> Container of real relative difference indexes for given axe and given difference paramener . </summary>
	public struct Jot
	{
		readonly double[] Content ;
		public Jot? Sub => sub as Jot? ; readonly object sub ;
		public bool Dual => sub!=null ;
		/// <summary> Constructs container od differences of poisitions proportional to <paramref name="dif"/> by axe value . </summary>
		/// <param name="context"> Axe to construct differences by . </param>
		/// <param name="dif"> Difference parameter which by <paramref name="context"/> value exactly corresponds to index difference from position in axe <paramref name="context"/> . </param>
		public Jot( Act.Axe context , Quant dif , Quant? codif = null )
		{
			sub = null ; if( codif is Quant s ) sub = new Jot(context,s,null) ;
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
		/// <param name="at"> Index to get pear distanced exactly by <see cref="Jot"/> construct parameter . </param>
		public double? this[ int at , uint lev = 0 ] => lev==0 ? Content.at(at) : Sub?[at,lev-1] ;
		/// <summary>
		/// Relative index real (noninteger) difference which must be add to <paramref name="at"/> to obtain exact position , 
		/// where the axe difference from <paramref name="at"/> position is exactly those given by constructor dif argument . 
		/// </summary>
		/// <param name="at"> Index to get pear distanced exactly by <see cref="Jot"/> construct parameter . </param>
		public double? this[ double at ] => Absolution.Inter(at) ; //{ get { var f = Math.Floor(at) ; var c = Math.Ceiling(at) ; return c==f ? Absolution.at((int)at) : Absolution.at((int)f)*(c-at)+Absolution.at((int)c)*(at-f) ; } }
		/// <summary> Real exact Positions equidistantly distributed respecting creation parameter difference . </summary>
		public double[] Absolution {get;}
		/// <summary> Specific axe to support Lap via Axe . </summary>
		public class Axe : Act.Axe.Support<Jot>
		{
			public override string Base => basis??base.Base ; readonly string basis ;
			internal Axe( Act.Axe context , Jot lap ) : base(lap,i=>lap.Absolution.at(i)is double a?context[a]:null,context) { Counter = ()=>lap.Absolution.Length ; basis = context?.Spec ; }
			internal Axe( Act.Axe context , Quant dif , Quant? codif = null ) : this(context,new Jot(context,dif,codif)) {}
			public static implicit operator Jot( Axe a ) => a.Arg ;
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
			public override string Spec { get => base.Spec ; set { if( value!=null ) if( value.Axis(true).Value is uint v && v<Context.Dimensions ) Ax = v ; else Axis = (Axis)v ; base.Spec = value ; } }
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
		public static Axe quo( this uint dif , Axe x , Axe y ) => dif==0 ? x/y : (x%dif)/(y%dif) ;
		public static Axe quo( this Jot dif , Axe x , Axe y ) => (x/dif)/(y/dif) ;
		public static Axe quo( this Axe dif , Axe x , Axe y ) => dif is Jot.Axe a ? a.Arg.quo(x,y) : Axe.No ;
		public static Axe Quo( this int dif , Axe x , Axe y ) => x is null || y is null ? Axe.No : dif==0 ? x/y : x.Extent(dif)/y.Extent(dif) ;
		public static Axe Quo( this Jot dif , Axe x , Axe y ) => x is null || y is null ? Axe.No : x.Extent(dif)/y.Extent(dif) ;
		public static Axe Quo( this Axe dif , Axe x , Axe y ) => dif is Jot.Axe a ? a.Arg.Quo(x,y) : Axe.No ;
		public static Axe d( this int dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this uint dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this Jot dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this Axe dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this Axe x , Axe y ) => y is Jot.Axe d ? d.Arg.d(x,d.Ctx) : x is Jot.Axe e ? e.Arg.d(y,e.Ctx) : 1.d(x,y) ;
		public static Axe D( this int dif , Axe x , Axe y ) => dif.Quo(x,y) ;
		public static Axe D( this Jot dif , Axe x , Axe y ) => dif.Quo(x,y) ;
		public static Axe D( this Axe dif , Axe x , Axe y ) => dif.Quo(x,y) ;
		public static Axe D( this Axe x , Axe y ) => y is Jot.Axe d ? d.Arg.D(x,d.Ctx) : x is Jot.Axe e ? e.Arg.D(y,e.Ctx) : 1.D(x,y) ;
		public static Axe cntr( this int dif , Axe y , Axe m ) => y==null ? Axe.No : m==null||dif==0 ? y : new Axe( i=>{ var me = m[i+dif]-m[i] ; return me==0 ? null : dif.Steps(i).Sum(j=>y[j]*(m[j+Math.Sign(dif)]-m[j]))/me ; } , y ) ;
		public static Axe cntr( this Jot dif , Axe y , Axe m ) => y==null ? Axe.No : new Axe( i=>{ var d = (int)(dif[i]??0) ; var c = (int)(dif[i,1]??0) ; var me = m[i+d]-m[i+c]??0 ; return me==0 ? null : (d-c).Steps(i).Sum(j=>y[j]*(m[j+Math.Sign(d-c)]-m[j]))/me ; } , y ) ;
		public static Axe cntr( this Axe dif , Axe y , Axe m ) => dif is Jot.Axe a ? a.Arg.cntr(y,m) : y??Axe.No ;
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
