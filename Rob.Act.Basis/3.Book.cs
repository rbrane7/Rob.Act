using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Aid.Extension;

namespace Rob.Act
{
	public class Book : Aid.Collections.ObservableList<Path>.Filtered.Texted , Aid.Gettable<int,Path>
	{
		public Book( string subject = null ) => Subject = subject ;
		public string Subject { get ; protected set ; }
		public Path this[ DateTime date ] => this.FirstOrDefault(p=>p.Date==date) ;
		public static Book operator+( Book book , Path path ) => book.Set(b=>b.Add(path)) ;
	}
}
