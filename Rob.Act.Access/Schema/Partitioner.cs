using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	public class Partitioner
	{
		public const string Sign = "Parter.Source=" ;
		public Partitioner( string data )
		{
			Object = (data=data.RightFromFirst(Sign)).LeftFromLast('.') ; Name = (data=data.RightFrom('.')).LeftFrom(Environment.NewLine) ;
			for( var seq = data.RightFromFirst(Environment.NewLine) ; !seq.No() ; seq = seq.RightFromFirst('-').RightFromFirst(' ',true) )
				Sequence.Add((seq.LeftFrom('-').RightFromFirst(' ',true).Parse<uint>(),seq.RightFromFirst('-').LeftFrom('-',true).LeftFrom(' ',true).Parse<uint>())) ; 
		}
		public static implicit operator Path( Partitioner p )
		{
			Path path = null ;
			if( p.Sequence.Count>0 && p.Object.get(System.IO.File.Exists)==true )
			{
				path = p.Object.Reconcile().Internalize() ; var o = 0 ;
				foreach( var (min,max) in p.Sequence ) { if( min is uint m ) path[(int)m-o].Mark |= Mark.Stop ; int a = (int?)min+1-o??o , c = (int?)max-o??path.Count ; for( o=c-a ; c>a ; --c ) path.RemoveAt(a) ; }
			}
			return path.Set(s=>s.Reset()).Set(s=>s.Tags.Add(p.Name)) ;
		}
		readonly string Object , Name ; readonly IList<(uint?Min,uint?Max)> Sequence = new List<(uint?Min,uint?Max)>() ;
	}
}
