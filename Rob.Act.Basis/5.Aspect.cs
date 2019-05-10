using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	public interface Aspectable : Aid.Gettable<int,Axe> , Aid.Gettable<Axe> , Aid.Countable<Axe> , Resourcable { string Spec { get; } }
	public interface Resourcable { Aspectable Source { set; } Aspectable[] Sources { set; } Aspect.Iterable Points { get; } }
	public struct Aspectables : Aid.Gettable<int,Aspectable> , Aid.Gettable<Aspectable> , Aid.Countable<Aspectable> , Resourcable
	{
		Aspectable[] Content ;
		public Aspectables( params Aspectable[] content ) => Content = content ;
		public Aspectable this[ int key ] => Content.At(key) ;
		[LambdaContext.Dominant] public Aspectable this[ string key ] { get { var reg = new Regex(key) ; return Content.SingleOrNo(a=>reg.Match(a.Spec).Success) ; } }
		public int Count => Content?.Length ?? 0 ;
		public Aspectable Source { set => this.Each(a=>a.Source=value) ; }
		public Aspectable[] Sources { set => this.Each(a=>a.Sources=value) ; }
		public Aspect.Iterable Points => new Iterator{ Context = this } ;
		public IEnumerator<Aspectable> GetEnumerator() => Content.Cast<Aspectable>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public struct Iterator : Aspect.Iterable
		{
			public IEnumerable<Aspectable> Context { get; set; }
			public int Count => Context?.Count()>0 ? Context.Max(s=>s?.Count>0?s.Max(a=>a?.Count>0?a.Count:0):0) : 0 ;
			Aspectable Aspect.Iterable.Context => throw new NotSupportedException("Single context not supported on multi-context version !") ;
			public IEnumerator<Quant?[]> GetEnumerator() => throw new NotSupportedException("Points iterator not supported on multi-context version !") ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;

		}
		#region ICollection
		public object SyncRoot => throw new NotImplementedException() ;
		public bool IsSynchronized => throw new NotImplementedException() ;
		public void CopyTo( Array array , int index ) => throw new NotImplementedException() ;
		#endregion
	}
	public class Aspect : List<Axe> , IList , Aspectable , INotifyCollectionChanged , INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
		public Aspect( Aspect source , bool multi = false ) : this(source?.Where(a=>a.Multi==multi).Select(a=>new Axe(a))) { spec = source?.Spec ; source.Trait.Each(t=>Trait.Add(new Traitlet(t))) ; }
		public Aspect( IEnumerable<Axe> axes = null , Traits trait = null ) : base(axes??Enumerable.Empty<Axe>()) => Trait = (trait??new Traits()).Set(t=>t.Context=this) ;
		public Aspect() : this(axes:null) {} // Default constructor must be present to enable DataGrid implicit Add .
		[LambdaContext.Dominant] public Axe this[ string key ] => this.FirstOrDefault(a=>a.Spec==key) ;
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public string Origin { get => origin ; set => origin = value.Set(v=>Spec=System.IO.Path.GetFileNameWithoutExtension(v)) ; } string origin ;
		public string Score { get => $"{Spec} {Trait}" ; set => propertyChanged.On(this,"Score") ; }
		public Traits Trait { get; }
		public virtual Aspectable Source { set { this.Each(a=>a.Source=value) ; Spec += $" {value?.Spec}" ; } }
		public virtual Aspectable[] Sources { set => this.Each(a=>a.Sources=value) ; }
		public virtual Iterable Points => new Iterator{ Context = this } ;
		public interface Iterable : IEnumerable<Quant?[]> { int Count { get; } Aspectable Context { get; } }
		public struct Iterator : Iterable
		{
			public Aspectable Context { get; set; }
			public int Count => Context?.Count>0 ? Context.Max(a=>a.Count) : 0 ;
			public IEnumerator<Quant?[]> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) { Quant?[] val = null ; try { val = Context.Select(a=>a[i]).ToArray() ; } catch( System.Exception ex ) { System.Diagnostics.Trace.TraceWarning(ex.Stringy()) ; yield break ; } yield return val ; } } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		}
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		internal void OnChanged( NotifyCollectionChangedAction act , Axable item ) => collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(act,item)) ;
		internal void OnChanged( object subject , PropertyChangedEventArgs item ) => collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)) ;
		public new virtual void Add( Axe ax ) { base.Add(ax) ; ax.PropertyChanged += OnChanged ; OnChanged(NotifyCollectionChangedAction.Add,ax) ; }
		public new virtual void Remove( Axe ax ) { base.Remove(ax) ; ax.PropertyChanged -= OnChanged ; OnChanged(NotifyCollectionChangedAction.Remove,ax) ; }
		void IList.Remove( object value ) => Remove( value as Axe ) ;
		int IList.Add( object value ) { Add( value as Axe ) ; return Count-1 ; }
		public override string ToString() => Score ;
		#region De/Serialization
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Aspect( string text ) => text.Get(t=>new Aspect(t.LeftFromLast(Serialization.Separator).Separate(Serialization.Separator,braces:null).Select(a=>(Axe)a),(Traits)t.RightFrom(Serialization.Separator))) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Aspect aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.Select(x=>(string)x))+(a.Count>0?Serialization.Separator:null)+(string)a.Trait) ;
		static class Serialization { public const string Separator = " \x1 Axe \x2\n" ; }
		#endregion
		public class Traitlet : INotifyPropertyChanged
		{
			internal Aspect Context ;
			public string Spec { get => name ; set => propertyChanged.On(this,"Spec",name=value) ; } string name ;
			public string Unit { get => unit ; set => propertyChanged.On(this,"Unit,Valunit",unit=value) ; } string unit ;
			public string Lex { get => lex ; set => propertyChanged.On(this,"Lex,Value,Valunit",Resolver=(lex=value).Compile<Func<Aspect,Quant?>>()) ; } Func<Aspect,Quant?> Resolver ; string lex ;
			public Quant? Value => Resolver?.Invoke(Context) ;
			public string Valunit => Value+Unit ;
			public override string ToString() => $"{Spec.Null(n=>n.No()).Get(s=>s+'=')}{Value}{Unit.Null(v=>v.No())}" ;
			public Traitlet() {} // Default constructor must be present to enable DataGrid implicit Add .
			public Traitlet( Traitlet source ) { name = source?.Spec ; unit = source?.Unit ; lex = source?.Lex ; Resolver = source?.Resolver ; }
			public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
			#region De/Serialization
			/// <summary>
			/// Deserializes aspect from string .
			/// </summary>
			public static explicit operator Traitlet( string text ) => text.Separate(Serialization.Separator,braces:null).Get(t=>new Traitlet{name=t.At(0),lex=t.At(1),unit=t.At(2)} ) ;
			/// <summary>
			/// Serializes aspect from string .
			/// </summary>
			public static explicit operator string( Traitlet trait ) => trait.Get(t=>string.Join(Serialization.Separator,t.name,t.lex,t.unit)) ;
			static class Serialization { public const string Separator = " \x1 Traitlet \x2 " ; }
			#endregion
		}
		public class Traits : Aid.Collections.ObservableList<Traitlet> , Aid.Gettable<Traitlet> , ICollection<Traitlet> , IList , INotifyPropertyChanged
		{
			internal Aspect Context { get => context ; set { context = value ; this.Each(t=>t.Context=value) ; } } Aspect context ;
			public Traitlet this[ string key ] => key.Get(k=>new Regex(k).Get(r=>this.SingleOrNo(t=>r.Match(t.Spec).Success))) ;
			public override void Add( Traitlet trait ) => base.Add(trait.Set(t=>{t.Context=Context;t.PropertyChanged+=ChangedItem;Spec=null;})) ;
			public static Traits operator+( Traits traits , Traitlet trait ) => traits.Set(t=>t.Add(trait)) ;
			public override bool Remove( Traitlet item ) => base.Remove(item).Set(r=>{item.PropertyChanged-=ChangedItem;Spec=null;}) ;
			void ICollection<Traitlet>.Add( Traitlet trait ) => Add(trait) ;
			int IList.Add( object item ) { Add((Traitlet)item) ; return Count-1 ; }
			void IList.Remove( object value ) => Remove( value as Traitlet ) ;
			public override string ToString() => Spec ;
			public string Spec { get => this.Stringy(',').Null(v=>v.No()) ; protected set => propertyChanged.On(this,"Spec",Context.Score=value) ; }
			void ChangedItem( object subject , PropertyChangedEventArgs prop ) { Spec = null ; Context?.propertyChanged.On(Context,"Trait") ; }
			public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
			#region De/Serialization
			/// <summary>
			/// Deserializes aspect from string .
			/// </summary>
			public static explicit operator Traits( string text ) => text.Get(t=>t.LeftFromLast(Serialization.Separator).Separate(Serialization.Separator,braces:null)?.Select(e=>(Traitlet)e).Aggregate(new Traits(),(a,e)=>a+=e)) ;
			/// <summary>
			/// Serializes aspect from string .
			/// </summary>
			public static explicit operator string( Traits traits ) => traits.Null(t=>t.Count<=0).Get(a=>string.Join(Serialization.Separator,a.Select(x=>(string)x)+Serialization.Separator)) ;
			static class Serialization { public const string Separator = " \x1 Trait \x2\n" ; }
			#endregion
		}
	}
	public partial class Path
	{
		public class Aspect : Act.Aspect
		{
			readonly Path Context ;
			public Aspect( Path path ) { Context = path ; Add(new Axe(Context){Axis=Axis.Time}) ; for( var ax = Axis.Lon ; ax<Axis.Time ; ++ax ) if( Context[ax]!=null ) Add(new Axe(Context){Axis=ax}) ; this.Each(a=>a.Quantizer=null) ; }
			public override string Spec { get => Context.Spec ; set => base.Spec = value ; }
			public override Aspectable Source { set {} }
			public Axable this[ Axis ax ] => this.OfType<Axe>().FirstOrDefault(a=>a.Axis==ax) ?? new Axe(Context){Axis=ax}.Set(Add) ;
			public override void Add( Act.Axe ax ) => base.Add(ax.Set(a=>{if(!(a is Axe))a.Aspect=this;} )) ;
			public override void Remove( Act.Axe ax ) => base.Remove(ax.Set(a=>{if(!(a is Axe))a.Aspect=null;} )) ;
			public override Iterable Points => new Iterator{ Context = this } ;
			public new class Iterator : Iterable
			{
				public Aspectable Context { get; set; }
				public int Count => (Context as Aspect)?.Context.Count ?? 0 ;
				public IEnumerator<Quant?[]> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return Context.Select(a=>a[i]).ToArray() ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
			}
		}
		public Aspect Spectrum => aspect ?? ( aspect = new Aspect(this) ) ; Aspect aspect ;
	}
}
