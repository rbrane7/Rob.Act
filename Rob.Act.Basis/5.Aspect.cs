using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	public interface Aspectable : Aid.Gettable<uint,Axe> , Aid.Gettable<Axe> , Aid.Countable<Axe> { string Spec { get; } Aspect.Iterator Points { get; } }
	public class Aspect : List<Axe> , Aspectable , INotifyCollectionChanged
	{
		public Axe this[ string key ] => this.FirstOrDefault(a=>a.Spec==key) ;
		public Axe this[ uint key ] => base[(int)key] ;
		public string Spec { get ; set ; }
		public Aspect( IEnumerable<Axe> axes = null ) : base(axes??Enumerable.Empty<Axe>()) {}
		public Iterator Points => new Iterator{ Context = this } ;
		public struct Iterator : IEnumerable<Quant?[]>
		{
			public Aspect Context { get ; set ; }
			public int Count => Context.Max(a=>a.Count) ;
			public IEnumerator<Quant?[]> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return Context.Select(a=>a[i]).ToArray() ; }
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		}
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		internal void OnChanged( NotifyCollectionChangedAction act , Axable item ) => collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(act,item)) ;
		public new void Add( Axe ax ) { base.Add(ax) ; OnChanged(NotifyCollectionChangedAction.Add,ax) ; }
		public new void Remove( Axe ax ) { base.Remove(ax) ; OnChanged(NotifyCollectionChangedAction.Remove,ax) ; }
	}
	public partial class Path
	{
		public class Aspect : Act.Aspect
		{
			readonly Path Context ;
			public Aspect( Path path ) { Spec = ( Context = path ).Spec ; Add(new Axe(Context){Axis=Axis.Time}) ; for( var ax = Axis.Lon ; ax<Axis.Time ; ++ax ) if( Context[ax]!=null ) Add(new Axe(Context){Axis=ax}) ; }
			public Axable this[ Axis ax ] => this.OfType<Axe>().FirstOrDefault(a=>a.Axis==ax) ?? new Axe(Context){Axis=ax}.Set(Add) ;
		}
		public Aspect Spectrum => aspect ?? ( aspect = new Aspect(this) ) ; Aspect aspect ;
	}
}
