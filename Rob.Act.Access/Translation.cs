using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Serialization.XML;

namespace Rob.Act
{
	public static class Translation
	{
		public static Path Internalize( this string data ) { if( data.Contains(Gpx.Extension.Sign) ) return data.Deserialize<Gpx.gpxType>() ; if( data.Contains(Tcx.Extension.Sign) ) return data.Deserialize<Tcx.TrainingCenterDatabase_t>() ; if( data.Contains(Csv.Skierg.Sign) ) return new Csv.Skierg(data) ; return null ; }
		public static string Externalize( this Path path , string ext ) { switch( ext ) { case "gpx" : return ((Gpx.gpxType)path).Serialize("utf-8","1.0") ; case "tcx" : return ((Tcx.TrainingCenterDatabase_t)path).Serialize("utf-8","1.0") ; case "skierg.csv" : return (Csv.Skierg)path ; default : return null ; } }
	}
}
