using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	using Quant = Double ;
	public static class Erg
	{
		public class Csv
		{
			public static bool Interpolate = false ; public static Func<Quant,bool> Powerage ;
			public static readonly string[] Axes = {"Number","Time (seconds)","Distance (meters)","Pace (seconds)","Watts","Cal/Hr","Stroke Rate","Heart Rate","Laps","Refine","Locus","Subject","Drag Factor","Date","Spec"} ;
			readonly IList<(TimeSpan Time,double Distance,double Beat,uint Bit,double Energy,double Drag,double Effort,Mark Mark)> Data = new List<(TimeSpan Time,double Distance,double Beat,uint Bit,double Energy,double Drag,double Effort,Mark Mark)>() ;
			readonly DateTime Date = DateTime.Now ; readonly string Spec , Subject , Locus , Refine ;
			public static bool Sign( string data ) => Axes.Take(8).All(a=>data.Consists(a)) ;
			/// <summary>
			/// Skierg data processing and clening . 
			/// </summary>
			/// <remarks>
			/// We can't valorize time according to pace/power , because we geto diskrepancy between results wioth Concept2 . 
			/// We could valorize pace/power data according to pair (time,dist) to make them consistent with differential forms but it gives even more erratic data . 
			/// </remarks>
			public Csv( string data )
			{
				bool First() => Data.Count<=1 ;
				(TimeSpan Time,Quant Distance,Quant Beat,uint Bit,Quant Energy,Quant Drag,Quant Effort,Mark Mark) accu = default ;
				(uint bit,double time,double dist,uint beat,uint power,uint drag,double pace,uint effort,Mark mark) last = default ;
				uint idrag = 0 ; Quant atime = 0 , adist = 0 ; IEnumerable<(Quant time,Quant dist)> laps = null ;
				foreach( var line in data.SeparateTrim('\n').Select(l=>l.Trim()) )
				{
					if( Sign(line) )
					{
						Data.Add(accu) ; var lapo = laps?.LastOrDefault() ?? default ;
						laps = line.RightFrom(Axes[^7]+'=').LeftFrom('"').SeparateTrim(';',false)?.Select(e=>(e.LeftFrom(',').Parse<Quant>(0)+lapo.time,e.RightFrom(',').Parse<Quant>(0)+lapo.dist)).ToArray() ;
						if( line.RightFrom(Axes[^6]+'=').LeftFrom('"') is string refine && !Refine.Includes(refine) ) if( Refine.No() ) Refine = refine ; else Refine += $"+{refine}" ;
						if( line.RightFrom(Axes[^5]+'=').LeftFrom('"') is string locus && !Locus.Includes(locus) ) if( Locus.No() ) Locus = locus ; else Locus += $" {locus}" ;
						if( line.RightFrom(Axes[^4]+'=').LeftFrom('"') is string subject && !Subject.Includes(subject) ) if( Subject.No() ) Subject = subject ; else Subject += $" {subject}" ;
						if( line.RightFrom(Axes[^3]+'=').LeftFrom('"').Parse<uint>() is uint drg ) idrag = drg ;
						if( line.RightFrom(Axes[^2]+'=').LeftFrom('"').Parse<DateTime>() is DateTime date && First() ) Date = date ; // Date is used only of the leader one
						if( line.RightFrom(Axes[^1]+'=').LeftFrom('"') is string spec && !Spec.Includes(spec) ) if( Spec.No() ) Spec = spec ; else Spec += $" {spec}" ;
						continue ;
					}
					var vals = line.Separate(',').Select(v=>v.Trim('"')).ToArray() ; if( vals.At(7)==null ) continue ;
					(uint bit,double time,double dist,uint beat,uint power,uint drag,double pace,uint effort,Mark mark) =
						(vals[0].Parse(0U),vals[1].Parse(0D),vals[2].Parse(0D),vals[7].Parse(0U),vals[4].Parse(0U).nil()??last.power,vals.At(8).Parse(0U).nil()??last.drag,vals[3].Parse(0D).nil()??last.pace,vals[5].Parse(0U).nil()??last.effort,default) ;
					(Quant time,Quant dist)? lap = null ; var velo = 500/(pace.nil()??Quant.PositiveInfinity) ;
					if( time<(accu.Time-TimeSpan.FromTicks(1)).TotalSeconds-atime ) { lap = laps?.FirstOrDefault(l=>l.time>=last.time) ; atime = lap?.time ?? last.time ; adist = lap?.dist ?? last.dist+(lap?.time-last.time??0)*velo ; }
					time += atime ; dist += adist ; var lim = laps?.FirstOrDefault(l=>last.time<l.time&&l.time<=time)??default ;
					if( lim.time!=default ) { /*dist -= (time-lim.time)*velo ;*/ dist = lim.dist ; time = lim.time ; mark = Mark.Lap ; } // Limits adjustion
					bit = Math.Max(bit,last.bit+1) ; var db = bit-last.bit ; var ib = Interpolate ? 1 : db ; var dt = TimeSpan.FromSeconds((time-last.time)*ib/db) ; accu.Bit = bit ;
					if( dist<last.dist ) dist = last.dist+dt.TotalSeconds*velo ;
					var ds = (dist-last.dist)*ib/db ;
					for( var i=ib ; i<=db ; i+=ib )/*interpolation*/
					{
						if( Interpolate ) { accu.Time += dt ; accu.Distance += ds ; } else { accu.Time = TimeSpan.FromSeconds(time) ; accu.Distance = dist ; }
						accu.Beat += beat*dt.TotalSeconds/60 ; var Dt = velo==0||(Powerage?.Invoke(power)??false)?dt.TotalSeconds:ds/velo ;
						accu.Energy += power*Dt ; accu.Drag += (idrag=drag.nil()??idrag)*ds/100 ; accu.Effort += effort*.41858*Dt ; accu.Mark = mark ;
						Data.Add(accu) ;
					}
					last = (bit,time,dist,beat,power,drag,pace,effort,mark) ; accu.Mark = default ;
				}
			}
			public static implicit operator Path( Csv work ) =>
				new Path(work.Date,work.Data.Select(p=>new Point(work.Date+p.Time){ Time = p.Time , Dist = p.Distance , Energy = p.Energy , Fuel = p.Effort , Beat = p.Beat , Bit = p.Bit , Drag = p.Drag.nil() , Mark = p.Mark }))
				{ Initing = true , Action = work.Spec , Tags = $"{Basis.Device.Skierg.Code} {work.Subject} {work.Locus} {work.Refine}   {work.Data[^1].get(d=>d.Drag/d.Distance):0.00}" }
				.Set(p=>{ var l = work.Data[^1] ; var f = work.Data[0] ; p.Dist = l.Distance-f.Distance ; p.Time = l.Time-f.Time ; p.Energy = l.Energy-f.Energy ; p.Fuel = l.Effort-f.Effort ; p.Beat = l.Beat-f.Beat ; p.Bit = l.Bit-f.Bit ; p.Drag = l.Drag-f.Drag ; p.Initing = false ; }) ;
			public static explicit operator Csv( Path data ) => throw new NotImplementedException() ;
			public static implicit operator string( Csv data ) => throw new NotImplementedException() ;
		}
		static (TimeSpan Time,Quant Distance,Quant Beat,uint Bit,Quant Energy,Quant Drag,Quant Effort) ToTime( this (TimeSpan Time,Quant Distance,Quant Beat,uint Bit,Quant Energy,Quant Drag,Quant Effort) accu , Quant time , (uint bit,double time,double dist,uint beat,uint power,uint drag,double pace,uint effort) last ) =>
		(TimeSpan.FromSeconds(time),accu.Distance+500/(last.pace.nil()??Quant.PositiveInfinity)*(time-last.time),accu.Beat+last.beat*(time-last.time)/60,accu.Bit+1,accu.Energy+last.power*(time-last.time),accu.Drag+last.drag*(time-last.time),accu.Effort+last.effort*(time-last.time)) ;
		public static Quant Power( this Quant velo ) => 2.8*velo.Cube() ;
	}
}
