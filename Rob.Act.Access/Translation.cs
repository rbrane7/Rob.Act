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
		static readonly char[] CrLf = new[]{'\r','\n'} ;
		const string GpxFitAnchor = "creator=\"" ;
		public static Path Internalize( this string data )
		{
			if( data.Consists(Gpx.Extension.Sign) ) return (Gpx.gpxType)data ;
			if( data.Consists(Tcx.Extension.Sign) ) return (Tcx.TrainingCenterDatabase_t)data ;
			if( Erg.Csv.Sign(data) ) return new Erg.Csv(data) ;
			if( data.StartsBy(Partitioner.Sign) ) return new Partitioner(data) ;
			if( data.StartsBy(Csv.Bio.Sign) ) return new Csv.Bio(data) ;
			return (Path)data ;
		}
		public static string Externalize( this Path path , string ext ) => ext switch { "gpx" => ((Gpx.gpxType)path).Serialize("utf-8","1.0") , "tcx" => ((Tcx.TrainingCenterDatabase_t)path).Serialize("utf-8","1.0") , "skierg.csv" => (Erg.Csv)path , _ => (string)path , } ;
		public static string Reconcile( this string file , bool primary = false )
		{
			string cofile = null ; if( file.EndsWith(".tcx") && file.Contains("concept2-logbook-workout-") ) { cofile = file ; file = file.Replace("logbook-workout","result").Replace(".tcx",Csv.Ext) ; }
			if( !primary )
			{
				if( cofile is not null )
				{
					if( Path.Primary && System.IO.Path.ChangeExtension(cofile,Path.Filext) is string p && cofile!=p && System.IO.File.Exists(p) ) return null ; // we prefer .path files over all serialization forms
					if( System.IO.File.Exists($"{cofile}.{Partitioner.Ext}") ) return null ; // we prefer ..par corrections over original serialization forms if they are not named
				}
				if( Path.Primary )if( System.IO.Path.ChangeExtension(file,Path.Filext) is string p && file!=p && System.IO.File.Exists(p) ) return null ; // we prefer .path files over all serialization forms
				if( System.IO.File.Exists($"{file}.{Partitioner.Ext}") ) return null ; // we prefer ..par corrections over original serialization forms if they are not named
				if( Gpx.Extension.Primary && Path.Primary^System.IO.Path.GetExtension(file).Consists(Path.Filext) )if( System.IO.Path.ChangeExtension(file,Gpx.Extension.File) is string p && file!=p && System.IO.File.Exists(p) ) return null ; // we prefer .gpx files over all serialization forms
			}
			var data = file.ReadAllText(false) ; if( data==null ) return null ;
			string sign = data.LeftFrom('\n')?.Trim() , rest , dart , dres ; /* first signing line of data */
			string Part( string data ) => data.LeftFrom(sign,from:sign.Length,all:true) ; static string Intra( string data ) => data.Null(v=>v.Contains("</Activity>")) ;
			if( (cofile??=file.Replace("result","logbook-workout").Replace(Csv.Ext,".tcx"))!=file && System.IO.File.Exists(cofile) ) for
			(
				rest = cofile.ReadAllText(false).RightFromFirst("<Activity") , dart = Part(data) , dres = data.Sub(dart.Length) , data = null ;
				!rest.No() ; rest = rest.RightFromFirst("<Activity") , dart = Part(dres) , dres = dres.Sub(dart?.Length??0)
			) // Now cofile is always just heder , not point-to-point data .
			{
				var text = rest.LeftFrom("<Track") ?? rest.LeftFrom("</Lap>") ;
				var date = text.RightFromFirst("<Lap StartTime=\"").LeftFrom("\"")?.Trim() ; var spec = text.RightFromFirst("<Id>").LeftFrom("</Id>") ;
				var time = text.RightFromFirst("<TotalTimeSeconds>").LeftFrom("</TotalTimeSeconds>") ; var dist = text.RightFromFirst("<DistanceMeters>").LeftFrom("</DistanceMeters>") ;
				var drag = text.RightFromFirst("<DragFactor>").LeftFrom("</DragFactor>") ?? text.RightFromFirst("<Drag>").LeftFrom("</Drag>") ?? "100" ;
				var action = text.RightFromFirst("<Action>").LeftFrom("</Action>") ; var subject = text.RightFromFirst("<Subject>").LeftFrom("</Subject>") ;
				var locus = text.RightFromFirst("<Locus>").LeftFrom("</Locus>") ; var refine = text.RightFromFirst("<Refine>").LeftFrom("</Refine>") ;
				string laps = null ; if( (rest=rest[(text.Length+6)..]).Consists("<Lap") )
				for( var (tacu,dacu) = (time.Parse(0D),dist.Parse(0D)) ; Intra(text=rest.Get(t=>t.LeftFrom("<Track")??t.LeftFrom("</Lap>"))) is not null ; rest = rest[(text.Length+6)..] )
				{
					if( laps==null ) laps = $"{tacu},{dacu};" ; // first element if we add active ones
					tacu += text.RightFromFirst("<TotalTimeSeconds>").LeftFrom("</TotalTimeSeconds>").Parse(0D) ; dacu += text.RightFromFirst("<DistanceMeters>").LeftFrom("</DistanceMeters>").Parse(0D) ;
					/*if( text.Contains("<Intensity>Resting</Intensity>") )*/ laps += $"{tacu},{dacu};" ;
				}
				else
				{
					var lavs = dart.Trim().RightFrom('\n').Separate(',') ; lavs[0] = (lavs[0].Trim('"').Parse<uint>()+1).Stringy() ?? lavs[0] ; lavs[1] = time ; lavs[2] = dist ;
					dart += lavs.Stringy(',') ; dart += $",\"{drag}\"{Environment.NewLine}" ; /* append of final misssing line */
				}
				data += dart.Replace(sign,sign+$",\"Refine={refine}\",\"Locus={locus}\",\"Subject={subject}\",\"Drag Factor={drag}\",\"Date={date}\",\"Spec={action??spec}\"{laps.Get(l=>$",\"Laps={laps}\"")}") ;
			}
			else if( file.EndsWith(Partitioner.Ext) ) data = $"{Partitioner.Sign}{file.LeftFromLast(Partitioner.Ext)}{Environment.NewLine}{data}" ;
			else if( file.EndsWith(Csv.Bio.Ext) && file.LeftFrom(Csv.Bio.Ext).RightFrom('.') is string sbj ) data = data.LeftFrom(true,CrLf)+$",Subject={sbj}"+data.RightFromFirst(CrLf,with:true) ;
			else if( file.EndsBy(Gpx.Extension.File) && data.Contains(GpxFitAnchor) && System.IO.Path.ChangeExtension(file,".fit") is string fit && System.IO.File.Exists(fit) ) data = data.Replace(GpxFitAnchor,$"{GpxFitAnchor}{fit}.") ;
			return data ;
		}
		public static void Partitionate( this Path path ) { if( path==null ) return ; var parter = path.Origin+Partitioner.Ext ; if( System.IO.File.Exists(parter) ) return ; path.Where(p=>p.Mark.HasFlag(Mark.Stop)&&p.No+1<path.Count).Select(p=>p.No).Stringy(' ').Null(p=>p.No()).Set(p=>parter.WriteAll(p)) ; }
	}
}
