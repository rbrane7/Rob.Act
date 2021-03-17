using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using System.Collections;
	using Quant = Double;
	public class Markage
	{
		public abstract class Land : Medium
		{
			Mark Signature = Mark.Ato|Mark.Sub|Mark.Sup|Mark.Hyp ;
			/// <summary>
			/// Applies traits of point .
			/// </summary>
			/// <returns> True if there was any medium modification . </returns>
			protected override bool Apply( Point point )
			{
				if( point?.Mark is Mark mark && (mark&Signature)>0 ); else return false ;
				return true ;
			}
		}
	}
	public partial class Path
	{
		public class Marklane : Markage.Land
		{
			/// <summary>
			/// Takes traits of frommedium to path and points .
			/// </summary>
			public override void Interact( Path path )
			{
				for( var i=0 ; i<path?.Count ; ++i )
				{
				}
			}
		}
	}
	static class MerklaneExtension
	{
	}
}
