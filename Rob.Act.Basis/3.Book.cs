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
	public class Book : IEnumerable<Path> , Aid.Gettable<int,Path> , INotifyCollectionChanged
	{
		public Book( string subject = null ) => Subject = subject ;
		public string Subject { get ; protected set ; }
		public Path this[ DateTime date ] => Content.At(date) ;
		public Path this[ int index ] => this.Skip(index).FirstOrDefault() ;
		IDictionary<DateTime,Path> Content = new SortedDictionary<DateTime,Path>() ;
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		public void Add( Path path ) { path.Set(p=>Content.Add(p.Date,p)) ; collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,path)) ; }
		public void Assign( Path path ) { path.Set(p=>Content[p.Date]=p) ; collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,path)) ; }
		public IEnumerator<Path> GetEnumerator() => Content.Values.GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public static Book operator+( Book book , Path path ) => book.Set(b=>b.Add(path)) ;
		public static Book operator|( Book book , Path path ) => book.Set(b=>b.Assign(path)) ;
	}
}
