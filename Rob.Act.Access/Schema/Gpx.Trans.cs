using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using Aid.Extension;

namespace Rob.Act.Gpx
{
	using Quant = Double ;
	public partial class gpxType
	{
		[XmlIgnore] public trkType First => trk.At(0) ; [XmlIgnore] public trkType Last => trk.At(trk.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( trk==null ) yield break ; foreach( var track in trk ) foreach( var point in track.Iterator ) yield return point/*.Set(p=>p.Mark|=Last==track&&p.Mark.HasFlag(Mark.Stop)?Mark.Act:Mark.No)*/ ; } }
		public static implicit operator Path( gpxType way ) => way.Get( w => new Path(w.metadata.time,true,w.Iterator) { Spec = w.First?.name , Action = w.trk?.Select(t=>t.type).Stringy(',') } ).Set( w => w[0].Set(p=>w.Date=p.Date) ) ;
		public static implicit operator gpxType( Path path ) => path.Get( p => new gpxType { creator = "Rob" , metadata = new metadataType { name = p.Spec , time = p.Date , timeSpecified = p.Date!=null } , trk = (p/Mark.Act).Select(l=>(trkType)p).ToArray() } ) ;
	}
	public partial class trkType
	{
		[XmlIgnore] public bool Close ;
		[XmlIgnore] public trksegType First => trkseg.At(0) ; [XmlIgnore] public trksegType Last => trkseg.At(trkseg.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( trkseg==null ) yield break ; foreach( var segment in trkseg ) foreach( var point in segment.Iterator ) yield return point.Set(p=>p.Mark|=Close&&Last==segment&&p.Mark.HasFlag(Mark.Stop)?Mark.Act:Mark.No) ; } }
		public static implicit operator Path( trkType track ) => track.Get( t => new Path(t.First.First.time,true,t.Iterator) { Action = t.type , Spec = t.name } ) ;
		public static implicit operator trkType( Path path ) => path.Get( p => new trkType { type = p.Action , name = p.Spec , trkseg = (p/Mark.Stop).Where(s=>s.Count>1).Select(s=>(trksegType)s).ToArray() } ) ;
	}
	public partial class trksegType
	{
		[XmlIgnore] public wptType First => trkpt.At(0) ; [XmlIgnore] public wptType Last => trkpt.At(trkpt.Length-1) ;
		internal IEnumerable<Point> Iterator { get { if( trkpt==null ) yield break ; foreach( var point in trkpt ) yield return ((Point)point).Set(p=>p.Mark|=Last==point?Mark.Stop:Mark.No) ; } }
		public static implicit operator Point[]( trksegType segment ) => segment?.Iterator.Cast<Point>().ToArray() ;
		public static implicit operator trksegType( Path path ) => path.Get( s => new trksegType { trkpt = s?.Select(p=>(wptType)p).ToArray() } ) ;
	}
	public partial class wptType : Aid.Accessible<Quant?> , Aid.Accessible<Axis,Quant?>
	{
		public static implicit operator Point( wptType point ) => point.Get( p => new Point(p.time) { Spec = p.name , [Axis.Lon] = p[Axis.Lon] , [Axis.Lat] = p[Axis.Lat] , [Axis.Alt] = p[Axis.Alt] , [Axis.Heart] = p[Axis.Heart] , [Axis.Cycle] = p[Axis.Cycle] } ) ;
		public static implicit operator wptType( Point point ) => point.Get( p => new wptType { time = p.Date , timeSpecified = p.Date!=null , [Axis.Lat] = p[Axis.Lat] , [Axis.Lon] = p[Axis.Lon] , [Axis.Alt] = p[Axis.Alt] , [Axis.Heart] = p[Axis.Heart] , [Axis.Cycle] = p[Axis.Cycle] } ) ;
		XmlElement Extension => ( extensions ?? ( extensions = new extensionsType{ Any = new XmlElement[]{ "<gpxtpx:TrackPointExtension xmlns:gpxtpx=\"http://www.garmin.com/xmlschemas/TrackPointExtension/v1\"/>".ToXmlElement() } }) ).Any.At(0) ;
		XmlElement Element( string name ) => extensions?.Any?.SelectMany(a=>a.ChildNodes.OfType<XmlElement>()).FirstOrDefault(e=>e.LocalName==name) ;
		public Quant? this[ string quant ]
		{
			get { return Element(quant)?.FirstChild?.Value.Parse<Quant>() ; }
			set
			{
				var ele = Element(quant) ;
				if( ele==null && value!=null ) Extension.AppendChild( ele = Extension.OwnerDocument.CreateElement("gpxtpx",quant,"http://www.garmin.com/xmlschemas/TrackPointExtension/v1") ) ;
				if( ele!=null && value==null ) Extension.RemoveChild( ele ) ; else ele.Set( e=> e.InnerText = value.Stringy() ) ;
			}
		}
		public Quant? this[ Axis axis ]
		{
			get { switch( axis ) { case Axis.Lon : return (Quant)lonField ; case Axis.Lat : return (Quant)latField ; case Axis.Alt : return eleFieldSpecified ? (Quant)eleField : null as Quant? ; default : return this[axis.Axis()] ; } }
			set { switch( axis ) { case Axis.Lon : value.Use(v=>lonField=(decimal)v) ; break ; case Axis.Lat : value.Use(v=>latField=(decimal)v) ; break ; case Axis.Alt : eleFieldSpecified = null!=value.Use(v=>eleField=(decimal)v) ; break ; default : this[axis.Axis()] = value ; break ; } }
		}
	}
	static class Extension
	{
		public const string Sign = "<gpx " ;
		static readonly string[] Axes = new[] { "lon" , "lat" , "alt" , "dist" , "crud" , "flow" , "hr" , "cad" , "top" } ;
		internal static string Axis( this Axis axe ) => Axes.At((int)axe) ;
	}
}
