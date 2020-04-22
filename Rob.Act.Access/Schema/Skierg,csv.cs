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
			public static bool Interpolate = false ;
			public static readonly string[] Axes = new[]{"Number","Time (seconds)","Distance (meters)","Pace (seconds)","Watts","Cal/Hr","Stroke Rate","Heart Rate","Refine","Locus","Subject","Drag Factor","Date","Spec"} ;
			public static readonly string Sign = $"\"{Axes[0]}\",\"{Axes[1]}\",\"{Axes[2]}\",\"{Axes[3]}\",\"{Axes[4]}\",\"{Axes[5]}\",\"{Axes[6]}\",\"{Axes[7]}\"" ;
			IList<(TimeSpan Time,double Distance,double Beat,uint Bit,double Energy,double Drag,double Effort)> Data = new List<(TimeSpan Time,double Distance,double Beat,uint Bit,double Energy,double Drag,double Effort)>() ;
			DateTime Date = DateTime.Now ; string Spec , Subject , Locus , Refine ;
			public Skierg( string data )
			{
				(TimeSpan Time,Quant Distance,Quant Beat,uint Bit,Quant Energy,Quant Drag,Quant Effort) accu = (TimeSpan.Zero,0,0,0,0,0,0) ;
				uint idrag = 0 ; Quant atime = 0 , adist = 0 ;
				foreach( var line in data.SeparateTrim('\n').Select(l=>l.Trim()) )
				{
					if( line.StartsBy(Sign) )
					{
						Data.Add(accu) ;
						Refine = line.RightFrom(Axes[Axes.Length-6]+'=').LeftFrom('"') ;
						Locus = line.RightFrom(Axes[Axes.Length-5]+'=').LeftFrom('"') ;
						Subject = line.RightFrom(Axes[Axes.Length-4]+'=').LeftFrom('"') ;
						if( line.RightFrom(Axes[Axes.Length-3]+'=').LeftFrom('"').Parse<uint>() is uint drg ) idrag = drg ;
						if( line.RightFrom(Axes[Axes.Length-2]+'=').LeftFrom('"').Parse<DateTime>() is DateTime date ) Date = date ;
						if( line.RightFrom(Axes[Axes.Length-1]+'=').LeftFrom('"') is string spec ) Spec = spec ;
						continue ;
					}
					var vals = line.Separate(',').Select(v=>v.Trim('"')).ToArray() ; if( vals.At(7)==null ) continue ;
					(uint bit,double time,double dist,uint beat,uint power,uint drag,double pace,uint effort) = (vals[0].Parse<uint>(0),vals[1].Parse<double>(0),vals[2].Parse<double>(0),vals[7].Parse<uint>(0),vals[4].Parse<uint>(0),vals.At(8).Parse<uint>(0),vals[3].Parse<double>(0),vals[5].Parse<uint>(0)) ;
					bit = Math.Max(bit,accu.Bit+1) ; var db = bit-accu.Bit ; var ib = Interpolate ? 1 : db ; if( time<(accu.Time-TimeSpan.FromTicks(1)).TotalSeconds-atime ) { atime = accu.Time.TotalSeconds ; adist = accu.Distance ; }
					time += atime ; var dt = TimeSpan.FromSeconds((time-accu.Time.TotalSeconds)/db*ib) ; dist += adist ; accu.Bit = bit ; var vq = 500/pace ; if( dist<accu.Distance ) dist = accu.Distance+dt.TotalSeconds*vq ; var ds = dist-accu.Distance ; //if( dt==TimeSpan.Zero ) { accu.Drag += (drag.nil()??idrag)*db ; Data[Data.Count-1] = accu ; continue ; } // skipping empty bits
					for( var i=ib ; i<=db ; i+=ib )/*interpolation*/{ accu.Time += dt ; accu.Distance = dist ; accu.Beat += beat*dt.TotalSeconds/60 ; accu.Energy += power*ds/vq/*dt.TotalSeconds*/ ; accu.Drag += (idrag=drag.nil()??idrag)*ds/100 ; accu.Effort += effort*.41858*ds/vq ; Data.Add(accu) ; }
				}
			}
			public static implicit operator Path( Skierg work ) =>
				new Path(work.Date,work.Data.Select(p=>new Point(work.Date+p.Time){ Time = p.Time , Dist = p.Distance , Energy = p.Energy , Flow = p.Effort , Beat = p.Beat , Bit = p.Bit , Drag = p.Drag.nil() }))
				{ Action = work.Spec , Tags = $"{Basis.Device.Skierg.Code} {work.Data[work.Data.Count-1].get(d=>d.Drag/d.Distance):0.00} {work.Subject} {work.Locus} {work.Refine}" }
				.Set(p=>{ var l = work.Data[work.Data.Count-1] ; var f = work.Data[0] ; p.Dist = l.Distance-f.Distance ; p.Time = l.Time-f.Time ; p.Energy = l.Energy-f.Energy ; p.Flow = l.Effort-f.Effort ; p.Beat = l.Beat-f.Beat ; p.Bit = l.Bit-f.Bit ; p.Drag = l.Drag-f.Drag ; }) ;
			public static explicit operator Skierg( Path data ) => throw new NotImplementedException() ;
			public static implicit operator string( Skierg data ) => throw new NotImplementedException() ;
		}
	}
}
