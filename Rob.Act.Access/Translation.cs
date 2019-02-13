using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Serialization.XML;
using Aid.Extension;

namespace Rob.Act
{
	public static class Translation
	{
		public static Path Internalize( this string data ) { if( data.Contains(Gpx.Extension.Sign) ) return data.Deserialize<Gpx.gpxType>() ; if( data.Contains(Tcx.Extension.Sign) ) return data.Deserialize<Tcx.TrainingCenterDatabase_t>() ; if( data.Contains(Csv.Skierg.Sign) ) return new Csv.Skierg(data) ; return null ; }
		public static string Externalize( this Path path , string ext ) { switch( ext ) { case "gpx" : return ((Gpx.gpxType)path).Serialize("utf-8","1.0") ; case "tcx" : return ((Tcx.TrainingCenterDatabase_t)path).Serialize("utf-8","1.0") ; case "skierg.csv" : return (Csv.Skierg)path ; default : return null ; } }
		public static string Reconcile( this string file )
		{
			string cofile = null ; if( file.EndsWith(".tcx") && file.Contains("concept2-logbook-workout-") ) { cofile = file ; file = file.Replace("logbook-workout","result").Replace(".tcx",".csv") ; }
			var data = file.ReadAllText() ; if( cofile==null ) cofile = file.Replace("result","logbook-workout").Replace(".csv",".tcx") ;
			if( cofile!=file && System.IO.File.Exists(cofile) )
			{
				var text = cofile.ReadAllText().LeftFrom("<Track>") ; var date = text.RightFromFirst("<Lap StartTime=\"").LeftFrom("\"") ; var spec = text.RightFromFirst("<Id>").LeftFrom("</Id>") ;
				var time = text.RightFromFirst("<TotalTimeSeconds>").LeftFrom("</TotalTimeSeconds>") ; var dist = text.RightFrom("<DistanceMeters>").LeftFrom("</DistanceMeters>") ;
				var drag = text.RightFrom("<DragFactor>").LeftFrom("</DragFactor>")??text.RightFrom("<Drag>").LeftFrom("</Drag>")??"100" ; var action = text.RightFrom("<Action>").LeftFrom("</Action>") ;
				var lavs = data.Trim().RightFrom(Environment.NewLine).Separate(',') ; lavs[1] = time ; lavs[2] = dist ; data += lavs.Stringy(',') ; data += $",\"{drag}\"{Environment.NewLine}" ; // append of final misssing line
				var first = data.LeftFrom(Environment.NewLine) ; var nef = first+$",\"Drag Factor={drag}\",\"Date={date}\",\"Spec={action??spec}\"" ; data = data.Replace(first,nef) ;
			}
			return data ;
		}
	}
}
