﻿using System;
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
		public virtual bool Dirty { get => this.Any(m=>m.Dirty) ; set => this.Each(m=>m.Dirty=value) ; }
		/// <summary>
		/// Applies traits of point .
		/// </summary>
		public void Interact( Point point ) => this.Each(m=>m.Interact(point)) ;
		public Aid.Closure Incognit => new(()=>Inco=true,()=>Inco=false) ;
		public bool Inco { set => this.Each(m=>m.Inco=value) ; }
	}
	public abstract class Medium
	{
		public interface Sharable { string Tags {get;} Mark Mark {get;} }
		public bool Dirty { get => Inco ? false : dirty ; protected internal set { if( value==dirty ) return ; dirty = value ; if( !value ) Clean() ; } } bool dirty ;
		public bool Inco { get => inco>0 ; set { if( value ) ++inco ; else --inco ; } } int inco ;
		public void Interact( Point point ) => Dirty |= Applicable(point) && Applied(point) ;
		public abstract void Interact( Path path ) ;
		protected abstract bool Applied( Point point ) ;
		protected abstract bool Applicable( Point point ) ;
		public Aid.Closure Incognit => new(()=>++inco,()=>--inco) ;
		protected abstract void Clean() ;
	}
	public partial class Path
	{
		public class Mediator : Act.Mediator
		{
			/// <summary>
			/// Takes traits of medium to path and points .
			/// </summary>
			public void Interact( Path path , bool direct = false ) => this.Where(m=>direct||m.Dirty).Each(m=>m.Interact(path)) ;
			public override bool Dirty => !Incognite && base.Dirty ;
			public bool Incognite { get => inco>0 ; set { if( value ) ++inco ; else --inco ; } } int inco ;
			public Aid.Closure Interrupt => new(()=>Incognite=true,()=>Incognite=false) ;
		}
	}
	static class MediExtension
	{
	}
}
