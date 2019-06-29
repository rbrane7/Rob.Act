﻿using System;
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
	public interface Aspectable : Aid.Gettable<int,Axe> , Aid.Gettable<Axe> , Aid.Countable<Axe> , Resourcable { string Spec { get; } }
	public interface Resourcable { Aspectable Source { set; } Aspectable[] Sources { set; } Aspect.Point.Iterable Points { get; } }
	public struct Aspectables : Aid.Gettable<int,Aspectable> , Aid.Gettable<Aspectable> , Aid.Countable<Aspectable> , Resourcable
	{
		Aspectable[] Content ;
		public Aspectables( params Aspectable[] content ) => Content = content ;
		public Aspectable this[ int key ] => Content.At(key) ;
		[LambdaContext.Dominant] public Aspectable this[ string key ] { get { var reg = new Regex(key) ; return Content.SingleOrNo(a=>reg.Match(a.Spec).Success) ; } }
		public int Count => Content?.Length ?? 0 ;
		public Aspectable Source { set => this.Each(a=>a.Source=value) ; }
		public Aspectable[] Sources { set => this.Each(a=>a.Sources=value) ; }
		public Aspect.Point.Iterable Points => new Iterator{ Context = this } ;
		public IEnumerator<Aspectable> GetEnumerator() => Content.Cast<Aspectable>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public struct Iterator : Aspect.Point.Iterable
		{
			public Aid.Countable<Aspectable> Context { get; set; }
			public int Count => Context?.Count>0 ? Context.Max(s=>s?.Count>0?s.Max(a=>a?.Count>0?a.Count:0):0) : 0 ;
			Aspectable Aspect.Point.Iterable.Context => throw new NotSupportedException("Single context not supported on multi-context version !") ;
			public IEnumerator<Aspect.Point> GetEnumerator() => throw new NotSupportedException("Points iterator not supported on multi-context version !") ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
			public Aspect.Point this[ int at ] => throw new NotSupportedException("Points not supported on multi-context version !") ;
		}
	}
	public class Aspect : List<Axe> , IList , Aspectable , INotifyCollectionChanged , INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
		public Aspect( Aspect source , bool multi = false ) : this(source?.Where(a=>a.Multi==multi).Select(a=>new Axe(a))) { spec = source?.Spec ; source.Trait.Each(t=>Trait.Add(new Traitlet(t))) ; }
		public Aspect( IEnumerable<Axe> axes = null , Traits trait = null ) : base(axes??Enumerable.Empty<Axe>()) { foreach( var ax in this ) ax.PropertyChanged += OnChanged ; Trait = (trait??new Traits()).Set(t=>t.Context=this) ; }
		public Aspect() : this(axes:null) {} // Default constructor must be present to enable DataGrid implicit Add .
		[LambdaContext.Dominant] public Axe this[ string key ] => this.FirstOrDefault(a=>a.Spec==key) ;
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; Dirty = true ; } } string spec ;
		public string Origin { get => origin ; set { origin = value.Set(v=>Spec=System.IO.Path.GetFileNameWithoutExtension(v)) ; Dirty = true ; } } string origin ;
		public string Score { get => $"{Spec} {Trait}" ; set => propertyChanged.On(this,"Score") ; }
		public Traits Trait { get; }
		public virtual Aspectable Source { get => source ; set { source = value ; this.Each(a=>a.Source=value) ; Spec += $" {value?.Spec}" ; } } Aspectable source ;
		public virtual Aspectable[] Sources { get => sources ; set { sources = value ; this.Each(a=>a.Sources=value) ; } } Aspectable[] sources ;
		public virtual Point.Iterable Points => new Point.Iterator{ Context = this } ;
		public int Index( string axe ) => IndexOf(this[axe]) ;
		public struct Point : Quantable , IEnumerable<Quant?>
		{
			public interface Iterable : IEnumerable<Point> { int Count { get; } Aspectable Context { get; } Point this[ int at ] { get; } }
			Aspect Context ; Quant?[] Content ;
			public Point( Aspect context , params Quant?[] content ) { Context = context ; Content = content ; Mark = Mark.No ; }
			public Point( Aspect context , int at ) { Context = context ; Content = context.Select(a=>a[at]).ToArray() ; Mark = Mark.No ; }
			public Quant? this[ uint key ] => Content[key] ;
			public Quant? this[ string key ] => Content[Context.Index(key)] ;
			public IEnumerator<double?> GetEnumerator() => Content.Cast<Quant?>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
			public Mark Mark ;
			public struct Iterator : Iterable
			{
				public Aspectable Context { get; set; }
				public int Count => Context?.Count>0 ? Context.Max(a=>a.Count) : 0 ;
				public IEnumerator<Point> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
				public Point this[ int at ] => new Point(Context as Aspect,at) ;
			}
		}
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		internal void OnChanged( NotifyCollectionChangedAction act , Axable item ) { collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(act,item)) ; Dirty = true ; }
		internal void OnChanged( object subject = null , PropertyChangedEventArgs item = null ) { collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)) ; Dirty = true ; }
		public new virtual void Add( Axe ax ) { base.Add(ax) ; ax.PropertyChanged += OnChanged ; OnChanged(NotifyCollectionChangedAction.Add,ax) ; }
		public new virtual void Remove( Axe ax ) { base.Remove(ax) ; ax.PropertyChanged -= OnChanged ; OnChanged(NotifyCollectionChangedAction.Remove,ax) ; }
		void IList.Remove( object value ) => Remove( value as Axe ) ;
		int IList.Add( object value ) { Add( value as Axe ) ; return Count-1 ; }
		public override string ToString() => Score ;
		#region De/Serialization
		/// <summary>
		/// Is object in dirty state ? To be reserialized .
		/// </summary>
		public bool Dirty ;
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Aspect( string text ) => text.Get(t=>new Aspect(t.LeftFromLast(Serialization.Separator).Separate(Serialization.Separator,braces:null)?.Select(a=>(Axe)a),(Traits)t.RightFrom(Serialization.Separator))) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Aspect aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.Select(x=>(string)x))+(a.Count>0?Serialization.Separator:null)+(string)a.Trait) ;
		protected static class Serialization { public const string Separator = " \x1 Axe \x2\n" ; }
		#endregion
		public Quant Offset { get => offset ; set { if( value!=offset ) propertyChanged.On(this,"Offset",offset=value) ; } } Quant offset ;
		public class Traitlet : INotifyPropertyChanged
		{
			internal Aspect Context ;
			public bool Dirty { set => Context.Set(c=>c.Dirty=value) ; }
			public string Spec { get => name ; set => Changed("Spec",name=value) ; } string name ;
			public string Form { get => form ; set => Changed("Form,Valunit",form=value) ; } string form ;
			public string Lex { get => lex ; set => Changed("Lex,Value,Valunit",Resolver=(lex=value).Compile<Func<Aspect,Quant?>>()) ; } Func<Aspect,Quant?> Resolver ; string lex ;
			void Changed<Value>( string properties , Value value ) { propertyChanged.On(this,properties,value) ; Dirty = true ; }
			public Quant? Value => Resolver?.Invoke(Context) ;
			public override string ToString() => $"{Spec.Null(n=>n.No()).Get(s=>s+'=')}{$"{{0{Form}}}".Form(Value)}" ;
			public Traitlet() {} // Default constructor must be present to enable DataGrid implicit Add .
			public Traitlet( Traitlet source ) { name = source?.Spec ; form = source?.Form ; lex = source?.Lex ; Resolver = source?.Resolver ; }
			public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
			#region De/Serialization
			/// <summary>
			/// Deserializes aspect from string .
			/// </summary>
			public static explicit operator Traitlet( string text ) => text.Separate(Serialization.Separator,braces:null).Get(t=>new Traitlet{Spec=t.At(0),Lex=t.At(1),Form=t.At(2)} ) ;
			/// <summary>
			/// Serializes aspect from string .
			/// </summary>
			public static explicit operator string( Traitlet trait ) => trait.Get(t=>string.Join(Serialization.Separator,t.name,t.lex,t.form)) ;
			static class Serialization { public const string Separator = " \x1 Traitlet \x2 " ; }
			#endregion
		}
		public class Traits : Aid.Collections.ObservableList<Traitlet> , Aid.Gettable<Quant?> , ICollection<Traitlet> , IList , INotifyPropertyChanged
		{
			public bool Dirty { get => Context?.Dirty==true ; set => Context.Set(c=>c.Dirty=value) ; }
			internal Aspect Context { get => context ; set { context = value ; this.Each(t=>t.Context=value) ; } } Aspect context ;
			public Quant? this[ string key ] => key.Get(k=>new Regex(k).Get(r=>this.SingleOrNo(t=>r.Match(t.Spec).Success)))?.Value ;
			public override void Add( Traitlet trait ) => base.Add(trait.Set(t=>{t.Context=Context;t.PropertyChanged+=ChangedItem;Spec=null;Dirty=true;})) ;
			public static Traits operator+( Traits traits , Traitlet trait ) => traits.Set(t=>t.Add(trait)) ;
			public override bool Remove( Traitlet item ) => base.Remove(item).Set(r=>{item.PropertyChanged-=ChangedItem;Spec=null;Dirty=true;}) ;
			void ICollection<Traitlet>.Add( Traitlet trait ) => Add(trait) ;
			int IList.Add( object item ) { Add((Traitlet)item) ; return Count-1 ; }
			void IList.Remove( object value ) => Remove( value as Traitlet ) ;
			public override string ToString() => Spec ;
			public string Spec { get => this.Stringy(',').Null(v=>v.No()) ; protected set => propertyChanged.On(this,"Spec",Context.Set(c=>c.Score=value)) ; }
			void ChangedItem( object subject , PropertyChangedEventArgs prop ) { Spec = null ; Context?.propertyChanged.On(Context,"Trait") ; }
			public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
			#region De/Serialization
			/// <summary>
			/// Deserializes aspect from string .
			/// </summary>
			public static explicit operator Traits( string text ) => text.Get(t=>t.LeftFromLast(Serialization.Separator).Separate(Serialization.Separator,braces:null)?.Select(e=>(Traitlet)e).Aggregate(new Traits(),(a,e)=>a+=e)) ;
			/// <summary>
			/// Serializes aspect from string .
			/// </summary>
			public static explicit operator string( Traits traits ) => traits.Null(t=>t.Count<=0).Get(a=>string.Join(Serialization.Separator,a.Select(x=>(string)x))+Serialization.Separator) ;
			static class Serialization { public const string Separator = " \x1 Trait \x2\n" ; }
			#endregion
		}
	}
	public partial class Path
	{
		public class Aspect : Act.Aspect
		{
			internal Path Context { get => context ; set { context = value ; foreach( var ax in this ) if( ax is Axe a ) a.Context = value ; OnChanged() ; } } Path context ;
			public Aspect( Path path ) { Context = path ; Add(new Axe(Context){Axis=Axis.Time}) ; for( var ax = Axis.Lon ; ax<Axis.Time ; ++ax ) if( Context[ax]!=null ) Add(new Axe(Context){Axis=ax}) ; this.Each(a=>a.Quantizer=null) ; }
			public override string Spec { get => Context.Spec ; set => base.Spec = value ; }
			public override Aspectable Source { set {} }
			public Axable this[ Axis ax ] { get => this.OfType<Axe>().FirstOrDefault(a=>a.Axis==ax) ?? new Axe(Context){Axis=ax}.Set(Add) ; }
			public override void Add( Act.Axe ax ) => base.Add(ax.Set(a=>{if(!(a is Axe))a.Aspect=this;})) ;
			public override void Remove( Act.Axe ax ) => base.Remove(ax.Set(a=>{if(!(a is Axe))a.Aspect=null;})) ;
			public void Reform( params string[] binds ) => this.OfType<Axe>().Each(a=>a.Form=binds.At((int)(uint)a.Axis)) ;
			public override Point.Iterable Points => new Iterator{ Context = this } ;
			public class Iterator : Point.Iterable
			{
				public Aspectable Context { get; set; }
				public int Count => (Context as Aspect)?.Context.Count??0 ; //Math.Max((Context as Aspect)?.Context.Count??0,Context.Where(a=>!(a is Axe)&&a.Counts).Null(e=>e.Count()<=0)?.Max(a=>a.Count)??0) ;
				public IEnumerator<Point> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
				public Point this[ int at ] => new Point(Context as Aspect,at){Mark=(Context as Aspect)?.Context[at]?.Mark??Mark.No} ;
			}
			#region De/Serialization
			public static explicit operator string( Aspect aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.Where(x=>!(x is Axe)).Select(x=>(string)x))+(a.Count(x=>!(x is Axe))>0?Serialization.Separator:null)+(string)a.Trait) ;
			#endregion
		}
		public Aspect Spectrum { get => aspect ?? ( aspect = new Aspect(this) ) ; internal set => aspect = value.Set(s=>s.Context=this) ; } Aspect aspect ;
		public string Origin { get => Spectrum.Origin ; set { Spectrum.Origin = value ; var asp = (Act.Aspect)System.IO.Path.ChangeExtension(value,"spt").ReadAllText() ; if( asp==null ) return ; foreach( var ax in asp ) if( Spectrum[ax.Spec]==null ) Spectrum.Add(ax) ; foreach( var trait in asp.Trait ) Spectrum.Trait.Add(trait) ; } }
	}
}
