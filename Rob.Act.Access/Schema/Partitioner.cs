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
			var parts = data.Separate(Environment.NewLine,braces:null) ;
			Object = (data=parts[0].RightFromFirst(Sign)).LeftFromLast('.') ; Name = data.RightFrom('.') ;
			for( var seq = parts.At(1) ; seq?.Contains('-')==true ; seq = seq.RightFromFirst('-') ) Sequence.Add((seq.LeftFrom('-').RightFrom(' ',true).Parse<uint>(),seq.RightFromFirst('-').LeftFrom('-',true).LeftFrom(' ',true).Parse<uint>())) ;
			for( var i = 2 ; i<parts.Length ; ++i ) Correction[parts[i].LeftFrom(':',all:true)] = new SortedDictionary<int,double>(parts[i].RightFromFirst(':').SeparateTrim(',',false).ToDictionary(v=>v.LeftFrom('=').Parse<int>().Value,v=>v.RightFrom('=').Parse<double>().Value)) ;
		}
		public static implicit operator Path( Partitioner p )
		{
			Path path = null ;
			if( p.Sequence.Count+p.Correction.Count>0 && p.Object.get(System.IO.File.Exists)==true ) path = p.Object.Reconcile().Internalize() ;
			var o = 0U ; foreach( var (min,max) in p.Sequence ) { if( min is uint m ) path[(int)(m-o)].Mark |= Mark.Stop ; var a = (int?)min+1-(int)o??0 ; var c = (max??(uint)path.Count)-(min??0U) ; for( o+=c ; c>0 ; --c ) path.RemoveAt(a) ; }
			path.Set(s=>s.Reset()).Set(s=>s.Tag.Add(p.Name)) ;
			foreach( var axe in p.Correction )
			{
				var cor = axe.Value.ToArray() ; var bas = 0D ; for( var i=0 ; i<cor.Length ; ++i )
				{
					var to = path[cor[i].Key]?[axe.Key] ; if( to==cor[i].Value ) continue ; var f = cor.At(i-1).Value ;
					if( (cor[i].Value-f)/(to-bas) is double q ) for( var j=cor.At(i-1).Key+1 ; j<=cor[i].Key ; ++j ) path[j][axe.Key] = f + (path[j][axe.Key]-bas)*q ;
					bas = to.Value ;
				}
				path[axe.Key] = path.Last()[axe.Key]-path.First()[axe.Key] ;
			}
			return path ;
		}
		readonly string Object , Name ; readonly IList<(uint?Min,uint?Max)> Sequence = new List<(uint?Min,uint?Max)>() ;
		readonly IDictionary<string,SortedDictionary<int,double>> Correction = new Dictionary<string,SortedDictionary<int,double>>() ;
	}
}
