using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using Aid.Extension;
using Aid.Serialization.XML;

namespace Rob.Act.Tcx
{
	using Quant = Double ;
	#if false
	public partial class TrainingCenterDatabase_t
	{
		[XmlIgnore] public Activity_t First => Activities.Activity.At(0) ; [XmlIgnore] public Activity_t Last => Activities.Activity.At(Activities.Activity.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( Activities.Activity==null ) yield break ; foreach( var act in Activities.Activity ) foreach( var point in act.Iterator ) yield return point ; } }
		public static implicit operator Path( TrainingCenterDatabase_t way ) => way.Get( w => new Path(w.First.Id,w.Iterator) { Action = w.Activities.Activity.Select(a=>a.Sport).Distinct().Stringy(',') } ) ;
		public static implicit operator TrainingCenterDatabase_t( Path path ) => path.Get( p => new TrainingCenterDatabase_t { Activities = new ActivityList_t { Activity = (p/Mark.Action).Select(a=>(Activity_t)a).ToArray() } } ) ;
	}
	public partial class Activity_t
	{
		[XmlIgnore] public ActivityLap_t First => Lap.At(0) ; [XmlIgnore] public ActivityLap_t Last => Lap.At(Lap.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( Lap==null ) yield break ; foreach( var lap in Lap ) foreach( var point in lap.Iterator ) yield return point.Set(p=>p.Mark|=Last==lap&&p.Mark.HasFlag(Mark.Lap)?Mark.Action:Mark.No) ; } }
		public static implicit operator Path( Activity_t act ) => act.Get( a => new Path(a.Id,a.Iterator) { Action = a.Sport.Stringy() } ) ;
		public static implicit operator Activity_t( Path path ) => path.Get( p => new Activity_t { Lap = (p/Mark.Lap).Select(s=>(ActivityLap_t)s).ToArray() } ) ;
	}
	public partial class ActivityLap_t
	{
		[XmlIgnore] public Trackpoint_t[] First => Track.At(0) ; [XmlIgnore] public Trackpoint_t[] Last => Track.At(Track.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( Track==null ) yield break ; foreach( var segment in Track ) foreach( var point in segment ) yield return ((Point)point).Set(p=>p.Mark|=segment.Last()==point?Mark.Stop:Mark.No).Set(p=>p.Mark|=Last==segment&&p.Mark.HasFlag(Mark.Stop)?Mark.Stop:Mark.No) ; } }
		public static implicit operator Path( ActivityLap_t lap ) => lap.Get( t => new Path(t.StartTime,t.Iterator) { Action = t.Notes } ) ;
		public static implicit operator ActivityLap_t( Path path ) => path.Get( w => new ActivityLap_t { Track = (w/Mark.Stop).Select(s=>s.Select(p=>(Trackpoint_t)p).ToArray()).ToArray() } ) ;
	}
	#else
	public partial class TrainingCenterDatabase_t
	{
		[XmlIgnore] public Activity_t First => Activities.Activity.At(0) ; [XmlIgnore] public Activity_t Last => Activities.Activity.At(Activities.Activity.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( Activities.Activity==null ) yield break ; foreach( var act in Activities.Activity ) foreach( var point in act.Iterator ) yield return point ; } }
		public static implicit operator TrainingCenterDatabase_t( string text ) => text.Deserialize<TrainingCenterDatabase_t>() ;
		public static implicit operator Path( TrainingCenterDatabase_t way ) => way.Get( w => new Path(w.First.Id,w.Iterator,Translation.Kind,(Axis.Beat,60),(Axis.Bit,60)) { Action = w.Activities.Activity.Select(a=>a.Sport).Distinct().Stringy(',') } ) ;
		public static implicit operator TrainingCenterDatabase_t( Path path ) => path.Get( p => new TrainingCenterDatabase_t { Activities = new ActivityList_t { Activity = (p/Mark.Lap).Select(a=>(Activity_t)a).ToArray() } } ) ;
	}
	public partial class Activity_t
	{
		[XmlIgnore] public ActivityLap_t First => Lap.At(0) ; [XmlIgnore] public ActivityLap_t Last => Lap.At(Lap.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( Lap==null ) yield break ; foreach( var lap in Lap ) foreach( var point in lap.Iterator ) yield return point ; } }
		public static implicit operator Path( Activity_t act ) => act.Get( a => new Path(a.Id,a.Iterator,Translation.Kind,(Axis.Beat,60),(Axis.Bit,60)) { Action = a.Sport.Stringy() } ) ;
		public static implicit operator Activity_t( Path path ) => path.Get( p => new Activity_t { Id = p.Date , Lap = (p/Mark.Stop).Select(s=>(ActivityLap_t)s).ToArray() } ) ;
	}
	public partial class ActivityLap_t
	{
		[XmlIgnore] public Trackpoint_t First => Track.At(0) ; [XmlIgnore] public Trackpoint_t Last => Track.At(Track.Count-1) ;
		internal IEnumerable<Point> Iterator { get { if( Track==null ) yield break ; foreach( var point in Track ) yield return ((Point)point).Set(p=>p.Mark|=Last==point?Mark.Stop:Mark.No) ; } }
		public static implicit operator Path( ActivityLap_t lap ) => lap.Get( t => new Path(t.StartTime,t.Iterator,Translation.Kind,(Axis.Beat,60),(Axis.Bit,60)) { Action = t.Notes , Time = TimeSpan.FromSeconds(t.TotalTimeSeconds) } ) ;
		public static implicit operator ActivityLap_t( Path path ) => path++.Get( w => new ActivityLap_t { StartTime = path.Date , TotalTimeSeconds = w.Time.TotalSeconds , DistanceMeters = w.Dist??0 , Track = w.Select(p=>(Trackpoint_t)p).ToList() } ) ;
	}
