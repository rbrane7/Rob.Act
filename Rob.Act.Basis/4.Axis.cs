﻿using System;
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
	using Aid.Math ;
	public interface Axable : Aid.Gettable<int,Quant?> , Aid.Gettable<Quant,Quant> , IEnumerable<Quant?> { string Spec { get; } }
	public class Axe : Axable , INotifyPropertyChanged
	{
		public readonly static Support No = new Support(null){resolver=i=>null as Quant?} , One = new Support(null,i=>1) ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Axe() : this(null,null) {} // Default constructor must be present to enable DataGrid implicit Add .
		public Axe( Func<int,Quant?> resolver = null , Axe source = null ) { this.resolver = resolver ; Quantile = new Quantile(this) ; aspect = source?.Aspect ; aspects = source?.Aspects??default ; rex = source?.rex??default ; selectlet = source?.selectlet ; selector = source?.selector ; multi = source?.multi??default ; }
		public Axe( Axe source ) { spec = source?.spec ; aspect = source?.aspect ; resolvelet = source?.resolvelet ; resolver = source?.resolver ; rex = source?.rex??default ; selectlet = source?.selectlet ; selector = source?.selector ; distribulet = source?.distribulet ; distributor = source?.distributor ; Quantizer = source?.Quantizer ; multi = source?.multi??default ; bond = source?.bond ; }
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public string Binder { get => bond ; set { if( value==bond ) return ; bond = value ; propertyChanged.On(this,"Binder") ; } } string bond ;
		public Aspectable Source { set { if( Selector==null && DefaultAspect==null ) Aspect = value ; else Resource.Source = value ; } }
		public Aspectable[] Sources { set { if( Selector==null && DefaultAspects==null ) Aspects = new Aspectables(value) ; else Resource.Sources = value ; } }
		public bool Multi { get => multi ; set { if( value==multi ) return ; multi = value ; Resolver = null ; Aspect = null ; propertyChanged.On(this,"Multi,Aspects") ; } } bool multi ;
		public bool Regular => !Multi || Selector!=null ;
		public bool Asrex { get => rex ; set { if( value==rex ) return ; rex = value ; Selectlet = selectlet ; propertyChanged.On(this,"Asrex,Aspects") ; } } bool rex ;
		protected virtual Aspectable DefaultAspect => Multi ? null : Selector?.Invoke(Aspectables.The?.Invoke()).SingleOrNo() ;
		protected virtual Aspectable[] DefaultAspects => Multi ? Selector?.Invoke(Aspectables.The?.Invoke()).ToArray() : null ;
		/// <summary> Cant' be null . </summary>
		protected internal Resourcable Resource => Aspect ?? Aspects as Resourcable ;
		public IEnumerable<Aspectable> Resources => (Regular?Multi?aspects:aspect?.Times():null)??Enumerable.Empty<Aspectable>() ;
		protected virtual Aspectables Aspects { get => aspects.Count>0 ? aspects : ( aspects = new Aspectables(DefaultAspects) ) ; set { aspects = value ; Resolver = null ; propertyChanged.On(this,"Aspects") ; } } Aspectables aspects ;
		public virtual Aspectable Aspect { get => aspect ??( aspect = DefaultAspect ) ; set { if( aspect==value ) return ; aspect = value ; Resolver = null ; propertyChanged.On(this,"Aspect") ; } } protected Aspectable aspect ;
		public virtual int Count => Resource.Points.Count ;
		internal Aspectable Own ;
		Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> Selector { get => selector ; set { if( selector==value ) return ; selector = value ; Aspect = null ; propertyChanged.On(this,"Selector") ; } } Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>> selector ;
		string Aspectlet { get => selectlet ?? Aspect?.Spec ; set { if( value==Aspectlet ) return ; Selectlet = value ; propertyChanged.On(this,"Aspectlet") ; } } string selectlet ;
		string Selectlet { set { selectlet = value.Null(s=>s.No()) ; var aspectlet = Asrex ? value.Null(s=>s.No()).Get(v=>new Regex(v)) : null ; Selector = aspectlet==null ? selectlet.Compile<Func<IEnumerable<Aspectable>,IEnumerable<Aspectable>>>() : s=>s.Where(a=>aspectlet.Match(a.Spec).Success) ; } }
		public Quant this[ Quant at ] => this.Count(q=>q>=at) ;
		public Quant this[ Quant at , Axe ax ] { get { if( ax==null ) return this[at] ; Quant rez = 0 ; for( int i=0 , count=Count ; i<count ; ++i ) if( Resolve(i)>=at ) rez += ax[i+1]-ax[i]??0 ; return rez ; } }
		public Quant? this[ int at ] => Resolve(at) ;
		protected internal virtual Quant? Resolve( int at ) => Resolver?.Invoke(at) ;
		Axe Coaxe { get { try { return Multi?Aspects.Get(a=>Resolvelet.Compile<Func<Contexts,Axe>>(use:"Rob.Act").Of(new Contexts{Base=a,This=Own,The=this})):Aspect.Get(a=>Resolvelet.Compile<Func<Context,Axe>>(use:"Rob.Act").Of(new Context{Base=a,This=Own,The=this})) ; } catch( LambdaContext.Exception ) { throw ; } catch( System.Exception e ) { throw new InvalidOperationException($"Problem resolving {Spec} !",e) ; } } }
		/// <summary> Never null . If nul than always throws . </summary>
		protected Func<int,Quant?> Resolver { private get => resolver ??( resolver = Coaxe is Axe a ? i=>a[i] : No.Resolver ) ; set { if( resolver==value ) return ; resolver = value ; propertyChanged.On(this,"Resolver") ; } } Func<int,Quant?> resolver ;
		public string Resolvelet { get => resolvelet ; set { if( value==resolvelet ) return ; resolvelet = value ; Resolver = null ; propertyChanged.On(this,"Resolvelet") ; } } string resolvelet ;
		Func<Axe,IEnumerable<Quant>> Distributor { get => distributor ?? ( distributor = distribulet.Compile<Func<Axe,IEnumerable<Quant>>>() ?? (a=>null) ) ; set { if( distributor==value ) return ; distributor = value ; propertyChanged.On(this,"Distributor,Quantile") ; } } Func<Axe,IEnumerable<Quant>> distributor ;
		public IEnumerable<Quant> Distribution => Distributor?.Invoke(this) ?? DefaultDistribution ; //?? Enumerable.Empty<Quant>() ;
		protected virtual IEnumerable<Quant> DefaultDistribution => this.Refine().ToArray().Null(d=>d.Length<=0) ;
		public string Distribulet { get => distribulet ; set { if( value==distribulet ) return ; distribulet = value ; Distributor = null ; propertyChanged.On(this,"Distribulet") ; } } string distribulet ;
		public IEnumerator<Quant?> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public Quantile Quantile { get ; private set ; }
		public Func<Quantile,IEnumerable<Quant>> Quantizer { get => Quantile.Quantizer ; set => propertyChanged.On(this,"Quantizer,Quantile",Quantile=new Quantile(this,value)) ; }
		public string Quantlet { get => quantlet ; set => propertyChanged.On(this,"Quantlet",Quantizer=(quantlet=value).Compile<Func<Quantile,IEnumerable<Quant>>>()) ; } string quantlet ;
		#region Operations
		public static Quant? operator+( Axe x ) => x?.Sum() ;
		public static Axe operator-( Axe x ) => x==null ? No : new Axe( i=>-x.Resolve(i) , x ) ;
		public static Axe operator+( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)+y.Resolve(i) , x ) ;
		public static Axe operator-( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)-y.Resolve(i) , x ) ;
		public static Axe operator*( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)*y.Resolve(i) , x ) ;
		public static Axe operator/( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i=>x.Resolve(i)/y.Resolve(i).Nil() , x ) ;
		//public static Axe operator^( Axe x , Axe y ) => x==null||y==null ? No : new Axe( i => x.Resolve(i) is Quant a && y.Resolve(i) is Quant b ? Math.Pow(a,b) : null as Quant? , a=>Math.Max(x.Count,y.Count) ) ;
		public static Axe operator+( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)+y , x ) ;
		public static Axe operator-( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)-y , x ) ;
		public static Axe operator*( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)*y , x ) ;
		public static Axe operator/( Axe x , Quant y ) => x==null ? No : new Axe( i=>x.Resolve(i)/y.nil() , x ) ;
		public static Axe operator+( Quant x , Axe y ) => y==null ? No : new Axe( i=>x+y.Resolve(i) , y ) ;
		public static Axe operator-( Quant x , Axe y ) => y==null ? No : new Axe( i=>x-y.Resolve(i) , y ) ;
		public static Axe operator*( Quant x , Axe y ) => y==null ? No : new Axe( i=>x*y.Resolve(i) , y ) ;
		public static Axe operator/( Quant x , Axe y ) => y==null ? No : new Axe( i=>x/y.Resolve(i).Nil() , y ) ;
		public static Axe operator/( bool x , Axe y ) => y==null ? No : new Axe( i=>1D/y.Resolve(i).Nil() , y ) ;
		public static Axe operator^( Axe x , Quant y ) => x==null ? No : new Axe( i => x.Resolve(i) is Quant a ? Math.Pow(a,y) : null as Quant? , x ) ;
		public static Axe operator^( Quant x , Axe y ) => y==null ? No : new Axe( i => y.Resolve(i) is Quant a ? Math.Pow(x,a) : null as Quant? , y ) ;
		public static Axe operator^( Axe x , bool y ) => x==null ? No : new Axe( i => x.Resolve(i) is Quant a ? a>0?Math.Log(a):null as Quant? : null as Quant? , x ) ;
		public static Axe operator^( bool x , Axe y ) => y==null ? No : new Axe( i => y.Resolve(i) is Quant a ? Math.Exp(a) : null as Quant? , y ) ;
		public static Axe operator|( Axe x , Axe y ) => x==null ? y : y==null ? x : new Axe( i => x[i]*y[i]??x[i]??y[i] , x ) ;
		public static Axe operator&( Axe x , Axe y ) => x.Centre(y) ;
		public static Axe operator&( Axe x , Quant y ) => x.Nil(v=>v>y) ;
		public static Axe operator&( Quant x , Axe y ) => y.Nil(v=>v<x) ;
		public static Axe operator&( Axe x , Func<Quant,bool> y ) => x.Nil(v=>!y(v)) ;
		public static Axe operator&( Axe x , Func<Quant,Quant> y ) => x.Fun(y) ;
		public static Axe operator++( Axe x ) => x==null ? No : new Axe( i=>x.Resolve(i+1) , x ) ;
		public static Axe operator--( Axe x ) => x==null ? No : new Axe( i=>x.Resolve(i-1) , x ) ;
		public static Axe operator>>( Axe x , int lev ) => x==null ? No : lev<0 ? x<<-lev : lev==0 ? x : new Axe( i=>x.Resolve(i)-x.Resolve(i-1) , x )>>lev-1 ;
		public static Axe operator<<( Axe x , int lev ) => x==null ? No : lev<0 ? x>>-lev : lev==0 ? x : new Axe( i=>i.Steps().Sum(x.Resolve) , x )<<lev-1 ;
		public static Axe operator^( Axe x , Axe y ) => x==null ? No : x.Drift(y) ;
		public static Axe operator%( Axe x , int dif ) => x==null ? No : new Axe( i=>x.Diff(i,dif) , x ) ;
		public static Lap operator%( Axe x , Quant dif ) => x.By(dif) ;
		public static Axe operator%( Axe x , float mod ) => x==null ? No : new Axe( i=>x.Resolve(i)%mod , x ) ;
		public static Axe operator%( Axe x , IEnumerable<int> mod ) => x==null ? No : x.Flor(mod) ;
		public static Axe operator%( Axe x , Support y ) => x==null ? No : x.Flor(y.Fragment) ;
		public static Axe operator/( Axe x , Lap dif ) => x==null ? No : new Axe( i=>x.Diff(i,dif[i]) , x ) ;
		public static Axe operator%( Axe x , Axe y ) => x==null ? No : x.Rift(y) ;
		public static IEnumerable<int> operator>( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]>val) ;
		public static IEnumerable<int> operator<( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]<val) ;
		public static IEnumerable<int> operator>=( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]>=val) ;
		public static IEnumerable<int> operator<=( Axe x , Quant? val ) => x?.Count.Steps().Where(i=>x[i]<=val) ;
		public static IEnumerable<int> operator>( Quant? val , Axe x ) => x<val ;
		public static IEnumerable<int> operator<( Quant? val , Axe x ) => x>val ;
		public static IEnumerable<int> operator>=( Quant? val , Axe x ) => x<=val ;
		public static IEnumerable<int> operator<=( Quant? val , Axe x ) => x>=val ;
		public static IEnumerable<int> operator>( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]>y[i]) ;
		public static IEnumerable<int> operator<( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]<y[i]) ;
		public static IEnumerable<int> operator>=( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]>=y[i]) ;
		public static IEnumerable<int> operator<=( Axe x , Axe y ) => Math.Min(x?.Count??0,y?.Count??0).Steps().Where(i=>x[i]<=y[i]) ;
		public static implicit operator Axe( Func<int,Quant?> resolver ) => resolver.Get(r=>new Axe(r)) ;
		public static implicit operator Axe( Quant q ) => new Axe( i=>q ) ;
		public static implicit operator Axe( int q ) => new Axe( i=>q ) ;
		public static implicit operator Quant?( Axe a ) => +a ;
		public Axe Round => new Axe( i=>Resolve(i).use(Math.Round) , this ) ;
		public Axe Skip( int count ) => new Axe( i=>Resolve(count+i) , this ) ;
		public Axe Wait( int count ) => new Axe( i=>Resolve(i<count?0:i-count) , this ) ;
		public Axe Take( int count ) => new Axe( Resolve , this ) ;
		/// <summary> Restricts axe to given subset of pointys . </summary>
		/// <param name="fragment"> Points subset to restrict axe on . </param>
		public Support For( IEnumerable<int> fragment ) => fragment.Get(f=>new HashSet<int>(f)).Get(f=>new Support(f,i=>f.Contains(i)?Resolve(i):null,this)) ?? No ;
		/// <summary> Axe of values relative to beginning of continual subset the point belongs to . </summary>
		/// <param name="fragment"> Fragment of continual subsets . </param>
		/// <returns> Axe which values are ofsetted to preceding continual predesessing value with respect to <paramref name="fragment"/> . </returns>
		public Support Flor( IEnumerable<int> fragment ) => fragment.Get(f=>f.ToArray()).Get(f=>new Support( f , i => Array.IndexOf(f,i) is int at && at>=0 ? Resolve(i)-Resolve(f[at.LastContinualPredecessorIn(f)]) : null , this )) ?? No ;
		public Support this[ IEnumerable<int> fragment ] => For(fragment) ;
		public Axe Drift( Axe upon , Quant quo = 0 ) => upon==null ? No : new Axe( i=>(quo*i).Get(at=>Drift(upon,(int)at,(int)((i-at)/2))) , this ) ;
		Quant? Diff( int at , int dif ) => (Resolve(at+dif)-Resolve(at))*Math.Sign(dif) ;
		Quant? Diff( int at , double dif ) { var a = at+dif ; var f = Math.Floor(a) ; var c = Math.Ceiling(a) ; return c==f ? Diff(at,(int)dif) : (Resolve((int)f)*(c-a)+Resolve((int)c)*(a-f)-Resolve(at))*Math.Sign(dif) ; }
		Quant? Quot( Axe upon , int at , int dif ) => Diff(at,dif)/upon.Diff(at,dif).Nil() ;
		Quant? Drift( Axe upon , int at , int dis ) => Quot(upon,at,dis)/Quot(upon,at+dis,dis).Nil() ;
		public Axe Rift( Axe upon , uint quo = 9 ) => upon==null ? No : new Axe( i=>Drift(upon,i,((Count-i)>>1)-1) , this ) ;
		public Lap Lap( Quant dif ) => new Lap(this,dif) ;
		public Lap By( Quant dif ) => Lap(dif) ;
		public Axe Nil( Predicate<Quant> nil ) => new Axe( i=>Resolve(i).Nil(nil) , this ) ;
		public Axe Fun( Func<Quant,Quant> fun ) => new Axe( i=>Resolve(i).use(fun) , this ) ;
		public Axe PacePower( Quant grade = 0 , Quant? resi = null , Quant flow = 0 ) => new Axe( i=>Resolve(i).PacePower(grade,(Aspect as Aspect)?.Resistance(resi)??0,flow,Aspect?.Raw?.Profile?.Mass) , this ) ;
		public Axe PowerPace( Quant grade = 0 , Quant? resi = null , Quant flow = 0 ) => new Axe( i=>Resolve(i).PowerPace(grade,resi??Aspect?.Raw?.Resister??0,flow,Aspect?.Raw?.Profile?.Mass) , this ) ;
		public Axe Centre( Axe mesure ) => this*mesure/+mesure ;
		#endregion
		#region De/Serialization
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Axe( string text ) => text.Separate(Serialization.Separator,braces:null).Get(t=>new Axe{Spec=t.At(0),Multi=t.At(1)==Serialization.Multier,Resolvelet=t.At(2),Selectlet=t.At(4),Distribulet=t.At(5),Quantlet=t.At(6),Binder=t.At(7),Asrex=t.At(8)==Serialization.Rex}) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Axe aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.spec,a.multi?Serialization.Multier:string.Empty,a.resolvelet,null,a.selectlet,a.distribulet,a.quantlet,a.Binder,a.rex?Serialization.Rex:string.Empty)) ;
		static class Serialization { public const string Separator = " \x1 Axlet \x2 " ; public const string Multier = "*" , Rex = "rex"; }
		#endregion
		public struct Context : Contextable
		{
			public Aspectable Base , This ; public Axe The ;
			[LambdaContext.Dominant] public Axe this[ string key ] => This?[key] is Axe a && a!=The ? a : Base?[key] ;
			public Support this[ IEnumerable<int> fragment ] => One[fragment] ;
			public Path Raw => Base?.Raw ;
			public Aspect.Traits Trait => This?.Trait ;
			public Axe Perf( Lap lap ) => (Base as Path.Aspect)?.perf(lap) ?? Axe.No ;
			public Axe Perf( int dif = 0 ) => (Base as Path.Aspect)?.perf(dif) ?? Axe.No ;
		}
		public struct Contexts : Contextables
		{
			public Aspectables Base ; public Aspectable This ; public Axe The ;
			[LambdaContext.Dominant] public Axe this[ string key ] => This?[key] is Axe a && a!=The ? a : null ;
			public Aspectable this[ int key ] => Base[key] ;
			public Path Raw( int at = 0 ) => Base[at].Raw ;
			public Support this[ IEnumerable<int> fragment ] => One[fragment] ;
		}
		public class Support : Axe { public readonly IEnumerable<int> Fragment ; internal Support( IEnumerable<int> fragment , Func<int,Quant?> resolver = null , Axe source = null ) : base(resolver,source) => Fragment = fragment ; }
	}
	public class Quantile : Aid.Gettable<int,Quant> , Aid.Countable<Quant>
	{
		static readonly Quant[] EmptyDis = new Quant[0] ;
		static Quant Zero = 0.0017 ;
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
		int AtExtreme { get { Quant ex = 0 , cd = 0 ; var j = 0 ; var s = Source ; var b = Basis ; for( var i = 0 ; i<Count-1 ; ++i ) if( (cd=Math.Abs((s[i]-s[i+1])/(b[i]-b[i+1])))>ex ) { ex = cd ; j = i ; } return j ; } }
		public Duo Extreme => AtExtreme.Do(j=>new Duo{X=this[j].nil(),Y=Basis.at(j)}) ;
		public Duo Central { get { Quant? cd = 0 ; var s = Source ; var b = Basis ; for( var i = 0 ; i<Count-1 ; ++i ) cd += Math.Abs((s[i]-s[i+1])*(b[i]+b[i+1]))/2 ; cd /= (s.at(0)-s.at(Count-1)).Nil().use(Math.Abs) ; var j = 0 ; for(; j<Count-1 ; ++j ) if( b[j]<=cd&&b[j+1]>=cd || b[j]>=cd&&b[j]<=cd ) break ; return new Duo{X=this[j].nil(),Y=cd} ; } }
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
	}
	public struct Lap : Aid.Gettable<int,double>
	{
		readonly double[] Content ;
		public Lap( Axe context , Quant dif )
		{
			var content = new List<int>() ; var dir = Math.Sign(dif) ; dif = Math.Abs(dif) ; if( context!=null )
			{
				if( dir>0 ) for( int c=context.Count , i=0 ; dif>0 && i<c ; content.Add(i-content.Count) ) for( var v = context.Resolve(content.Count) ; i<c && (context.Resolve(i)-v).use(Math.Abs)<dif ; ++i ) ;
				else for( int c=context.Count , i=0 , j=0 ; dif>0 && i<c ; ++j ) for( var v = context.Resolve(j) ; i<c && (context.Resolve(i)-v).use(Math.Abs)<dif ; content.Add(j-++i) ) ;
			}
			var k = 0 ; Content = content.Select(i=>i+( context?[k++] is Quant a && context?[k+i-1] is Quant x && context?[k+i] is Quant y && x!=y ? Math.Abs(Math.Abs(x-a)-dif)/Math.Abs(x-y) : 0 )).ToArray() ;
		}
		public double this[ int key ] => Content.At(key) ;
	}
	public partial class Path
	{
		public class Axe : Act.Axe
		{
			new internal Path Context ;
			public virtual uint Ax { get => axis ; set { base.Spec = ( axis = value ).Get(v=>v<(uint)Axis.Time?((Axis)v).Stringy():v<Context.Dimension?Context.Metax?[v].Name:v==Context.Dimension?Axis.Time.ToString():Axis.Date.ToString()) ; Resolver = at=>Context?[at]?[Ax] ; } } uint axis ;
			public Axis Axis { get => axis==Context.Dimension||ax==Axis.Time ? Axis.Time : axis==Context.Dimension+1||ax==Axis.Date ? Axis.Date : axis==(uint)Axis.Time ? (Axis)Context.Dimension : axis==(uint)Axis.Date ? (Axis)Context.Dimension+1 : (Axis)axis ; set => Ax = (ax=value)==Axis.Time ? Context.Dimension : value==Axis.Date ? Context.Dimension+1 : value<Axis.Time ? (uint)value : (uint)value-2 ; } Axis ax ;
			//public Axis Axis { get => axis ?? Axis4(ax) ; set => Ax = (uint)( axis = value ) ; } Axis? axis ;
			//public virtual uint Ax { get => axis==Axis.Time ? Context.Dimension : axis==Axis.Date ? Context.Dimension+1 : ax ; set { base.Spec = ( ax = value ).Get(v=>v<(uint)Axis.Time?(axis=(Axis)v).Stringy():v<Context.Dimension?Context.Metax?[v].Name:(axis=v==Context.Dimension?Axis.Time:Axis.Date).ToString()) ; Resolver = at=>Context?[at]?[Ax] ; } } uint ax ;
			Axis Axis4( uint a ) => a==Context.Dimension ? Axis.Time : a==Context.Dimension+1 ? Axis.Date : a==(uint)Axis.Time ? (Axis)Context.Dimension : a==(uint)Axis.Date ? (Axis)Context.Dimension+1 : (Axis)a ;
			public override string Spec { get => base.Spec ; set { if( value!=null ) if( value.Axis(Context.Dimension) is uint v && v<Context.Dimension ) Ax = v ; else Axis = Axis4(v) ; base.Spec = value ; } }
			public Axe( Path context ) => Context = context ;
			public override int Count => Context?.Count ?? 0 ;
			protected override Aspectable DefaultAspect => Context?.Spectrum ;
			bool Intensive => Axis==Axis.Time || Axis==Axis.Date || Axis==Axis.Bit || Axis==Axis.Beat ;
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
		public static Axe d( this int dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static Axe d( this Lap dif , Axe x , Axe y ) => dif.quo(x,y) ;
		public static IEnumerable<Quant> Refine( this IEnumerable<Quant?> source ) => source?.OfType<Quant>().Distinct().OrderBy(q=>q) ;
		/// <summary> Seeks last continualpredecessor (subsequent) in <paramref name="file"/> of <paramref name="at"/> position . 
		/// If we consider file as set of continual number subsets , then this function seeks begining of continual subcet which is around the position <paramref name="at"/> . </summary>
		/// <param name="at"> Position as <paramref name="file"/> file . </param>
		/// <param name="file"> File of points representing continual (subsequent numbers) sets . </param>
		/// <returns> Index in <paramref name="file"/> where continual subset begins . </returns>
		internal static int LastContinualPredecessorIn( this int at , params int[] file ) { if( at<file.Length ) while( at>0 ) if( file[at-1]+1<file[at] ) return at ; else --at ; return at ; }
		public static Axe Plus( this Axe x , Axe y ) => x==null ? y : y==null ? x : new Axe( i=>(x[i]??0)+(y[i]??0) , x ) ;
	}
}
