using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	public static partial class Csv
	{
		public const string Ext = ".csv" ;
		public class Bio
		{
			public static bool Interpolate = false ;
			public static readonly string[] Axes = {"Date","Heart rate(count/min)","Heart rate variability (SDNN)(ms)","Oxygen saturation(%)","VO2 Max(mL/min·kg)","Spec","Subject"} ;
			public static readonly string Sign = $"{Axes[0]},{Axes[1]},{Axes[2]}" , Act = Basis.Device.Bio.Code , Ext = $".{Act}{Csv.Ext}" ;
			readonly IList<(DateTime Date,double Beat,double Var,double Sat,double Max)> Data = new List<(DateTime Date,double Beat,double Var,double Sat,double Max)>() ;
			TimeSpan Time => Data[^1].Date-Data[0].Date ; DateTime Date => Data.at(0)?.Date??DateTime.Today ; readonly string Spec = Act , Subject ;
			/// <summary>
			/// iOs export and cleaning . 
			/// </summary>
			public Bio( string data )
			{
				(DateTime Date,double Beat,double Var,double Sat,double Max) accu = default , last = default ;
				foreach( var line in data.SeparateTrim('\n').Select(l=>l.Trim()) )
				{
					if( line.StartsBy(Sign) )
					{
						if( line.RightFrom(Axes[^2]+'=').LeftFrom(',',true) is string spec ) Spec = spec ;
						if( line.RightFrom(Axes[^1]+'=') is string subj ) Subject = subj ;
						continue ;
					}
					var vals = line.Separate(',').Select(v=>v.Trim('"')).ToArray() ; if( vals.At(4)==null ) continue ;
					(DateTime Date,double Beat,double Var,double Sat,double Max) = (vals[0].LeftFrom(" - ",all:true).Parse(last.Date,"yyyy-MM-dd HH:mm:ss"),vals[1].Parse(last.Beat),vals[2].Parse(last.Var),vals[3].Parse(last.Sat),vals[4].Parse(last.Max)) ;
					if( accu.Date==default ) { accu.Date = Date.Date ; Data.Add(accu) ; last = (accu.Date,Beat,Var,Sat,Max) ; }
					var dt = (Date-last.Date).TotalSeconds ; var dB = Beat*dt/60 ;
					accu.Date = Date ; accu.Beat += dB ; accu.Var += Var*dB ; accu.Sat += Sat*dt ; accu.Max += Max*dt/60 ;
					Data.Add(accu) ; last = (Date,Beat,Var,Sat,Max) ;
				}
			}
			public static implicit operator Path( Bio work ) =>
				new Path(work.Date,work.Data.Select(p=>new Point(p.Date){ Beat = p.Beat , Flow = p.Sat , Grade = p.Var , Energy = p.Max })){ Action = work.Spec }
				.Set(p=>{ p.Tags = $"{Basis.Device.Bio.Code} {p.O2Rate:0.0} {work.Subject}" ; var l = work.Data[^1] ; var f = work.Data[0] ; p.Time = work.Time ; p.Beat = l.Beat-f.Beat ; p.Energy = l.Max-f.Max ; p.Grade = l.Var-f.Var ; p.Flow = l.Sat-f.Sat ; }) ;
			public static explicit operator Bio( Path data ) => throw new NotImplementedException() ;
			public static implicit operator string( Bio data ) => throw new NotImplementedException() ;
		}
	}
}
