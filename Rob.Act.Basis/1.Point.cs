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
	public class Point : Pre.Point , Accessible<Axis,Quant?> , INotifyPropertyChanged
	{
		#region Construction
		public Point( DateTime date ) : base(date) {}
		public Point( Point point ) : base(point) {}
		#endregion

		#region State
		/// <summary>
		/// Assotiative text .
		/// </summary>
		public override string Spec { set { if( value==base.Spec ) return ; base.Spec = value ; propertyChanged.On(this,"Spec") ; } }
		/// <summary>
		/// Ascent of the path .
		/// </summary>
		public Bipole? Asc { get; set; }
		/// <summary>
		/// Deviation of the path .
		/// </summary>
		public Bipole? Dev { get; set; }
		#endregion

		#region Vector
		public static Point Zero( DateTime date ) => new Point(date){ Time = TimeSpan.Zero }.Set( p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = 0 ; } ) ;
		public override uint Dimension => (uint)( ((Quant?[])this)?.Length ?? (int)Axis.Time ) ;
		public virtual Quant? this[ Axis axis ] { get => axis==Axis.Time ? Time.TotalSeconds : this[(uint)axis] ; set { if( axis!=Axis.Time ) this[(uint)axis] = value ; else if( value is Quant q ) Time = TimeSpan.FromSeconds(q) ; } }
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
		public Bipole? Bendlet => Dev / Dist ;
		#endregion

		#region Query
		public bool IsGeo => this[Axis.Lon]!=null || this[Axis.Lat]!=null ;
		public Quant Resist => Math.Pow( Draglet??1 , 1D/3D ) ;
		public override string Exposion => "{0}={1}bW".Comb("{0}/{1}".Comb(Power.Get(p=>$"{Math.Round(p)}W"),Beatrate.Get(b=>$"{Math.Round(b)}`b")),Beatage.use(Math.Round))+$" {Speed*3.6:0.00}km/h" ;
		public override string Trace => $"{Draglet.Get(v=>$"Drag={v:0.00}")} {Asc.Get(v=>$"Ascent={v:0}m")} {Gradelet.Get(v=>$"Grade={v:.000}")} {Dev.Get(v=>$"Devia={v:0}m")} {Bendlet.Get(v=>$"Bend={v:.000}")} {Quantities} {Mark.nil(m=>m==Mark.No)}" ;
		#endregion

		#region Operation
		public static Point operator|( Point prime , Quant?[] quantities ) => prime.Set(p=>{ for( uint i=0 ; i<quantities?.Length ; ++i ) if( p[i]==null ) p[i] = quantities[i] ; }) ;
		public static Point operator|( Point point , IEnumerable<Point> points ) => point | point.Date.Give(points) ;
		public static implicit operator Quant?[]( Point point ) => point as Pre.Point ;
		public static Point operator/( Point point , Axis axis ) => point / (uint)(int)axis ;
		public static Point operator/( Point point , uint axis ) => point.Set(p=>p[axis]=null) ;
		public static Point operator/( Point point , string axis ) => point / axis.Axis() ;
		public static Point operator-( Point point , Point offset ) => new Point(new DateTime(point.Date.Ticks+offset.Date.Ticks>>1)){ Time = point.Date-offset.Date }.Set( p=>{ for( uint i=0 ; i<p.Dimension ; ++i ) p[i] = point[i]-offset[i] ; if( p.IsGeo ) p.Dist = p.Euclid(offset) ; } ) ;
		public static Point operator+( Point accu , Point diff ) => accu.Set( p => diff.Set( d => { p.Time += d.Time ; for( uint i=0 ; i<p.Dimension ; ++i ) p[i] += d[i] ; } ) ) ;
		public Geos? Geo => this ;
		#endregion

		public event PropertyChangedEventHandler PropertyChanged { add => propertyChanged += value.DispatchResolve() ; remove => propertyChanged -= value.DispatchResolve() ; } protected PropertyChangedEventHandler propertyChanged ;
	}
}
