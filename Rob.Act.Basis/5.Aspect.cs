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
	public interface Aspectable : Aid.Gettable<uint,Axe> , Aid.Gettable<Axe> , Aid.Countable<Axe> , Resourcable { string Spec { get; } }
	public interface Resourcable { Aspectable Source { set; } Aspect.Iterable Points { get; } }
	public struct Aspectables : Aid.Gettable<uint,Aspectable> , Aid.Gettable<Aspectable> , Aid.Countable<Aspectable> , Resourcable
	{
		Aspectable[] Content ;
		public Aspectables( params Aspectable[] content ) => Content = content ;
		public Aspectable this[ uint key ] => Content.At((int)key) ;
		public Aspectable this[ string key ] { get { var reg = new Regex(key) ; return Content.SingleOrNo(a=>reg.Match(a.Spec).Success) ; } }
		public int Count => Content.Length ;
		public Aspectable Source { set => this.Each(a=>a.Source=value) ; }
		public Aspect.Iterable Points => new Iterator{Context=this} ;
		public IEnumerator<Aspectable> GetEnumerator() => Content.Cast<Aspectable>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		#region ICollection
		public object SyncRoot => throw new NotImplementedException() ;
		public bool IsSynchronized => throw new NotImplementedException() ;
		public void CopyTo( Array array, int index ) => throw new NotImplementedException() ;
		#endregion
		public struct Iterator : Aspect.Iterable
		{
			public IEnumerable<Aspectable> Context { get; set; }
			public int Count => Context.Max(s=>s.Max(a=>a.Count)) ;
			Aspectable Aspect.Iterable.Context => throw new NotSupportedException("Single context not supported on multi-context version !") ;
			public IEnumerator<Quant?[]> GetEnumerator() => throw new NotSupportedException("Points iterator not supported on multi-context version !") ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;

		}
	}
	public class Aspect : List<Axe> , Aspectable , INotifyCollectionChanged , INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } PropertyChangedEventHandler propertyChanged ;
		public Aspect( Aspect source ) : this(source?.Select(a=>new Axe(a))) => spec = source?.Spec ;
		public Aspect( IEnumerable<Axe> axes = null ) : base(axes??Enumerable.Empty<Axe>()) {}
		public Axe this[ string key ] => this.FirstOrDefault(a=>a.Spec==key) ;
		public Axe this[ uint key ] => base[(int)key] ;
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		public virtual Aspectable Source { set { this.Each(a=>a.Source=value) ; Spec += $" {value?.Spec}" ; } }
		public virtual Iterable Points => new Iterator{Context=this} ;
		public interface Iterable : IEnumerable<Quant?[]> { int Count { get; } Aspectable Context { get; } }
		public struct Iterator : Iterable
		{
			public Aspectable Context { get; set; }
			public int Count => Context.Max(a=>a.Count) ;
			public IEnumerator<Quant?[]> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return Context.Select(a=>a[i]).ToArray() ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		}
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		internal void OnChanged( NotifyCollectionChangedAction act , Axable item ) => collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(act,item)) ;
		internal void OnChanged( object subject , PropertyChangedEventArgs item ) => collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)) ;
		public new void Add( Axe ax ) { base.Add(ax) ; ax.PropertyChanged += OnChanged ; OnChanged(NotifyCollectionChangedAction.Add,ax) ; }
		public new void Remove( Axe ax ) { base.Remove(ax) ; ax.PropertyChanged -= OnChanged ; OnChanged(NotifyCollectionChangedAction.Remove,ax) ; }
		public override string ToString() => Spec ;
	}
	public partial class Path
	{
		public class Aspect : Act.Aspect
		{
			readonly Path Context ;
			public Aspect( Path path ) { Context = path ; Add(new Axe(Context){Axis=Axis.Time}) ; for( var ax = Axis.Lon ; ax<Axis.Time ; ++ax ) if( Context[ax]!=null ) Add(new Axe(Context){Axis=ax}) ; }
			public override string Spec { get => Context.Spec ; set => base.Spec = value ; }
			public override Aspectable Source { set {} }
			public Axable this[ Axis ax ] => this.OfType<Axe>().FirstOrDefault(a=>a.Axis==ax) ?? new Axe(Context){Axis=ax}.Set(Add) ;
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
