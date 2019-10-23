﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	using Configer = System.Configuration.ConfigurationManager ;
	public struct Profile { public Quant? Mass ; }
	public partial class Path : Point , IList<Point> , Gettable<DateTime,Point> , INotifyCollectionChanged , Pathable
	{
		public static bool Dominancy = Configer.AppSettings["Path.Dominancy"]!=null ;
		public static double Margin = Configer.AppSettings["Path.Margin"].Parse<double>()??0 ;
		public static Dictionary<string,Quant?[]> Meta = new Dictionary<string,Quant?[]>{ ["Tabata"]=new Quant?[]{1,2} } ;
		public static IDictionary<string,Quant> GradeTolerancy = new Dictionary<string,Quant>{ ["Polling"]=.25 , ["ROLLER_SKIING"]=.27 } ;
		public static IDictionary<string,Quant> DeviaTolerancy = new Dictionary<string,Quant>{ ["Polling"]=.25 , ["ROLLER_SKIING"]=3 } ;
		public static IDictionary<string,Profile> SubjectProfile = new Dictionary<string,Profile>{ ["Rob"]=new Profile{Mass=76} } ;

		#region Construct
		public Path( DateTime date , IEnumerable<Point> points = null ) : base(date) { Borrow(points) ; Impose() ; }
		void Impose()
		{
			if( Alti==null ) Alti = this.Average(p=>p.Alti) ; if( this[Axis.Lon]==null ) this[Axis.Lon] = this.Average(p=>p[Axis.Lon]) ; if( this[Axis.Lat]==null ) this[Axis.Lat] = this.Average(p=>p[Axis.Lat]) ;
			var date = DateTime.Now ; for( var i=0 ; i<Count ; ++i )
			{
				if( this[i].Metax==null ) Metax.Set(m=>this[i].Metax=m) ;
				if( i<=0 || this[i-1].Mark.HasFlag(Mark.Stop) )
				{
					date = this[i].Date-(this[i-1]?.Time??default) ;
					if( this[i].Dist==null ) this[i].Dist = this[i-1]?.Dist??0 ;
					if( this[i].Asc==null && Alti!=null ) this[i].Asc = this[i-1]?.Asc??0 ;
					if( this[i].Dev==null && IsGeo ) this[i].Dev = this[i-1]?.Dev??0 ;
					if( this[i].Alti==null && Alti!=null ) this[i].Alti = ((Count-i).Steps(i).FirstOrDefault(j=>this[j].Alti!=null).nil()??i-1).Get(j=>this[j].Alti) ;
				}
				if( this[i].Time==default ) this[i].Time = this[i].Date-date ;
				if( this[i].Bit==null ) this[i].Bit = i ;
				if( this[i].IsGeo )
				{
					if( this[i].Dist==null ) this[i].Dist = this[i-1].Dist + (this[i]-this[i-1]).Euclid(this[i-1]) ;
					if( Alti!=null )
					{
						if( this[i].Alti==null ) this[i].Alti = this[i-1].Alti + ((Count-i).Steps(i).FirstOrDefault(j=>this[j].Alti!=null).nil()??i-1).Get(j=>(this[j].Alti-this[i-1].Alti)/j) ;
						if( this[i].Asc==null ) this[i].Asc = this[i-1].Asc + ( this[i].Alti-this[i-1].Alti is Quant u && Math.Abs(u)/(this[i].Dist-this[i-1].Dist)<(GradeTolerancy.On(Action)??.3) ? u : 0 ) ;
					}
					if( this[i].Dev==null ) this[i].Dev = this[i-1].Dev + ( i<Count-1 && !this[i].Mark.HasFlag(Mark.Stop) && (this[i].Geo-this[i-1].Geo).Devia(this[i+1].Geo-this[i].Geo) is Quant v ? v : 0 ) ;
				}
			}
			this[Count-1].Set(p=>{if(Bit==null)Bit=p.Bit;if(Time==default)Time=p.Time;if(Dist==null)Dist=p.Dist;if(Asc==null)Asc=p.Asc;if(Dev==null)Dev=p.Dev;}) ;
		}
		void Depose() { for( var i=0 ; i<Count ; ++i ) { this[i].Time = default ; this[i].Bit = this[i].Dist = null ; this[i].Asc = this[i].Dev = null ; } }
		public void Reset() { Depose() ; Impose() ; }
		public Path( DateTime time , bool close , IEnumerable<Point> points = null ) : this(time,points) { if( close ) { if( Beat==null ) { this[0].Beat = 0 ; Quant lb = 0 ; for( var i=1 ; i<Count ; ++i ) this[i].Beat = ((this[i-1].Beat??lb)+this[i].Beat/60*(this[i].Time-this[i-1].Time).TotalSeconds).use(b=>lb=b) ; Beat = lb ; } } }
		protected virtual void Adopt( Path path )
		{
			Depose() ; base.Adopt(path) ;
			if( Count>path.Count ) Content.RemoveRange(path.Count,Count-path.Count) ; for( var i=0 ; i<Count ; ++i ) this[i].Adopt(path[i]) ; if( Count<path.Count ) Content.AddRange(path.Content.Skip(Count)) ;
			Impose() ; propertyChanged.On(this,"Spec,Spectrum") ; collectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)) ;
		}
		void Pointable.Adopt( Pointable path ) => (path as Path).Set(Adopt) ;
		public void Populate() { Metax.Reset(Spectrum.Trait) ; Spectrum.Trait.Each(t=>this[t.Spec]=t.Value) ; Spectrum.Tags.Set(Tag.Add) ; }
		void Borrow( IEnumerable<Point> points ) { points.Set(p=>Content.AddRange(p.OrderBy(t=>t.Date))) ; if( Metax==null ) Metax = points?.FirstOrDefault(p=>p.Metax!=null)?.Metax ; }
		internal Path On( IEnumerable<Point> points )
		{
			Borrow(points) ;
			for( var ax = Axis.Lon ; ax<=Axis.Alt ; ++ax ) this[ax] = this.Average(p=>p[ax]) ; for( var ax = Axis.Dist ; ax<=Axis.Time ; ++ax ) this[ax] = this.Sum(p=>p[ax]) ;
			Asc = this.Sum(p=>(Quant?)p.Asc) ; Dev = this.Sum(p=>(Quant?)p.Dev) ; if( Metax!=null ) foreach( var ax in Metax ) this[ax.Value.At] = this.Average(p=>p[ax.Value.At]) ;
			for( int i=0 , c=this.Min(p=>p.Tag.Count) ; i<c ; ++i ) Tag[i] = this.Where(p=>p.Tags!=null).Aggregate(string.Empty,(a,p)=>a==null?null:a==string.Empty?p.Tag[i]:a==p.Tag[i]||p.Tag[i].No()?a:null) ;
			return this ;
		}
		#endregion

		#region State
		int Depth = 1 ;
		readonly List<Point> Content = new List<Point>() ;
		public bool Dominant = Dominancy ;
		protected override void SpecChanged( string value ) { base.SpecChanged(value) ; aspect.Set(a=>a.Spec=value) ; }
		public Profile? Profile => SubjectProfile.On(Subject) ;
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => collectionChanged += value.DispatchResolve() ; remove => collectionChanged -= value.DispatchResolve() ; } NotifyCollectionChangedEventHandler collectionChanged ;
		#endregion

		#region Trait
		public Quant? MaxPower => MaxEffort is Quant x && MaxPerform.Nil(e=>e>x*1.1) is Quant y ? Math.Max(x,y) : MaxEffort??MaxPerform ;
		public Quant? MaxEffort => (Count-1).Steps().Max(i=>(Content[i+1].Effort-Content[i].Effort)/(Content[i+1].Bit-Content[i].Bit)) ;
		public Quant? MaxPerform => (Count-1).Steps().Max(i=>(Content[i+1].Energy-Content[i].Energy)/(Content[i+1].Time-Content[i].Time).TotalSeconds) ;
		public Quant? MinEffort => (Count-1).Steps().Select(i=>Content[i+1].Effort-Content[i].Effort).Skip(5).ToArray().Get(a=>(a.Length-2).Steps(1).Min(i=>9.Steps(1).All(j=>i-j>=0&&a[i-j]>=a[i]&&i+j<a.Length&&a[i]<=a[i+j])?a[i]:null)) ;
		public Quant? MinMaxEffort => (Count-1).Steps().Select(i=>Content[i+1].Effort-Content[i].Effort).Skip(5).ToArray().Get(a=>(a.Length-2).Steps(1).Min(i=>9.Steps(1).All(j=>i-j>=0&&a[i-j]<=a[i]&&i+j<a.Length&&a[i]>=a[i+j])?a[i]:null)) ;
		public Quant? AeroEffort { get { var min = MinEffort ; var max = MinMaxEffort ; var mav = (Count-1).Steps().Count(i=>Content[i+1].Effort-Content[i].Effort>=max*0.9) ; var miv = (Count-1).Steps().Count(i=>Content[i+1].Effort-Content[i].Effort<=min*1.2) ; return (min*miv+max*mav)/(miv+mav)*Durability ; } } // => (Meta.By(Action).At(0)*MinEffort+Meta.By(Action).At(1)*MinMaxEffort)/(Meta.By(Action).At(0)+Meta.By(Action).At(1)) ;
		public Quant? MaxBeat => (Count-1).Steps().Max(i=>(Content[i+1].Beat-Content[i].Beat).Quotient((Content[i+1].Time-Content[i].Time).TotalSeconds)) ;
		public Quant? MaxExposure => MaxEffort/MaxBeat ;
		public string MaxExposion => "{0}={1}".Comb("{0}/{1}".Comb(MaxEffort.Get(e=>$"{e}W"),MaxBeat.Get(v=>$"{Math.Round(v*60)}`b")),MaxExposure.Get(e=>$"{Math.Round(e)}bW")) ;
		Quant Durability => Math.Max(0,1.1-20/Time.TotalSeconds) ;
		public Quant? Shift => ((Spectrum[Axis.Energy] as Axe)^(Spectrum[Axis.Beat] as Axe))?.LastOrDefault() is Quant v ? Math.Log(v) : null as Quant? ;
		public Quant? MaxShift => ((Spectrum[Axis.Energy] as Axe)^(Spectrum[Axis.Beat] as Axe)).Skip(150)?.Max() is Quant v ? Math.Log(v) : null as Quant? ;
		#endregion

		#region Access
		public void Add( Point item ) { var idx = IndexOf(item.Date) ; if( this[idx]?.Date==item.Date ) Content[idx] |= item ; else Content.Insert( idx , item.Set(i=>{if(idx<Count&&i.Date>Date)i.Mark=0;} ) | Vicinity(idx) ) ; while( idx>0 && !this[idx-1].Mark.HasFlag(Mark.Stop) ) --idx ; item.Time = item.Date-this[idx].Date ; }
		public Point this[ DateTime time ] => time.Give( Vicinity(time) ) ;
		Pointable Gettable<DateTime,Pointable>.this[ DateTime date ] => this[date] ;
		public int IndexOf( DateTime time ) => this.IndexWhere(p=>p.Date>=time).nil(i=>i<0) ?? Count ;
		public IEnumerable<Point> Vicinity( DateTime time ) => Vicinity(IndexOf(time)) ;
		public IEnumerable<Point> Vicinity( int index ) => this.Skip(index-Depth).Take(Depth<<1) ; //todo: solve stops
		#endregion

		#region Operation
		public static IEnumerable<Path> operator/( Path path , Mark kind ) { var seg = new Path(path.Date) ; foreach( var point in path ) { seg.Content.Add(point) ; if( point.Mark.HasFlag(kind) ) { yield return seg ; seg = new Path(point.Date) ; } } if( seg.Count>0 ) yield return seg ; }
		public static Path operator|( Path path , Point point ) => path.Set(p=>p.Add(point)) ;
		public static Path operator|( Path prime , IEnumerable<Point> second ) => prime.Set(pri=>second.Each(pri.Add)) ;
		public static Path operator|( Path prime , Path second ) => prime.Set( w => { if( w.Dominant ) w.Each(p=>p|=second[p.Date]) ; else w |= second as IEnumerable<Point> ; if( w[0]?.Date<w.Date ) w.Date = w[0].Date ; } ) ?? second ;
		public static Path operator&( Path path , Path lead ) => path.Set(p=>p.Rely(lead)) ;
		public static Path operator/( Path path , uint axis ) => path.Set(p=>p.Each(i=>i/=axis)) ;
		public static Path operator/( Path path , Axis axis ) => path / (uint)axis ;
		public static Path operator/( Path path , string axis ) => path / axis.Axis(path?.Dimension??default) ;
		public static Path operator>>( Path path , int depth ) { if( path!=null ) while( depth-->0 ) path = new Path( path.Date , path.Diff ) ; return ++path ; }
		public static Path operator<<( Path path , int depth ) { if( path!=null ) while( depth-->0 ) path = new Path( path.Date , path.Inte ) ; return ++path ; }
		public static Path operator--( Path path ) => path - true ;
		public static Path operator*( Path path , Oper oper ) => ( path % oper.HasFlag(Oper.Smooth) - oper.HasFlag(Oper.Trim) ) / oper.HasFlag(Oper.Relat) ;
		public static Point operator+( Path path ) => path?.Aggregate(Zero(path.Date),(a,p)=>a+=p) ;
		public static Path operator++( Path path ) => path.Set( w => w.From(+w) ) ;
		public static Path operator-( Path path , bool trim ) => trim ? path.Set(p=>p.Trim()) : path ;
		public static Path operator%( Path path , bool smooth ) => smooth ? path.Set(p=>p.Smooth()) : path ;
		public static Path operator/( Path path , bool reval ) => reval ? path.Set(p=>p.Relat()) : path ;
		#endregion

		#region Calculus
		public IEnumerable<Point> Diff { get { for( var i=1 ; i<Count ; ++i ) if( !this[i-1].Mark.HasFlag(Mark.Stop) ) yield return (this[i]-this[i-1]).Set(d=>d.Mark=this[i].Mark.HasFlag(Mark.Stop)?Mark.Stop:Mark.No) ; } }
		public IEnumerable<Point> Inte { get { Point point = null ; for( var i=0 ; i<Count ; ++i ) yield return point = new Point(this[i]) + point ; } }
		#endregion

		public override string ToString() => $"{Action} {Sign} {Distance/1000:0.00}km {Exposion} {"\\ {0} /".Comb(MaxExposion)} {MinEffort.Get(e=>$"{e}W\\")}{MinMaxEffort.Get(e=>$"{e}W")}:{AeroEffort.Get(a=>$"{a:#W}")} {Trace} {Tags}" ;
		public override string Sign => Dominant ? Date.ToString() : base.Sign ;

		#region Implementation
		public void Rely( Path lead )
		{
			if( lead?.Count>0 && Count>0 )
			{
				var i = IndexOf(lead[0].Date) ; if( this.At(i)?.Date==lead[0].Date && i>0 ) --i ; Content.RemoveRange(0,i) ; i = IndexOf(lead[lead.Count-1].Date) ; if( this.At(i)?.Date==lead[lead.Count-1].Date && i<Count ) ++i ; Content.RemoveRange(i,Count-i) ;
				for( i=0 ; i<lead.Count-1 ; ++i ) if( lead[i].Mark.HasFlag(Mark.Stop) ) { var j = IndexOf(lead[i].Date) ; var k = IndexOf(lead[i+1].Date) ; if( this.At(k)?.Date==lead[i+1].Date && k<Count ) ++k ; if( this.At(j)?.Date==lead[i].Date && j<k ) ++i ; Content.RemoveRange(j,k-j) ; }
			}
		}
		public void Trim()
		{
			var dif = this>>1 ;
			var on = true ; Quant? dst = 0 ; for( int i=0,j=0 ; j<dif.Count-1 ; ++i,++j )
			{
				var po = this[i] ; if( on ) if( !this[i].Mark.HasFlag(Mark.Stop) && dst<Margin && dif[j].Speed.use(s=>s<dif.Speed)==true ) { RemoveAt(i--) ; dst += dif[j].Dist ; dif.RemoveAt(j--) ; } else { on = false ; dst = 0 ; }
				if( !on && ( on = po.Mark.HasFlag(Mark.Stop) ) ) --j ;
			}
			on = true ; dst = 0 ; for( int i=Count-1,j=dif.Count-1 ; j>1 ; --i,--j )
			{
				if( on ) if( !this[i-1].Mark.HasFlag(Mark.Stop) && dst<Margin && dif[j].Speed.use(s=>s<dif.Speed)==true ) { this[i-1].Mark |= this[i].Mark ; RemoveAt(i) ; dst += dif[j].Dist ; dif.RemoveAt(j) ; } else { on = false ; dst = 0 ; }
				if( !on && ( on = this[i-1].Mark.HasFlag(Mark.Stop) ) ) ++j ;
			}
		}
		public void Smooth()
		{
			var dif = this>>1 ;
#if false	// single run through
			for( int i=1,j=0 ; j<dif.Count ; ++i,++j ) { if( this[i-1].Mark.HasFlag(Mark.Stop) ) ++i ; if( this[i].Mark.HasFlag(Mark.Stop) ) continue ; dif.Vibre(j).Use( q =>{ var t = dif[j].Time ; dif[j].Time = new TimeSpan((long)(dif[j].Time.Ticks*q)) ; if( j+1<dif.Count ) dif[j+1].Time += t-dif[j].Time ; } ) ; }
#elif false	// double run througd
			var vibre = dif.Count.Steps().Select(j=>dif.Vibre(j)).ToArray() ;
			for( int i=1,j=0 ; j<dif.Count ; ++i,++j ) { if( this[i-1].Mark.HasFlag(Mark.Stop) ) ++i ; if( this[i].Mark.HasFlag(Mark.Stop) ) continue ; vibre[j].Use( q =>{ var t = dif[j].Time ; dif[j].Time = new TimeSpan((long)(dif[j].Time.Ticks*q)) ; /*if( j+1<dif.Count ) dif[j+1].Time += t-dif[j].Time ;*/ } ) ; }
#else		// double run througd
			var veloa = dif.Count.Steps().Select(j=>dif.Veloa(j)).ToArray() ;
			for( int i=1,j=0 ; j<dif.Count ; ++i,++j ) { if( this[i-1].Mark.HasFlag(Mark.Stop) ) ++i ; if( this[i].Mark.HasFlag(Mark.Stop) ) continue ; veloa[j].Use( v =>(dif[j].Dist/v).Use( s=> dif[j].Time = TimeSpan.FromSeconds(s) ) ) ; }
#endif
			for( int i=1,j=0 ; j<dif.Count ; ++i,++j ) { if( this[i-1].Mark.HasFlag(Mark.Stop) ) ++i ; if( this[i].Mark.HasFlag(Mark.Stop) ) continue ; this[i].Time = this[i-1].Time+dif[j].Time ; this[i].Date = this[i-1].Date+dif[j].Time ; }
		}
		public void Relat()
		{
			var dif = this>>1 ; for( int i=1,j=0 ; j<dif.Count ; ++i,++j )
			{
				if( this[i-1].Mark.HasFlag(Mark.Stop) ) { if( this[i-1].Date > this[i].Date ) this[i].Date = this[i-1].Date ; ++i ; }
				dif.Grade(j,this[i]).Use( g => dif[j].Time = new TimeSpan((long)( dif[j].Time.Ticks / Math.Exp(Basis.Gravity*g) )) ) ; this[i].Date = this[i-1].Date+dif[j].Time ; this[i].Time = this[i-1].Time+dif[j].Time ;
			}
		}
		#endregion

		#region Redirects
		public int IndexOf( Point item ) => Content.IndexOf(item) ;
		public void Insert( int index , Point item ) => throw new NotSupportedException("Path can't be directly inserted or repaced to .") ;
		public void RemoveAt( int index ) => Content.RemoveAt(index) ;
		public void Clear() => Content.Clear() ;
		public bool Contains( Point item ) => Content.Contains(item) ;
		public void CopyTo( Point[] array , int arrayIndex ) => Content.CopyTo(array,arrayIndex) ;
		public bool Remove( Point item ) => Content.Remove(item) ;
		public IEnumerator<Point> GetEnumerator() => Content.GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public int Count => Content.Count ;
		public bool IsReadOnly => false ;
		public Point this[ int index ] { get => Content.At(index) ; set => throw new NotSupportedException("Path can't be directly inserted or repaced to .") ; }
		Pointable Gettable<int,Pointable>.this[ int index ] => this[index] ;
		#endregion

		#region De/Serialization
		Path( string text ) : base(text.LeftFrom(Serialization.Separator,all:true).Get(t=>t.StartsBy(Serialization.Domimator)?t.RightFromFirst(Serialization.Domimator):t))
		{
			text.RightFromFirst(Serialization.Separator).Separate(Serialization.Separator,false).Set(e=>Content.AddRange(e.Select(p=>((Point)p).Set(a=>{if(a.Metax==null)a.Metax=Metax;})))) ; if( Dominant = text.StartsBy(Serialization.Domimator) );else return ;
			for( var ax=Axis.Dist ; ax<=Axis.Time ; ++ax ) { Quant lval = 0 ; for( var i=0 ; i<Count ; ++i ) { this[i][ax] += lval ; this[i][ax].Use(v=>lval=v) ; } }
		}
		public static explicit operator string( Path path ) => path.Get(a=>$"{(path.Dominant?Serialization.Domimator:null)}{(string)(a as Point)}{(string)a.Metax}{Serialization.Separator}{(string.Join(null,a.Content.Select(p=>(string)p+(string)p.Metax.Null(m=>m==a.Metax)+Serialization.Separator)))}") ;
		public static explicit operator Path( string text ) => text.Null(v=>v.No()).Get(t=>new Path(t)) ;
		new internal static class Serialization { public const string Separator = " \x1 Point \x2\n" ; public const string Domimator = "^ " ; }
		#endregion

		public override Metax Metax { set { if( Dominant && Metax!=null ) Metax.Basis = value ; else base.Metax = value ; } }

		#region Comparation
		public virtual bool Equals( Pathable path ) => path is Path p && (this as Point).Equals(p) && Content.SequenceEquate(p.Content,(x,y)=>x.EqualsRestricted(y)) && Metax?.Equals(p.Metax)!=false ;
		#endregion
	}
}