#endif
	public partial class Trackpoint_t : Aid.Accessible<Axis,Quant?>
	{
		public static implicit operator Point( Trackpoint_t point ) => point.Get( p => new Point(p.Time) { [Axis.Lon] = p[Axis.Lon] , [Axis.Lat] = p[Axis.Lat] , [Axis.Alt] = p[Axis.Alt] , [Axis.Beat] = p[Axis.Beat] , [Axis.Bit] = p[Axis.Bit] } ) ;
		public static implicit operator Trackpoint_t( Point point ) => point.Get( p => new Trackpoint_t { Time = p.Date , [Axis.Lat] = p[Axis.Lat] , [Axis.Lon] = p[Axis.Lon] , [Axis.Alt] = p[Axis.Alt] , [Axis.Beat] = p[Axis.Beat] , [Axis.Bit] = p[Axis.Bit] } ) ;
		Position_t Sphere => Position ??= new() ;
		HeartRateInBeatsPerMinute_t Heart => HeartRateBpm ??= new() ;
		public Quant? this[ Axis axis ]
		{
			get => axis switch { Axis.Lon => Position?.LongitudeDegrees , Axis.Lat => Position?.LatitudeDegrees , Axis.Alt => AltitudeMetersSpecified ? (Quant)AltitudeMeters : null , Axis.Beat => HeartRateBpm?.Value , Axis.Bit => Cadence , _ => null } ;
			set { switch( axis ) { case Axis.Lon : value.Use(v=>Sphere.LongitudeDegrees=(double)v) ; break ; case Axis.Lat : value.Use(v=>Sphere.LatitudeDegrees=(double)v) ; break ; case Axis.Alt : AltitudeMetersSpecified = null!=value.Use(v=>altitudeMetersField=(double)v) ; break ; case Axis.Beat : value.Use(v=>Heart.Value=(byte)v) ; break ; case Axis.Bit : value.Use(v=>Cadence=(byte)v) ; break ; } }
		}
	}
	static class Extension
	{
		public const string Sign = "<TrainingCenterDatabase" ;
		internal static Trackpoint_t Last( this Trackpoint_t[] segment ) => segment.At(segment.Length-1) ;
	}
}
          