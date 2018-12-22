using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using System.Collections ;
	using System.ComponentModel;
	using Quant = Double ;
	public interface Axable : Aid.Gettable<int,Quant?> , Aid.Countable<Quant?> { Func<int,Aspectable,Quant?> Resolver { get; set; } Func<Aspectable,int> Counter { get; set; } string Spec { get; } }
	public class Axe : Axable , Aspectable , INotifyPropertyChanged
	{
		public Axe( Func<int,Aspectable,Quant?> resolver = null ) => Resolver = resolver ;
		public virtual Aspectable Aspect { get => aspect ?? ( aspect = new Aspect{Spec=Spec} ) ; set => aspect = value ; } protected Aspectable aspect ; string reference ; //serialization purpose
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public Quant? this[ int at ] => (uint)at<Count ? Resolve(at) : null as Quant? ;
		public virtual int Count => Counter is Func<Aspectable,int> c ? c(this) : Aspect.Points.Count ;
		public Func<int,Aspectable,Quant?> Resolver { get => resolver ; set { if( resolver==value ) return ; resolver = value ; resolvelet = null ; } } Func<int,Aspectable,Quant?> resolver ;
		public string Resolvelet { get => resolvelet ; set { if( value==resolvelet ) return ; Resolver = value.Compile<Func<Aspectable,Axe>>()(this).Resolver ; resolvelet = value ; } } string resolvelet ;
		public Func<Aspectable,int> Counter { get => counter ; set { if( counter==value ) return ; counter = value ; countlet = null ; } } Func<Aspectable,int> counter ;
		public string Countlet { get => countlet ; set { if( value==countlet ) return ; Counter = value.Compile<Func<Aspectable,int>>() ; countlet = value ; } } string countlet ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Aspect.Iterator Points => Aspect.Points ;
		public IEnumerator<Quant?> GetEnumerator() { for( int i = 0 , count = Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		protected virtual Quant? Resolve( int at ) => Resolver?.Invoke(at,this) ;
		#region Aspectable
		public Axe this[ string key ] => key==Spec ? this : Aspect?[key] as Axe ;
		Axe Aid.Gettable<uint,Axe>.this[ uint key ] => key<=Aspect?.Count ? Aspect[key] : this ;
		IEnumerator<Axe> IEnumerable<Axe>.GetEnumerator() => Aspect?.GetEnumerator() ?? Enumerable.Empty<Axe>().GetEnumerator() ;
		#endregion
		#region ICollection
		object ICollection.SyncRoot => throw new NotImplementedException() ;
		bool ICollection.IsSynchronized => throw new NotImplementedException() ;
		void ICollection.CopyTo( Array array , int index ) => throw new NotImplementedException() ;
		#endregion
		#region Operations
		public static Axe operator+( Axe x , Axe y ) => new Axe( (i,ctx)=>x.Resolve(i)+y.Resolve(i) ) ;
		public static Axe operator-( Axe x , Axe y ) => new Axe( (i,ctx)=>x.Resolve(i)-y.Resolve(i) ) ;
		public static Axe operator*( Axe x , Axe y ) => new Axe( (i,ctx)=>x.Resolve(i)*y.Resolve(i) ) ;
		public static Axe operator/( Axe x , Axe y ) => new Axe( (i,ctx)=>y.Resolve(i).Nil().Get(v=>x.Resolve(i)/v) ) ;
		public static Axe operator++( Axe x ) => new Axe( (i,ctx)=>x.Resolve(i+1) ) ;
		public static Axe operator>>( Axe x , int lev ) => lev<0 ? x<<-lev : lev==0 ? x : new Axe( (i,ctx)=>x.Resolve(i+1)-x.Resolve(i) )>>lev-1 ;
		public static Axe operator<<( Axe x , int lev ) => lev<0 ? x>>-lev : lev==0 ? x : new Axe( (i,ctx)=>i.Steps().Sum(x.Resolve) )>>lev-1 ;
		public static implicit operator Axe( Func<int,Aspectable,Quant?> resolver ) => resolver.Get(r=>new Axe(r)) ;
		public static implicit operator Axe( Func<int,Quant?> resolver ) => resolver.Get(r=>new Axe((i,c)=>r(i))) ;
		#endregion
	}
	public partial class Path
	{
		public class Axe : Act.Axe
		{
			readonly Path Context ;
			public virtual Axis Axis { get => axis ; set { base.Spec = ( axis = value ).Stringy() ; Resolver = (at,ctx)=>Context[at][Axis] ; } } Axis axis ;
			public override string Spec { get => base.Spec ; set { if( value!=null ) Axis = value.Axis() ; base.Spec = value ; } }
			public Axe( Path context ) => Context = context ;
			public override int Count => Counter is Func<Aspectable,int> c ? c(this) : Context.Count ;
			public override Aspectable Aspect { get => aspect ?? ( base.Aspect = Context.Spectrum ) ; }
		}
		public static Axable operator&( Path path , string name ) => path.Spectrum[name] ;
		public static Axable operator&( Path path , Axis name ) => path.Spectrum[name] ;
	}
}
