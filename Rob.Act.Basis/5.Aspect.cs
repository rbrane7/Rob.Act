using System;
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
	public interface Contextable { [LambdaContext.Dominant] Axe this[ string key ] {get;} Axe.Support this[ IEnumerable<int> fragment ] {get;} Path Raw {get;} Aspect.Traits Trait {get;} }
	public interface Contextables { [LambdaContext.Dominant] Axe this[ string key ] {get;} Axe.Support this[ IEnumerable<int> fragment ] {get;} Aspectable this[ int at ] {get;} Path Raw( int at = 0 ) ; }
	public interface Aspectable : Aid.Gettable<int,Axe> , Contextable , Resourcable , Aid.Countable<Axe> { string Spec {get;} Aspect Base {get;} }
	public interface Resourcable { Aspectable Source {set;} Aspectable[] Sources {set;} Aspect.Point.Iterable Points {get;} }
	public struct Aspectables : Aid.Gettable<int,Aspectable> , Aid.Gettable<Aspectable> , Aid.Countable<Aspectable> , Resourcable
	{
		public static (Func<IEnumerable<Aspectable>> All,Func<IEnumerable<Aspectable>> Def) The ;
		readonly Aspectable[] Content ;
		public bool No => Content==null ;
		public Aspectables( params Aspectable[] content ) => Content = content ;
		public Aspectable this[ int key ] => Content.At(key) ;
		[LambdaContext.Dominant] public Aspectable this[ string key ] { get { var reg = new Regex(key) ; return Content.SingleOrNo(a=>reg.Match(a.Spec).Success) ; } }
		public int Count => Content?.Length ?? 0 ;
		public Aspectable Source { set => this.Each(a=>a.Source=value) ; }
		public Aspectable[] Sources { set => this.Each(a=>a.Sources=value) ; }
		public Aspect.Point.Iterable Points => new Iterator{ Context = this } ;
		public IEnumerator<Aspectable> GetEnumerator() => (Content?.Cast<Aspectable>()??Enumerable.Empty<Aspectable>()).GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		struct Iterator : Aspect.Point.Iterable
		{
			internal Aid.Countable<Aspectable> Context {get;set;}
			public int Count => Context?.Count>0 ? Context.Max(s=>s?.Count(a=>!a.Multi)>0?s.Where(a=>!a.Multi).Max(a=>a?.Count??0):0) : 0 ;
			Aspectable Aspect.Point.Iterable.Context { get => throw new NotSupportedException("Single context not supported on multi-context version !") ; set => throw new NotSupportedException("Single context not supported on multi-context version !") ; }
			public IEnumerator<Aspect.Point> GetEnumerator() => throw new NotSupportedException("Points iterator not supported on multi-context version !") ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
			public Aspect.Point this[ int at ] => throw new NotSupportedException("Points not supported on multi-context version !") ;
		}
	}
	public class Aspect : List<Axe> , IList , Aspectable , INotifyCollectionChanged , INotifyPropertyChanged , ICollection<Axe>
	{
		public static IEnumerable<Aspectable> Set => Aspectables.The.Def.Of() ;
		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
		public Aspect( IEnumerable<Aspect> sources ) : this(sources?.SelectMany(s=>s).Distinct(a=>a.Spec).Select(a=>new Axe(a,Set))) { spec = sources?.Select(s=>s.Spec).Stringy(' ') ; sources?.SelectMany(s=>s.Trait).Distinct(t=>t.Spec).Each(t=>Trait.Add(new Traitlet(t,Set),Set)) ; }
		public Aspect( Aspect source ) : this(source?.Select(a=>new Axe(a,Set))) { spec = source?.Spec ; source.Trait.Each(t=>Trait.Add(new Traitlet(t,Set),Set)) ; taglet = source?.taglet ; }
		void Join( IEnumerable<Axe> source , IEnumerable<Aspectable> set = null ) => source?.Except(this,a=>a.Spec)?.Select(a=>new Axe(a,set)).Set(AddRange) ;
		public Aspect( IEnumerable<Axe> axes = null , Traits trait = null ) : base(axes??Enumerable.Empty<Axe>()) { foreach( var ax in this ) { ax.Own = this ; ax.PropertyChanged += OnChanged ; } Trait = (trait??new Traits()).Set(t=>t.Context=this) ; }
		public Aspect() : this(axes:null) {} // Default constructor must be present to enable DataGrid implicit Add .
		[LambdaContext.Dominant] public Axe this[ string key ] => this.One(a=>a.Spec==key) ?? Base.Null(b=>b==this)?[key] ;
		public virtual string Spec { get => spec ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; Dirty = true ; } } string spec ;
		public string Origin { get => origin ; set { origin = value.Set(v=>Spec=System.IO.Path.GetFileNameWithoutExtension(v).LeftFrom('?',all:true)) ; Dirty = true ; } } string origin ;
		public string Score { get => $"{Spec} {Trait} {Tags}" ; set => propertyChanged.On(this,"Score") ; }
		public Traits Trait { get; }
		public string Taglet { get => taglet ; set { if( value==taglet ) return ; taglet = value.Null(v=>v.No()) ; Tager = null ; tag = null ; propertyChanged.On(this,"Taglet") ; Dirty = true ; } } string taglet ;
		public Action<Aspect> Tager { get => tager ??( tager = taglet.Compile<Action<Aspect>>() ) ; set { tager = value ; tags?.Clear() ; if( value==null ) tags = null ; propertyChanged.On(this,"Tager,Tags") ; } } Action<Aspect> tager ;
		public Tagger Tag => ( tags ?? Tager.Get(t=>System.Threading.Interlocked.CompareExchange(ref tags,new Tagger(p=>{tag=null;propertyChanged.On(this,p??"Tags");}),null)) ?? tags ).Set(t=>{if(t.Count<=0&&!notag)using(new Aid.Closure(()=>notag=true,()=>notag=false))Tager.On(this);}) ; Tagger tags ; bool notag ;
		public string Tags { get => tag ??( tag = Tag.Stringy() ) ; set { if( value==tag ) return ; tag = null ; Tag[value.ExtractTags()] = true ; Score = value ; } } string tag ;
		public virtual Aspectable Source { get => source ; set { source = value ; this.Where(a=>!a.Multi).Each(a=>a.Source=value) ; Spec += $" {value?.Spec}" ; } } Aspectable source ;
		public virtual Aspectable[] Sources { get => sources ; set { sources = value ; this.Where(a=>a.Multi).Each(a=>a.Sources=value) ; } } Aspectable[] sources ;
		public bool Regular => this.All(a=>a.Regular) ;
		IEnumerable<Aspectable> Resources => this.SelectMany(a=>a.Resources).Distinct() ;
		public virtual Point.Iterable Points => new Point.Iterator{ Context = this } ;
		public virtual IList<Point> Pointes { get => pointes ??= new Point.Parit<Point.Iterator>{ Context = this , Changes = PointsChanged } ; set { if( value==pointes ) return ; pointes = value ; propertyChanged.On(this,"Pointes,Points") ; } } protected IList<Point> pointes ; // Parallel accessor of points . Can be made resistent if adequate updates are satisfied to make it relevant . 
		protected void PointsChanged( object subject , NotifyCollectionChangedEventArgs arg ) => Raw?.Edited(subject,arg) ;
		public int Index( string axe ) => IndexOf(this[axe]) ;
		public virtual Path Raw => Source?.Raw ;
		public Aspect Base => Raw?.Spectrum ;
		public bool Orphan => Source==null && Sources==null ;
		#region Operations
		public Axe.Support this[ IEnumerable<int> fragment ] => Axe.One[fragment] ;
		#endregion
		public struct Point : Quantable , IEnumerable<Quant?>
		{
			public interface Iterable : IEnumerable<Point> { int Count {get;} Aspectable Context {get;set;} Point this[ int at ] {get;} }
			readonly Aspect Context ; readonly int At ; Act.Point Raw => Matrix?[At] ; Path Matrix => Context?.Raw ;
			public Point( Aspect context , int at ) { Context = context ; At = at ; }
			public Quant? this[ uint key ] { get => ((Context as Path.Aspect)?[(int)key] is Path.Axe a?Matrix.Corrections?[a.Axis,At]:null) ?? Context[(int)key][At] ; set { if( (Context as Path.Aspect)?[(int)key] is Path.Axe a ) { if( (value??a[At]) is Quant v ) Matrix.Correction[a.Axis] = (At,v) ; } else throw new InvalidOperationException($"Can't set {key} axe of {Context} aspect !") ; } }
			public Quant? this[ string key ] { get => ((Context as Path.Aspect)?[key] is Path.Axe a?Matrix.Corrections?[a.Axis,At]:null) ?? Context[key][At] ; set { if( (Context as Path.Aspect)?[key] is Path.Axe a ) { if( (value??a[At]) is Quant v ) Matrix.Correction[a.Axis] = (At,v) ; } else throw new InvalidOperationException($"Can't set {key} axe of {Context} aspect !") ; } }
			public IEnumerator<Quant?> GetEnumerator() { var at = At ; return Context.Select(a=>a[at]).GetEnumerator() ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
			public Mark Mark { get => Raw?.Mark??Mark.No ; set { if( Raw is Act.Point raw ) raw.Mark = value ; } }
			public Mark? Marklet { get => Mark.nil() ; set => Mark = value??Mark.No ; }
			public string Tags { get => Raw?.Tags ; set { if( Raw is Act.Point raw ) raw.Tags = value ; } }
			public struct Iterator : Iterable
			{
				public Aspectable Context {get;set;}
				public int Count => Context?.Count>0 ? Context.Max(a=>a.Count) : Context?.Raw?.Count??0 ;
				public IEnumerator<Point> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
				public Point this[ int at ] => new Point(Context as Aspect,at) ;
			}
			public class Parit<Iter> : Aid.Collections.ObservableList<Point> , Iterable where Iter : Iterable , new()
			{
				public Aspectable Context { get => Source.Context ; set => Source = new Iter{ Context = value } ; } Iter Source ;
				public override IEnumerator<Point> GetEnumerator() { if( Count==0&&Source.Count>0 ) Task.Factory.StartNew(()=>{Source.Each(Add);if(Source.Count==Count)CollectionChanged+=Changes;}) ; return base.GetEnumerator() ; }
				public NotifyCollectionChangedEventHandler Changes ;
			}
			/// <summary>
			/// Accessor of binding .
			/// </summary>
			public object This { get => this ; set => (value as Action<object>)?.Invoke(this) ; }
		}
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		internal void OnChanged( NotifyCollectionChangedAction act , Axable item ) { Pointes = null ; collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(act,item)) ; Dirty = true ; }
		internal void OnChanged( object subject = null , PropertyChangedEventArgs item = null ) { Pointes = null ; collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)) ; Dirty = true ; }
		public new virtual void Add( Axe ax ) => Insert(Count,ax) ;
		public new virtual void Insert( int at , Axe ax ) { if( (uint)at<Count ) base.Insert(at,ax) ; else base.Add(ax) ; ax.Own = this ; ax.PropertyChanged += OnChanged ; OnChanged(NotifyCollectionChangedAction.Add,ax) ; }
		public new virtual void Remove( Axe ax ) { base.Remove(ax) ; ax.Own = null ; ax.PropertyChanged -= OnChanged ; OnChanged(NotifyCollectionChangedAction.Remove,ax) ; }
		void IList.Remove( object value ) => Remove( value as Axe ) ;
		int IList.Add( object value ) { Add( value as Axe ) ; return Count-1 ; }
		void ICollection<Axe>.Add( Axe axe ) => Add(axe) ;
		void IList.Insert( int at , object value ) => Insert(at,value as Axe) ;
		internal Quant Resistance( Quant? resi ) => Raw.Object==Basis.Device.Skierg.Code ? Basis.Device.Skierg.Draw : (Raw?.Resister).use(d=>resi??d)??0 ;
		internal Quant Gradient( Quant grad ) => Raw.Object==Basis.Device.Skierg.Code ? 0 : Path.Tolerancy.On(Raw?.Object)?.Grade is Quant v && v<Math.Abs(grad) ? -.01 : grad ;
		public override string ToString() => Score ;
		#region De/Serialization
		/// <summary>
		/// Is object in dirty state ? To be reserialized .
		/// </summary>
		public bool Dirty ;
		/// <summary>
		/// Deserializes aspect from string .
		/// </summary>
		public static explicit operator Aspect( string text ) => text.Get(t=>new Aspect(t.LeftFromLast(Serialization.Separator).Separate(Serialization.Separator,braces:null)?.Select(a=>(Axe)a),(Traits)t.RightFrom(Serialization.Separator,all:true).LeftFromLast(Serialization.Postseparator,all:true)){taglet=t.LeftFromLast(Serialization.Postseparator).RightFrom('\n').Null(v=>v.Void())}) ;
		/// <summary>
		/// Serializes aspect from string .
		/// </summary>
		public static explicit operator string( Aspect aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.Select(x=>(string)x))+(a.Count>0?Serialization.Separator:null)+(string)a.Trait+a.Taglet.Get(t=>$"{a.Taglet}{Serialization.Postseparator}")) ;
		protected static class Serialization { public const string Separator = " \x1 Axe \x2\n" , Postseparator = " \x1 Tag \x2\n"  ; }
		#endregion
		public Quant Offset { get => offset ; set { if( value!=offset ) propertyChanged.On(this,"Offset",offset=value) ; } } Quant offset ;
		public class Traitlet : INotifyPropertyChanged
		{
			public const string Extern = Axe.Extern ;
			internal Aspect Context ;
			public bool Orphan => Context?.Orphan!=false ;
			public bool Dirty { set => Context.Set(c=>c.Dirty=value) ; }
			public string Spec { get => name ; set => Changed("Spec",name=value) ; } string name ; public string Name => name.RightFrom(Extern,all:true) ;
			public string Bond { get => bond ; set => Changed("Bond",bond=value) ; } string bond ;
			public string Lex { get => lex ; set => Changed("Lex,Value",Resolver=(lex=value).Compile<Func<Contextable,Quant?>>()) ; } Func<Contextable,Quant?> Resolver ; string lex ;
			void Changed<Value>( string properties , Value value ) { propertyChanged.On(this,properties,value) ; Dirty = true ; }
			public Quant? Value { get { try { return Orphan ? null : Resolver?.Invoke(Context) ; } catch( System.Exception e ) { throw new InvalidOperationException($"Failed evaluating Trait {Spec} = {Lex} !",e) ; } } }
			public override string ToString() => Orphan ? null : $"{Spec.Null(n=>n.No()).Get(s=>s+'=')}{new Basis.Binding(Bond).Of(Value)}" ;
			public Traitlet() {} // Default constructor must be present to enable DataGrid implicit Add .
			internal Traitlet( Traitlet source , IEnumerable<Aspectable> set = null ) { var det = source?.Deref(set) ; name = (det??source)?.Spec ; bond = source?.Bond.Null(b=>b.No())??det?.Bond ; lex = (det??source)?.Lex ; Resolver = (det??source)?.Resolver ; Context = det?.Context ; }
			Traitlet Deref( IEnumerable<Aspectable> aspects ) => IsRef ? Spec.RightFrom(Extern,all:true).Get(s=>(Spec.LeftFromLast(Extern)is string asp?aspects?.Where(a=>asp==a.Spec):aspects)?.SelectMany(a=>a.Trait).One(x=>x.Spec==s&&!x.IsRef)) : null ;
			public bool IsRef => Resolver==null && lex.No() ;
			public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
			#region De/Serialization
			/// <summary>
			/// Deserializes aspect from string .
			/// </summary>
			public static explicit operator Traitlet( string text ) => text.Separate(Serialization.Separator,braces:null).Get(t=>new Traitlet{Spec=t.At(0),Lex=t.At(1),Bond=t.At(2)} ) ;
			/// <summary>
			/// Serializes aspect from string .
			/// </summary>
			public static explicit operator string( Traitlet trait ) => trait.Get(t=>string.Join(Serialization.Separator,t.name,t.lex,t.bond)) ;
			static class Serialization { public const string Separator = " \x1 Traitlet \x2 " ; }
			#endregion
		}
		public class Traits : Aid.Collections.ObservableList<Traitlet> , Aid.Gettable<Quant?> , ICollection<Traitlet> , IList , INotifyPropertyChanged
		{
			public bool Orphan => Context?.Orphan!=false ;
			public bool Dirty { get => Context?.Dirty==true ; set => Context.Set(c=>c.Dirty=value) ; }
			internal Aspect Context { get => context ; set { this.Each(t=>{if(t.Context==context)t.Context=value;}) ; context = value ; } } Aspect context ;
			public IEnumerable<Aspect> Contexts => this.Select(t=>t.Context).Distinct() ;
			public Quant? this[ string rek ] => this[rek,t=>rek.Contains(Traitlet.Extern)?t.Spec:t.Name]?.Value ;
			public void Add( Traitlet trait , IEnumerable<Aspectable> set = null ) => base.Add(trait.Set(t=>{if(set!=null||t.Context==null){t.Context=Context.Set(c=>c.Join(t.Context,set));Dirty=true;}t.PropertyChanged+=ChangedItem;Spec=null;})) ;
			public new void Add( IEnumerable<Traitlet> traits ) => traits.Each(Add) ;
			public static Traits operator+( Traits traits , Traitlet trait ) => traits.Set(t=>t.Add(trait)) ;
			public override bool Remove( Traitlet item ) => base.Remove(item).Set(r=>{if(item.Context==context){item.Context=null;Dirty=true;}item.PropertyChanged-=ChangedItem;Spec=null;}) ;
			void ICollection<Traitlet>.Add( Traitlet trait ) => Add(trait) ;
			int IList.Add( object item ) { Add((Traitlet)item) ; return Count-1 ; }
			void IList.Remove( object value ) => Remove( value as Traitlet ) ;
			public void Clean() => this.Where(t=>t.Context!=Context).ToArray().Each(t=>Remove(t)) ;
			public override string ToString() => Spec ;
			public string Spec { get => Orphan ? null : this.Stringy(',').Null(v=>v.No()) ; protected set { if( Propagate() ) propertyChanged.On(this,"Spec",Context.Set(c=>c.Score=value)) ; } }
			void ChangedItem( object subject , PropertyChangedEventArgs prop ) { Spec = null ; Context?.propertyChanged.On(Context,"Trait") ; }
			public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
			protected override void Refresh( IEnumerable<Traitlet> add = null, IEnumerable<Traitlet> rem = null ) { base.Refresh(add,rem) ; if( add==null && rem==null ) Spec = null ; }
			#region De/Serialization
			/// <summary>
			/// Deserializes aspect from string .
			/// </summary>
			public static explicit operator Traits( string text ) => text.Get(t=>t.LeftFromLast(Serialization.Separator).Separate(Serialization.Separator,braces:null)?.Select(e=>(Traitlet)e).Aggregate(new Traits(),(a,e)=>a+=e)) ;
			/// <summary>
			/// Serializes aspect from string .
			/// </summary>
			public static explicit operator string( Traits traits ) => traits.Null(t=>t.Count<=0)?.Where(t=>t.Context==traits.Context).Get(a=>string.Join(Serialization.Separator,a.Select(x=>(string)x))).Null(v=>v.No()).Get(v=>v+Serialization.Separator) ;
			static class Serialization { public const string Separator = " \x1 Trait \x2\n" ; }
			#endregion
		}
	}
	public partial class Path
	{
		public class Aspect : Act.Aspect
		{
			public static string Filex = "spt" ;
			internal Path Context { get => context ; set { context = value ; foreach( var ax in this ) if( ax is Axe a ) a.Context = value ; OnChanged() ; } } Path context ;
			public Aspect( Path path )
			{
				Context = path ; Add(new Axe(Context){Axis=Axis.Date}) ; Add(new Axe(Context){Axis=Axis.Time}) ;
				for( uint ax = 0 ; ax<Context.Dimensions ; ++ax ) if( Context[ax]!=null ) Add(new Axe(Context){Ax=ax}) ;
				foreach( var mark in Basis.Marks ) if( Context[mark]!=null ) Add(new Axe(Context){Mark=mark}) ;
				this.Each(a=>a.Quantizer=null) ;
			}
			public override string Spec { get => Context.Spec ; set => base.Spec = value ; }
			public override Aspectable Source { set {} }
			public Axable this[ Axis ax , bool insure = false ] => this.OfType<Axe>().FirstOrDefault(a=>a.Axis==ax) ??( insure ? new Axe(Context){Axis=ax}.Set(Add) : Axe.No as Axable ) ;
			public override void Add( Act.Axe ax ) => base.Add(ax.Set(a=>{if(!(a is Axe))a.Aspect=this;})) ;
			public override void Remove( Act.Axe ax ) => base.Remove(ax.Set(a=>{if(!(a is Axe))a.Aspect=null;})) ;
			public void Reform( params string[] binds ) => this.OfType<Axe>().Each(a=>a.Binder=binds.At((int)a.Axis)??Context.Metaxes?[a.Ax].Form) ;
			#region Operation
			public Act.Axe perf( Lap lap ) => perf(pace(lap),grad(lap),resi(lap),flow(lap),gran(lap)) ;
			public Act.Axe velo( Lap lap ) => lap.quo(this[Axis.Dist,false]as Axe,this[Axis.Time,false]as Axe) ;
			public Act.Axe pace( Lap lap ) => lap.quo(this[Axis.Time,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe grad( Lap lap ) => lap.quo(this[Axis.Alt,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe gran( Lap lap ) => lap.quo(this[Axis.Grade,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe flow( Lap lap ) => Raw.Object==Basis.Device.Skierg.Code ? Axe.No : lap.quo(this[Axis.Flow,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe resi( Lap lap ) => lap.quo(this[Axis.Drag,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe perf( int lap = 0 ) => perf(pace(lap),grad(lap),resi(lap),flow(lap),gran(lap)) ;
			public Act.Axe velo( int lap = 0 ) => lap.quo(this[Axis.Dist,false]as Axe,this[Axis.Time,false]as Axe) ;
			public Act.Axe pace( int lap = 0 ) => lap.quo(this[Axis.Time,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe grad( int lap = 0 ) => lap.quo(this[Axis.Alt,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe gran( int lap = 0 ) => lap.quo(this[Axis.Grade,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe flow( int lap = 0 ) => Raw.Object==Basis.Device.Skierg.Code ? Axe.No : lap.quo(this[Axis.Flow,false]as Axe,this[Axis.Dist,false]as Axe) ;
			public Act.Axe resi( int lap = 0 ) => lap.quo(this[Axis.Drag,false]as Axe,this[Axis.Dist,false]as Axe) ;
			Act.Axe perf( Act.Axe pace , Act.Axe grad = null , Act.Axe resi = null , Act.Axe flow = null , Act.Axe gran = null ) => Context.Get( c => new Act.Axe( i => pace[i].PacePower(Gradient(grad?[i]??0),Resistance(resi?[i]),flow?[i]??0,gran?[i]??0) , pace ) ) ?? Axe.No ;
			#endregion
			public override Point.Iterable Points => new Iterator{ Context = this } ;
			public override IList<Point> Pointes => pointes ??= new Point.Parit<Iterator>{ Context = this , Changes = PointsChanged } ;
			public override Path Raw => Context ;
			public class Iterator : Point.Iterable
			{
				public Aspectable Context {get;set;}
				public int Count => Context?.Raw?.Count ?? 0 ; //Math.Max((Context as Aspect)?.Context.Count??0,Context.Where(a=>!(a is Axe)&&a.Counts).Null(e=>e.Count()<=0)?.Max(a=>a.Count)??0) ;
				public IEnumerator<Point> GetEnumerator() { for( int i=0 , count=Count ; i<count ; ++i ) yield return this[i] ; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
				public Point this[ int at ] => new Point(Context as Aspect,at) ;
			}
			#region De/Serialization
			public static explicit operator string( Aspect aspect ) => aspect.Get(a=>string.Join(Serialization.Separator,a.Where(x=>!(x is Axe)).Select(x=>(string)x))+(a.Count(x=>!(x is Axe))>0?Serialization.Separator:null)+(string)a.Trait) ;
			#endregion
		}
		public Aspect Spectrum { get => aspect ??= new Aspect(this) ; internal set => aspect = value.Set(s=>s.Context=this) ; } Aspect aspect ;
		public string Origin { get => Spectrum.Origin ; set { Spectrum.Origin = value ; var asp = (Act.Aspect)System.IO.Path.ChangeExtension(value,Aspect.Filex).ReadAllText() ; if( asp==null ) return ; foreach( var ax in asp ) if( Spectrum[ax.Spec]==null ) Spectrum.Add(ax) ; foreach( var trait in asp.Trait ) Spectrum.Trait.Add(trait) ; } }
	}
}
