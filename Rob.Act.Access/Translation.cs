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
		public static Path Internalize( this string data )
		{
			if( data.Contains(Gpx.Extension.Sign) ) return data.Deserialize<Gpx.gpxType>() ;
			if( data.Contains(Tcx.Extension.Sign) ) return data.Deserialize<Tcx.TrainingCenterDatabase_t>() ;
			if( data.StartsBy(Csv.Skierg.Sign) ) return new Csv.Skierg(data) ;
			if( data.StartsBy(Partitioner.Sign) ) return new Partitioner(data) ;
			return null ;
		}
		public static string Externalize( this Path path , string ext ) { switch( ext ) { case "gpx" : return ((Gpx.gpxType)path).Serialize("utf-8","1.0") ; case "tcx" : return ((Tcx.TrainingCenterDatabase_t)path).Serialize("utf-8","1.0") ; case "skierg.csv" : return (Csv.Skierg)path ; default : return null ; } }
		public static string Reconcile( this string file )
		{
			string cofile = null ; if( file.EndsWith(".tcx") && file.Contains("concept2-logbook-workout-") ) { cofile = file ; file = file.Replace("logbook-workout","result").Replace(".tcx",".csv") ; }
			var data = file.ReadAllText() ; if( (cofile??(cofile=file.Replace("result","logbook-workout").Replace(".csv",".tcx")))!=file && System.IO.File.Exists(cofile) )
			{
				var text = cofile.ReadAllText().Get(t=>t.LeftFrom("<Track")??t.LeftFrom("</Lap>")) ; var date = text.RightFromFirst("<Lap StartTime=\"").LeftFrom("\"") ; var spec = text.RightFromFirst("<Id>").LeftFrom("</Id>") ;
				var time = text.RightFromFirst("<TotalTimeSeconds>").LeftFrom("</TotalTimeSeconds>") ; var dist = text.RightFrom("<DistanceMeters>").LeftFrom("</DistanceMeters>") ;
				var drag = text.RightFrom("<DragFactor>").LeftFrom("</DragFactor>")??text.RightFrom("<Drag>").LeftFrom("</Drag>")??"100" ;
				var action = text.RightFrom("<Action>").LeftFrom("</Action>") ; var subject = text.RightFrom("<Subject>").LeftFrom("</Subject>") ; var locus = text.RightFrom("<Locus>").LeftFrom("</Locus>") ;
				var lavs = data.Trim().RightFrom(Environment.NewLine).Separate(',') ; lavs[0] = (lavs[0].Trim('"').Parse<uint>()+1).Stringy()??lavs[0] ; lavs[1] = time ; lavs[2] = dist ; data += lavs.Stringy(',') ; data += $",\"{drag}\"{Environment.NewLine}" ; // append of final misssing line
				var first = data.LeftFrom(Environment.NewLine) ; var nef = first+$",\"Locus={locus}\",\"Subject={subject}\",\"Drag Factor={drag}\",\"Date={date}\",\"Spec={action??spec}\"" ; data = data.Replace(first,nef) ;
			}
			else if( file.EndsWith(".par") ) data = $"{Partitioner.Sign}{file.LeftFromLast(".par")}{Environment.NewLine}{data}" ;
			return data ;
		}
		public static void Partitionate( this Path path ) { if( path==null ) return ; var parter = path.Origin+".par" ; if( System.IO.File.Exists(parter) ) return ; path.Where(p=>p.Mark.HasFlag(Mark.Stop)&&p.Bit+1<path.Count).Select(p=>(uint)p.Bit).Stringy(' ').Null(p=>p.No()).Set(p=>parter.WriteAll(p)) ; }
	}
}
