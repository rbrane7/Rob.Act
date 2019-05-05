using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid;
using Aid.Extension;

namespace Rob.Act
{
	using Configer = System.Configuration.ConfigurationManager ;
	using Quant = Double ;
	public partial class Path : Point , IList<Point> , Gettable<DateTime,Point>
	{
		public static bool Dominancy = Configer.AppSettings["Path.Dominancy"]!=null ;
		public static double Margin = Configer.AppSettings["Path.Margin"].Parse<double>()??0 ;
		public static Dictionary<string,Quant?[]> Meta = new Dictionary<string,Quant?[]>{ ["Tabata"]=new Quant?[]{1,2} } ;

		#region Construct
		public Path( DateTime date , IEnumerable<Point> points = null ) : base(date)
		{
			points.Set(p=>Content.AddRange(p.OrderBy(t=>t.Date))) ;
			date = DateTime.Now ; for( var i=0 ; i<Count ; ++i )
			{
				if( i<=0 || this[i-1].Mark.HasFlag(Mark.Stop) ) { date = this[i].Date ; if( this[i].Dist==null ) this[i].Dist = 0 ; }
				if( this[i].Time==TimeSpan.Zero ) this[i].Time = this[i].Date-date ;
				if( this[i].Dist==null && this[i].IsGeo ) this[i].Dist = this[i-1].Dist + (this[i]-this[i-1]).Euclid(this[i-1]) ;
			}
			this.At(Count-1).Set(p=>{Time=p.Time;Dist=p.Dist;}) ;
		}
		public Path( DateTime time , bool close , IEnumerable<Point> points = null ) : this(time,points)
		{
			if( close )
			{
				if( this[0]?.Mark.HasFlag(Mark.Stop)==false ) Content.Insert(0,new Point(Content[0]){Mark=Mark.Stop}) ;
				if( Beat==null ) { this[0].Beat = 0 ; for( var i=1 ; i<Count ; ++i ) this[i].Beat = this[i-1].Beat+this[i].Beat/60*(this[i].Time-this[i-1].Time).TotalSeconds ; Beat = this[Count-1].Beat ; }
			}
		}
		#endregion

		#region State
		int Depth = 1 ; public bool Dominant = Dominancy ;
		List<Point> Content = new List<Point>() ;
		/// <summary>
		/// Tags for path recognition among the others in a book .
		/// </summary>
		public List<string> Tags => tags ?? System.Threading.Interlocked.CompareExchange(ref tags,new List<string>(),null) ?? tags ; List<string> tags ;
		public string Tag { get => tag ?? ( tag = tags.Null(t=>t.Count<=0).Stringy(' ') ) ; set { if( value==tag ) return ; tags?.Clear() ; tag = null ; Tags.AddRange(value.SeparateTrim(' ')) ; propertyChanged.On(this,"Tag,Spec") ; } } string tag ;
		public override string Spec { get => Tag is string t ? $"{base.Spec} {t}" : base.Spec ; set { if( value==base.Spec ) return ; base.Spec = value ; aspect.Set(a=>a.Spec=value) ; } }
		#endregion

		#region Trait
		public Quant? MaxEffort => (Count-1).Steps().Max(i=>Content[i+1].Effort-Content[i].Effort) ;
		public Quant? MinEffort => (Count-1).Steps().Select(i=>Content[i+1].Effort-Content[i].Effort).Skip(5).ToArray().Get(a=>(a.Length-2).Steps(1).Min(i=>9.Steps(1).All(j=>i-j>=0&&a[i-j]>=a[i]&&i+j<a.Length&&a[i]<=a[i+j])?a[i]:null)) ;
		public Quant? MinMaxEffort => (Count-1).Steps().Select(i=>Content[i+1].Effort-Content[i].Effort).Skip(5).ToArray().Get(a=>(a.Length-2).Steps(1).Min(i=>9.Steps(1).All(j=>i-j>=0&&a[i-j]<=a[i]&&i+j<a.Length&&a[i]>=a[i+j])?a[i]:null)) ;
		public Quant? AeroEffort { get { var min = MinEffort ; var max = MinMaxEffort ; var mav = (Count-1).Steps().Count(i=>Content[i+1].Effort-Content[i].Effort>=max*0.9) ; var miv = (Count-1).Steps().Count(i=>Content[i+1].Effort-Content[i].Effort<=min*1.2) ; return (min*miv+max*mav)/(miv+mav)*Durability ; } } // => (Meta.By(Action).At(0)*MinEffort+Meta.By(Action).At(1)*MinMaxEffort)/(Meta.By(Action).At(0)+Meta.By(Action).At(1)) ;
		public Quant? MaxBeat => (Count-1).Steps().Max(i=>(Content[i+1].Beat-Content[i].Beat).Quotient((Content[i+1].Time-Content[i].Time).TotalSeconds)) ;
		public Quant? MaxExposure => MaxEffort/MaxBeat ;
		public string MaxExposion => $"{MaxEffort}W/{MaxBeat.use(v=>Math.Round(v*60))}`b={MaxExposure.use(Math.Round)}bW" ;
		public Quant Durability => Math.Max(0,1.1-20/Time.TotalSeconds) ;
		#endregion

		#region Access
		public void Add( Point item ) { var idx = IndexOf(item.Date) ; if( this[idx]?.Date==item.Date ) Content[idx] |= item ; else Content.Insert( idx , item.Set(i=>{if(idx<Count&&i.Date>Date)i.Mark=0;} ) | Vicinity(idx) ) ; while( idx>0 && !this[idx-1].Mark.HasFlag(Mark.Stop) ) --idx ; item.Time = item.Date-this[idx].Date ; }
		public Point this[ DateTime time ] => time.Give( Vicinity(time) ) ;
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
		public static Path operator/( Path path , Axis axis ) => path.Set(p=>p.Each(i=>i/=axis)) ;
		public static Path operator/( Path path , string axis ) => path / axis.Axis() ;
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

		public override string ToString() => $"{Action} {Sign} {Exposion} \\ {MaxExposion} / {MinEffort}W\\{MinMaxEffort}W={AeroEffort:#}W {Trace} {Tag}" ;

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
		public IEnumerator<Point> GetEnumerator() => Content.GetEnumerator() ;
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		public int Count => Content.Count ;
		public bool IsReadOnly => false ;
		public Point this[ int index ] { get => Content.At(index) ; set => throw new NotSupportedException("Path can't be directly inserted or repaced to .") ; }
		#endregion
	}
}
