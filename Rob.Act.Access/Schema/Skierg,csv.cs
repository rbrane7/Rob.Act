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
		public class Skierg
		{
			public static readonly string[] Axes = new[]{"Number","Time (seconds)","Distance (meters)","Pace (seconds)","Watts","Cal/Hr","Stroke Rate","Heart Rate","Drag Factor","Date","Spec"} ;
			public static readonly string Sign = $"\"{Axes[0]}\",\"{Axes[1]}\",\"{Axes[2]}\",\"{Axes[3]}\",\"{Axes[4]}\",\"{Axes[5]}\",\"{Axes[6]}\",\"{Axes[7]}\"" ;
			IList<(TimeSpan Time,double Distance,double Work,double Heart,uint Cycle,double Effort,double Drag)> Data = new List<(TimeSpan Time,double Distance,double Work,double Heart,uint Cycle,double Effort,double Drag)>() ;
			DateTime Date = DateTime.Now ; string Spec ;
			public Skierg( string data )
			{
				(TimeSpan Time,Quant Distance,Quant Work,Quant Heart,uint Cycle,Quant Effort,Quant Drag) accu = (TimeSpan.Zero,0,0,0,0,0,0) ; uint idrag = 0 ;
				foreach( var line in data.SeparateTrim('\n').Select(l=>l.Trim()) )
				{
					if( line.StartsBy(Sign) )
					{
						Data.Add(accu) ;
						if( line.RightFrom(Axes[Axes.Length-3]+'=').LeftFrom('"').Parse<uint>() is uint drg ) idrag = drg ;
						if( line.RightFrom(Axes[Axes.Length-2]+'=').LeftFrom('"').Parse<DateTime>() is DateTime date ) Date = date ;
						if( line.RightFrom(Axes[Axes.Length-1]+'=').LeftFrom('"') is string spec ) Spec = spec ;
						continue ;
					}
					var vals = line.Separate(',').Select(v=>v.Trim('"')).ToArray() ; if( vals.At(7)==null ) continue ;
					(uint cycle,double time,double dist,uint heart,uint power,uint drag) = (vals[0].Parse<uint>(0),vals[1].Parse<double>(0),vals[2].Parse<double>(0),vals[7].Parse<uint>(0),vals[4].Parse<uint>(0),vals.At(8).Parse<uint>(0)) ; var dt = TimeSpan.FromSeconds(time-accu.Time.TotalSeconds) ;
					var dc = cycle-accu.Cycle ; accu.Cycle = cycle ; if( dt==TimeSpan.Zero ) { accu.Drag += (drag.nil()??idrag)*dc ; Data[Data.Count-1] = accu ; }
					else { accu.Time += dt ; accu.Distance = dist ; accu.Work = 2.8*Math.Pow(dist/time,3)*time ; accu.Heart += heart*dt.TotalSeconds/60 ; accu.Effort += power ; accu.Drag += (drag.nil()??idrag)*dc ; Data.Add(accu) ; }
				}
			}
			public static implicit operator Path( Skierg work ) => new Path(work.Date,work.Data.Select(p=>new Point(work.Date+p.Time){ Time = p.Time , Dist = p.Distance , Ergy = p.Work , Heart = p.Heart , Cycle = p.Cycle , Effort = p.Effort , Drag = p.Drag })){ Action = work.Spec , Tag = $"Skierg {work.Data[work.Data.Count-1].get(d=>d.Drag/d.Cycle)}" }
				.Set(p=>{ var l = work.Data[work.Data.Count-1] ; var f = work.Data[0] ; p.Dist = l.Distance-f.Distance ; p.Time = l.Time-f.Time ; p.Ergy = l.Work-f.Work ; p.Heart = l.Heart-f.Heart ; p.Cycle = l.Cycle-f.Cycle ; p.Effort = l.Effort-f.Effort ; p.Drag = l.Drag-f.Drag ; }) ;
			public static explicit operator Skierg( Path data ) => throw new NotImplementedException() ;
			public static implicit operator string( Skierg data ) => throw new NotImplementedException() ;
		}
	}
}
