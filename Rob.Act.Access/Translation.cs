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
		public static Mark Kind ;
		public static Path Internalize( this string data )
		{
			if( data.Consists(Gpx.Extension.Sign) ) return data.Deserialize<Gpx.gpxType>() ;
			if( data.Consists(Tcx.Extension.Sign) ) return data.Deserialize<Tcx.TrainingCenterDatabase_t>() ;
			if( data.StartsBy(Csv.Skierg.Sign) ) return new Csv.Skierg(data) ;
			if( data.StartsBy(Partitioner.Sign) ) return new Partitioner(data) ;
			return (Path)data ;
		}
		public static string Externalize( this Path path , string ext ) { switch( ext ) { case "gpx" : return ((Gpx.gpxType)path).Serialize("utf-8","1.0") ; case "tcx" : return ((Tcx.TrainingCenterDatabase_t)path).Serialize("utf-8","1.0") ; case "skierg.csv" : return (Csv.Skierg)path ; default : return (string)path ; } }
		public static string Reconcile( this string file , bool primary = false )
		{
			string cofile = null ; if( file.EndsWith(".tcx") && file.Contains("concept2-logbook-workout-") ) { cofile = file ; file = file.Replace("logbook-workout","result").Replace(".tcx",".csv") ; }
			if( !primary )
			{
				if( Path.Primary && System.IO.Path.ChangeExtension(file,Path.Filext) is string p && file!=p && System.IO.File.Exists(p) ) return null ; // we prefer .path files over all serialization forms
				if( System.IO.File.Exists($"{file}.{Partitioner.Ext}") ) return null ; // we prefer ..par corrections over original serialization forms if they are not named
			}
			var data = file.ReadAllText(false) ; if( data==null ) return null ;
			if( (cofile??=file.Replace("result","logbook-workout").Replace(".csv",".tcx"))!=file && System.IO.File.Exists(cofile) )
			{
				var rest = cofile.ReadAllText(false) ; var text = rest.Get(t=>t.LeftFrom("<Track")??t.LeftFrom("</Lap>")) ;
				var date = text.RightFromFirst("<Lap StartTime=\"").LeftFrom("\"") ; var spec = text.RightFromFirst("<Id>").LeftFrom("</Id>") ;
				var time = text.RightFromFirst("<TotalTimeSeconds>").LeftFrom("</TotalTimeSeconds>") ; var dist = text.RightFrom("<DistanceMeters>").LeftFrom("</DistanceMeters>") ;
				var drag = text.RightFrom("<DragFactor>").LeftFrom("</DragFactor>") ?? text.RightFrom("<Drag>").LeftFrom("</Drag>") ?? "100" ;
				var action = text.RightFrom("<Action>").LeftFrom("</Action>") ; var subject = text.RightFrom("<Subject>").LeftFrom("</Subject>") ; var locus = text.RightFrom("<Locus>").LeftFrom("</Locus>") ; var refine = text.RightFrom("<Refine>").LeftFrom("</Refine>") ;
				string laps = null ; if( (rest=rest.Substring(text.Length+6)).Consists("<Lap") ) for( var (tacu,dacu) = (time.Parse(0D),dist.Parse(0D)) ; (text=rest.Get(t=>t.LeftFrom("<Track")??t.LeftFrom("</Lap>")))!=null ; rest = rest.Substring(text.Length+6) )
				{
					if( laps==null ) laps = $"{tacu},{dacu};" ; // first element if we add active ones
					tacu += text.RightFromFirst("<TotalTimeSeconds>").LeftFrom("</TotalTimeSeconds>").Parse(0D) ; dacu += text.RightFrom("<DistanceMeters>").LeftFrom("</DistanceMeters>").Parse(0D) ;
					/*if( text.Contains("<Intensity>Resting</Intensity>") )*/ laps += $"{tacu},{dacu};" ;
				}
				else { var lavs = data.Trim().RightFrom(Environment.NewLine).Separate(',') ; lavs[0] = (lavs[0].Trim('"').Parse<uint>()+1).Stringy() ?? lavs[0] ; lavs[1] = time ; lavs[2] = dist ; data += lavs.Stringy(',') ; data += $",\"{drag}\"{Environment.NewLine}" ; } // append of final misssing line
				var first = data.LeftFrom(Environment.NewLine) ; var nef = first+$",\"Refine={refine}\",\"Locus={locus}\",\"Subject={subject}\",\"Drag Factor={drag}\",\"Date={date}\",\"Spec={action??spec}\"{laps.Get(l=>$",\"Laps={laps}\"")}" ; data = data.Replace(first,nef) ;
			}
			else if( file.EndsWith(Partitioner.Ext) ) data = $"{Partitioner.Sign}{file.LeftFromLast(Partitioner.Ext)}{Environment.NewLine}{data}" ;
			return data ;
		}
		public static void Partitionate( this Path path ) { if( path==null ) return ; var parter = path.Origin+Partitioner.Ext ; if( System.IO.File.Exists(parter) ) return ; path.Where(p=>p.Mark.HasFlag(Mark.Stop)&&p.At+1<path.Count).Select(p=>p.At).Stringy(' ').Null(p=>p.No()).Set(p=>parter.WriteAll(p)) ; }
	}
}
