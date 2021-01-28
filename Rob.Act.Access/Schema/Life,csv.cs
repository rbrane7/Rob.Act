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
			readonly IList<(DateTime Date,Quant? Beat,Quant? Var,Quant? Sat,Quant? Max)> Data = new List<(DateTime Date,Quant? Beat,Quant? Var,Quant? Sat,Quant? Max)>() ;
			TimeSpan Time => Data[^1].Date-Data[0].Date ; DateTime Date => Data.at(0)?.Date??DateTime.Today ; readonly string Spec = Act , Subject ; readonly bool Multi ;
			/// <summary>
			/// iOs export and cleaning . 
			/// </summary>
			public Bio( string data )
			{
				(DateTime Date,Quant? Beat,Quant? Var,Quant? Sat,Quant? Max) accu = default , last = default ;
				foreach( var line in data.SeparateTrim('\n').Select(l=>l.Trim()) )
				{
					if( line.StartsBy(Sign) )
					{
						if( line.RightFrom(Axes[^2]+'=').LeftFrom(',',true) is string spec ) Spec = spec ;
						if( line.RightFrom(Axes[^1]+'=') is string subj ) Subject = subj ;
						continue ;
					}
					var vals = line.Separate(',').Select(v=>v.Trim('"')).ToArray() ; if( vals.At(4)==null ) continue ;
					(DateTime Date,Quant? Beat,Quant? Var,Quant? Sat,Quant? Max) = (vals[0].LeftFrom(" - ",all:true).Parse(last.Date,"yyyy-MM-dd HH:mm:ss"),vals[1].Parse(last.Beat),vals[2].Parse(last.Var),vals[3].Parse(last.Sat),vals[4].Parse(last.Max)) ;
					if( accu.Date==default ) { accu.Date = Date.Date ; Data.Add(accu) ; last = (accu.Date,Beat,Var,Sat,Max) ; }
					if( accu.Sat==null && Sat!=null ) { accu.Sat = 0 ; Data[^1] = accu ; }
					if( accu.Var==null && Var!=null ) { accu.Var = 0 ; Data[^1] = accu ; }
					if( accu.Max==null && Max!=null ) { accu.Max = 0 ; Data[^1] = accu ; }
					if( accu.Beat==null && Beat!=null ) { accu.Beat = 0 ; Data[^1] = accu ; }
					var dt = (Date-last.Date).TotalSeconds ; var dB = Beat*dt/60 ;
					accu.Date = Date ; accu.Beat = (accu.Beat??0)+dB ; accu.Var = (accu.Var??0)+Var*dB ; accu.Sat = (accu.Sat??0)+Sat*dt ; accu.Max = (accu.Max??0)+Max*dt/60 ;
					Data.Add(accu) ; last = (Date,Beat,Var,Sat,Max) ;
				}
				Multi = (Data.LastOrDefault().Date-Date).Days>0 ;
			}
			public static implicit operator Path( Bio work ) =>
				new Path(work.Date,work.Data.Select(p=>new Point(p.Date){ Beat = p.Beat , Flow = p.Sat , Grade = p.Var , Energy = p.Max , Bit = work.Multi?(p.Date-work.Date).Days:null as Quant? }))
				{ Initing = true , Action = work.Spec , Metax = new Metax{ [Axis.Energy]=("∫O₂↑",":0.0") , [Axis.Flow]=("∫µO₂",":0%") , [Axis.Grade]=("∫♥↕",":0ms") } }
				.Set(p=>{
					p.Tags = $"{Basis.Device.Bio.Code} {p.O2Rate:0.0} {work.Subject}" ; var l = work.Data[^1] ;
					p.Time = work.Time ; p.Beat = l.Beat ; p.Energy = l.Max.Nil() ; p.Grade = l.Var.Nil() ; p.Flow = l.Sat.Nil() ; p.Bit = p[^1].Bit ;
					p.Initing = false ;
				}) ;
			public static explicit operator Bio( Path data ) => throw new NotImplementedException() ;
			public static implicit operator string( Bio data ) => throw new NotImplementedException() ;
		}
	}
}
