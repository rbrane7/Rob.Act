using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using Configer = System.Configuration.ConfigurationManager ;
	using Quant = Double ;
	/// <summary>
	/// Kind of separation marks .
	/// </summary>
	[Flags] public enum Mark { No = 0 , Stop = 1 , Lap = 2 , Act = 4 }
	[Flags] public enum Oper { Merge = 0 , Combi = 1 , Trim = 2 , Smooth = 4 , Relat = 8 }
	public enum Axis { Lon , Longitude = Lon , Lat , Latitude = Lat , Alt , Altitude = Alt , Dist , Distance = Dist , Crud , Flow , Heart , Cycle , Ergy , Energy = Ergy , Effort , Time }
	static class Basis
	{
		#region Axis specifics
		static List<string> axis = Enum.GetNames(typeof(Axis)).ToList() ;
		internal static Axis Axis( this string name ) => (Axis)( axis.IndexOf(name).nil(i=>i<0) ?? axis.Set(a=>a.Add(name)).Count-1 ) ;
		internal static Quant ActLim( this Axis axis , string activity ) => 50 ;
		#endregion

		#region Point interpolation
		/// <summary>
		/// Interpolation point from given points at given time .
		/// </summary>
		/// <param name="date"> Time to which interpolate the point . </param>
		/// <param name="points"> Points are expected to be ordered by time . </param>
		/// <returns> Interpolation point at given time of given points . </returns>
		internal static Point Give( this DateTime date , IEnumerable<Point> points ) { Point bot = null ; foreach( var point in points ) if( point.Date<date ) bot = point ; else if( point.Date==date ) return point ; else if( bot==null ) return new Point(date)|point ; else return new Point(date)|date.Give(bot,point) ; return new Point(date)|bot ; }
		internal static Quant?[] Give( this DateTime date , Point bot , Point top ) { var quo = (Quant)( (date-bot.Date).TotalSeconds/(top.Date-bot.Date).TotalSeconds ) ; var cuo = 1-quo ; return ((int)Math.Max(bot.Dimension,top.Dimension)).Steps().Select(i=>(bot[(uint)i]*cuo+top[(uint)i]*quo)).ToArray() ; }
		internal static Quant? Quotient( this Quant? x , Quant? y ) => x / y.Nil() ;
		#endregion

		#region Euclid metrics
		public static int GradeAccu = Configer.AppSettings["Grade.Accumulation"].Parse<int>() ?? 7 , VeloAccu = Configer.AppSettings["Speed.Accumulation"].Parse<int>() ?? 1 ;
		static Quant? Sqrm( this Point point , Axis axis , Point at ) { Quant? value = point[axis]??0 ; if( axis==Act.Axis.Lat ) value *= Degmet ; if( axis==Act.Axis.Lon ) value *= Londeg(at[Act.Axis.Lat]) ; return value*value ; }
		static readonly Quant Degmet = 111321.5 ;
		internal const Quant Gravity = 10 ;
		static Quant? Londeg( Quant? latdeg ) => latdeg.use(l=>Math.Cos(l.Rad())) * Degmet ;
		static Quant Rad( this Quant deg ) => deg/180*Math.PI ;
		static Quant? Polar( this Point point , Point offset ) => point.Sqrm(Act.Axis.Lon,offset)+point.Sqrm(Act.Axis.Lat,offset) ;
		internal static Quant? Euclid( this Point point , Point offset ) => (point.Polar(offset)+point.Sqrm(Act.Axis.Alt,offset)).use(Math.Sqrt) ;
		internal static Quant? Sphere( this Point point , Point offset ) => point.Polar(offset).use(Math.Sqrt) ;
		internal static Quant? Grade( this Point point , Point offset ) => point.Get(p=>p[Act.Axis.Alt]/p.Sphere(offset).Nil(d=>d==0)) ;
#if true // grade average by count
		internal static Quant? Grade( this Path path , int at , Point offset ) { var count = 0 ; Quant? grade = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(g=>grade+=g)!=null ) ++count ; return count>0 ? grade/count : null ; }
#else	// grade average by distance
		internal static Quant? Grade( this Path path , int at , Point offset ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-GradeAccu,0) ; i<Math.Min(path.Count,at+GradeAccu+1) ; ++i ) if( path[i].Grade(offset).Set(v=>val+=v*path[i].Sphere(offset))!=null ) vol+=path[i].Sphere(offset).Value ; return vol>0 ? val/vol : null ; }
#endif
		internal static Quant? Veloa( this Path path , int at ) { Quant vol = 0 ; Quant? val = 0 ; for( var i=Math.Max(at-VeloAccu,0) ; i<Math.Min(path.Count,at+VeloAccu+1) ; ++i ) if( path[i].Speed.Set(v=>val+=v*path[i].Time.TotalSeconds)!=null ) vol+=path[i].Time.TotalSeconds ; return (vol>0?val/vol:null) ; }
		internal static Quant? Vibre( this Path path , int at ) => path[at].Speed / path.Veloa(at) ;
		#endregion
	}
}
