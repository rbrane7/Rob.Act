using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	public static class Serialization
	{
		public class Book : Gen.Book
		{
			public static Book operator+( Book book , string path ) => book.Set(b=>b.Add(path.Reconcile().Internalize())) ;
		}
	}
}
