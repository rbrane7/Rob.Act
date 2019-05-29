using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Aid.Extension;
using Aid;
using System.ComponentModel;

namespace Rob.Act
{
	using Quant = Double ;
	public class Point : DynamicObject , Accessible<uint,Quant?> , Accessible<Axis,Quant?> , Accessible<Quant?> , INotifyPropertyChanged
	{
		#region Construction
		public Point( DateTime date ) => Date = date ;
		public Point( Point point ) { Date = point.Date ; Quantity = point.Quantity.ToArray() ; Mark = point.Mark ; Spec = point.Spec ; Action = point.Action ; }
		#endregion

		#region Setup
		public void From( Point point ) { Time = point.Time ; for( uint i=0 ; i<point.Dimension ; ++i ) this[i] = point[i] ; }
		public void Adopt( Point point ) { From(point) ; Date = point.Date ; Action = point.Action ; Mark = point.Mark ; }
		#endregion

		#region State
		/// <summary>
		/// Referential date of object .
		/// </summary>
		public DateTime Date { get => date ; set { if( date==value ) return ; date = value ; sign = null ; } } DateTime date ;
		/// <summary>
		/// Quanitity data vector .
		/// </summary>
		Quant?[] Quantity = new Quant?[(int)Axis.Time] ;
		/// <summary>
		/// Relative time of object .
		/// </summary>
		public TimeSpan Time { get => time ; set { if( time==value ) return ; time = value ; sign = null ; } } TimeSpan time ;
		/// <summary>
		/// Signature of the point .
		/// </summary>
		public string Sign => sign ?? ( sign = $"{Date}{Time.nil(t=>t==TimeSpan.Zero).Get(t=>$"+{t:hh\\:mm\\:ss}")}" ) ; string sign ;
		/// <summary>
		/// Assotiative text .
		/// </summary>
		public virtual string Spec { get => spec ?? ( spec = $"{Action} {Sign}" ) ; set { if( value==spec ) return ; spec = value ; propertyChanged.On(this,"Spec") ; } } string spec ;
		/// <summary>
		/// Action specification .
		/// </summary>
		public string Action { get => action ; set { if( action==value ) return ; var a = action ; action = value ; if( spec==$"{a} {Sign}" ) Spec = null ; } } string action ;
		/// <summary>
		/// Kind of demarkaition .
		/// </summary>
		public Mark Mark { get; set; } public Mark? Marklet => Mark.nil() ;
		/// <summary>
		/// Ascent of the path .
		/// </summary>
		public Bipole? Asc { get; set; }
		#endregion

		#region Vector
		public static Point Zero( DateTime date ) => new Point(date){ Time = TimeSpan.Zero }.Set( p=>{ for( var i=0 ; i<p.Dimension ; ++i ) p.Quantity[i] = 0 ; } ) ;
		public uint Dimension => (uint) Quantity.Length ;
		public Quant? this[ uint axis ] { get => Quantity.At((int)axis) ; set { if( axis>=Quantity.Length && value!=null ) Quantity.Set(q=>q.CopyTo(Quantity=new Quant?[axis+1],0)) ; if( axis<Quantity.Length ) Quantity[axis] = value ; } }
		public virtual Quant? this[ Axis axis ] { get => axis==Axis.Time ? Time.TotalSeconds : this[(uint)axis] ; set { if( axis!=Axis.Time ) this[(uint)axis] = value ; else if( value is Quant q ) Time = TimeSpan.FromSeconds(q) ; } }
		public Quant? this[ string axis ] { get => this[axis.Axis()] ; set => this[axis.Axis()] = value ; }
		public override bool TrySetMember( SetMemberBinder binder , object value ) { this[binder.Name] = (Quant?)value ; return base.TrySetMember( binder, value ) ; }
		public override bool TryGetMember( GetMemberBinder binder , out object result ) { result = this[binder.Name] ; return true ; }
		#endregion

		#region Trait
		public Quant? Dist { get => this[Axis.Dist] ; set => this[Axis.Dist] = value ; }
		public Quant? Ergy { get => this[Axis.Ergy] ; set => this[Axis.Ergy] = value ; }
		public Quant? Beat { get => this[Axis.Beat] ; set => this[Axis.Beat] = value ; }
		public Quant? Bit { get => this[Axis.Bit] ; set => this[Axis.Bit] = value ; }
		public Quant? Effort { get => this[Axis.Effort] ; set => this[Axis.Effort] = value ; }
		public Quant? Drag { get => this[Axis.Drag] ; set => this[Axis.Drag] = value ; }
		public Quant? Alt { get => this[Axis.Alt] ; set => this[Axis.Alt] = value ; }
		#endregion

		#region Quotient
		public Quant? Distance => Dist / Resist ;
		public Quant? Speed => Distance.Quotient(Time.TotalSeconds) ;
		public Quant? Pace => Time.TotalSeconds / Distance ;
		public Quant? Power => Ergy.Quotient(Time.TotalSeconds) ;
		public Quant? Force => Ergy.Quotient(Distance) ;
		public Quant? Beatage => Ergy.Quotient(Beat) ;
		public Quant? Bitage => Ergy.Quotient(Bit) ;
		public Quant? Beatrate => Beat.Quotient(Time.TotalMinutes) ;
		public Quant? Bitrate => Bit.Quotient(Time.TotalMinutes) ;
		public Quant? Draglet => Drag.Quotient(Bit) ;
		public Bipole? Gradelet => Asc / Dist ;
		#endregion

		#region Query
		public bool IsGeo => this[Axis.Lon]!=null || this[Axis.Lat]!=null ;
		public Quant Resist => Math.Pow( Draglet??1 , 1D/3D ) ;
		public string Exposion => "{0}={1}bW".Comb("{0}W/{1}`b".Comb(Power.use(Math.Round),Beatrate.use(Math.Round)),Beatage.use(Math.Round)) ;
		#endregion

		#region Operation
		public static Point operator|( Point prime , Quant?[] quantities ) => prime.Set(p=>{ for( uint i=0 ; i<quantities?.Length ; ++i ) if( p[i]==null ) p[i] = quantities[i] ; }) ;
		public static Point operator|( Point point , IEnumerable<Point> points ) => point | point.Date.Give(points) ;
		public static implicit operator Quant?[]( Point point ) => point?.Quantity ;
		public static Point operator/( Point point , Axis axis ) => point.Set(p=>p[axis]=null) ;
		public static Point operator/( Point point , string axis ) => point / axis.Axis() ;
		public static Point operator-( Point point , Point offset ) => new Point(new DateTime(point.Date.Ticks+offset.Date.Ticks>>1)){ Time = point.Date-offset.Date }.Set( p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = point[i]-offset[i] ; if( p.IsGeo ) p.Dist = p.Euclid(offset) ; } ) ;
		public static Point operator+( Point accu , Point diff ) => accu.Set( p => diff.Set( d => { p.Time += d.Time ; for( uint i=0 ; i<p.Dimension ; ++i ) p[i] += d[i] ; } ) ) ;
		#endregion

		#region Info
		public override string ToString() => $"{Action} {Sign} {Exposion} {Trace}" ;
		public virtual string Quantities => $"{((int)Dimension).Steps().Select(i=>Quantity[i].Get(q=>$"{(Axis)i}={q}")).Stringy(' ')}" ;
		public virtual string Trace => $"{Quantities} {Asc.Get(v=>$"Ascent={v:0}")} {Gradelet.Get(v=>$"Grade={v:.000}")} {Mark.nil(m=>m==Mark.No)}" ;
		#endregion

		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
	}
}
