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
	public class Mediator : List<Medium>
	{
		/// <summary>
		/// Is medium changed and so elligible to update pathes ?
		/// </summary>
		public bool Dirty { get => this.Any(m=>m.Dirty) ; set => this.Each(m=>m.Dirty=value) ; }
		/// <summary>
		/// Applies traits of point .
		/// </summary>
		public void Interact( Point point ) => this.Each(m=>m.Interact(point)) ;
	}
	public abstract class Medium
	{
		public bool Dirty {get;protected internal set;}
		public void Interact( Point point ) => Dirty |= Apply(point) ;
		public abstract void Interact( Path path ) ;
		protected abstract bool Apply( Point point ) ;
	}
	public partial class Path
	{
		public class Mediator : Act.Mediator
		{
			/// <summary>
			/// Takes traits of frommedium to path and points .
			/// </summary>
			public void Interact( Path path ) => this.Each(m=>m.Interact(path)) ;
		}
	}
	static class MediExtension
	{
	}
}
