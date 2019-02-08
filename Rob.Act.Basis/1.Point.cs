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
		public string Sign => sign ?? ( sign = $"{Date}{Time.nil(t=>t==TimeSpan.Zero).Get(t=>$"+{t}")}" ) ; string sign ;
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
		public Mark Mark { get; set; }
		#endregion

		#region Trait
		public static Point Zero( DateTime date ) => new Point(date){ Time = TimeSpan.Zero }.Set( p=>{ for( var i=0 ; i<p.Dimension ; ++i ) p.Quantity[i] = 0 ; } ) ;
		public uint Dimension => (uint) Quantity.Length ;
		public Quant? this[ uint axis ] { get => Quantity.At((int)axis) ; set { if( axis>=Quantity.Length && value!=null ) Quantity.Set(q=>q.CopyTo(Quantity=new Quant?[axis+1],0)) ; if( axis<Quantity.Length ) Quantity[axis] = value ; } }
		public virtual Quant? this[ Axis axis ] { get => axis==Axis.Time ? Time.TotalSeconds : this[(uint)axis] ; set { if( axis!=Axis.Time ) this[(uint)axis] = value ; else if( value is Quant q ) Time = TimeSpan.FromSeconds(q) ; } }
		public Quant? this[ string axis ] { get => this[axis.Axis()] ; set => this[axis.Axis()] = value ; }
		public override bool TrySetMember( SetMemberBinder binder , object value ) { this[binder.Name] = (Quant?)value ; return base.TrySetMember( binder, value ) ; }
		public override bool TryGetMember( GetMemberBinder binder , out object result ) { result = this[binder.Name] ; return base.TryGetMember( binder, out result ) ; }
		public Quant? Dist { get => this[Axis.Dist] ; set => this[Axis.Dist] = value ; }
		public Quant? Ergy { get => this[Axis.Ergy] ; set => this[Axis.Ergy] = value ; }
		public Quant? Heart { get => this[Axis.Heart] ; set => this[Axis.Heart] = value ; }
		public Quant? Cycle { get => this[Axis.Cycle] ; set => this[Axis.Cycle] = value ; }
		public Quant? Effort { get => this[Axis.Effort] ; set => this[Axis.Effort] = value ; }
		public Quant? Drag { get => this[Axis.Drag] ; set => this[Axis.Drag] = value ; }
		#endregion

		#region Quotient
		public Quant? Distance => Dist / Resist ;
		public Quant? Speed => Distance.Quotient(Time.TotalSeconds) ;
		public Quant? Pace => Time.TotalSeconds / Distance ;
		public Quant? Power => Ergy.Quotient(Time.TotalSeconds) ;
		public Quant? Force => Ergy.Quotient(Distance) ;
		public Quant? Heartage => Ergy.Quotient(Heart) ;
		public Quant? Cycleage => Ergy.Quotient(Cycle) ;
		public Quant? Heartrate => Heart.Quotient(Time.TotalMinutes) ;
		public Quant? Cyclerate => Cycle.Quotient(Time.TotalMinutes) ;
		public Quant? Draglet => Drag.Quotient(Cycle) ;
		#endregion

		#region Query
		public bool IsGeo => this[Axis.Longitude]!=null || this[Axis.Lat]!=null ;
		public Quant Resist => Math.Pow( Draglet/100 ?? 1 , 1D/3D ) ;
		public string Exposion => "{0}={1}J/H".Comb("{0}W/{1}H".Comb(Power.use(Math.Round),Heartrate.use(Math.Round)),Heartage.use(Math.Round)) ;
		#endregion

		#region Operation
		public static Point operator|( Point prime , Quant?[] quantities ) => prime.Set(p=>{ for( uint i=0 ; i<quantities?.Length ; ++i ) if( p[i]==null ) p[i] = quantities[i] ; }) ;
		public static Point operator|( Point point , IEnumerable<Point> points ) => point | point.Date.Give(points) ;
		public static implicit operator Quant?[]( Point point ) => point?.Quantity ;
		public static Point operator/( Point point , Axis axis ) => point.Set(p=>p[axis]=null) ;
		public static Point operator/( Point point , string axis ) => point / axis.Axis() ;
		public static Point operator-( Point point , Point offset ) => new Point(new DateTime(point.Date.Ticks+offset.Date.Ticks>>1)){ Time = point.Date-offset.Date }.Set( p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = point[i]-offset[i] ; if( p.IsGeo ) p[Axis.Dist] = p.Euclid(offset) ; } ) ;
		public static Point operator+( Point accu , Point diff ) => accu.Set( p => diff.Set( d => { p.Time += d.Time ; for( uint i=0 ; i<p.Dimension ; ++i ) p[i] += d[i] ; } ) ) ;
		#endregion

		#region Info
		public override string ToString() => $"{Action} {Sign} {Exposion} {Trace}" ;
		public virtual string Quantities => $"{((int)Dimension).Steps().Select(i=>Quantity[i].Get(q=>$"{(Axis)i}={q}")).Stringy(' ')}" ;
		public virtual string Trace => $"{Quantities} {Mark.nil(m=>m==Mark.No)}" ;
		#endregion

		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
	}
}
