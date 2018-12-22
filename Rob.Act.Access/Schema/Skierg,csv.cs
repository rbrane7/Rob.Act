using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act
{
	public static partial class Csv
	{
		public class Skierg
		{
			public static readonly string[] Axes = new[]{ "Number" , "Time (seconds)" , "Distance (meters)" , "Pace (seconds)" , "Watts" , "Cal/Hr" , "Stroke Rate" , "Heart Rate" } ;
			public static readonly string Sign = $"\"{Axes[0]}\",\"{Axes[1]}\",\"{Axes[2]}\",\"{Axes[3]}\",\"{Axes[4]}\",\"{Axes[5]}\",\"{Axes[6]}\",\"{Axes[7]}\"" ;
			IList<(TimeSpan Time,double Distance,double Work,double Heart,uint Cycle)> Data = new List<(TimeSpan Time,double Distance,double Work,double Heart,uint Cycle)>() ;
			public Skierg( string data )
			{
				(TimeSpan Time,double Distance,double Work,double Heart,uint Cycle) accu = (TimeSpan.Zero,0,0,0,0) ;
				foreach( var line in data.SeparateTrim('\n').Select(l=>l.Trim()) )
				{
					if( line==Sign ) continue ;
					var vals = line.Separate(',').Select(v=>v.Trim('"')).Skip(1).ToArray() ; if( vals.At(6)==null ) continue ;
					(double time,double dist,uint heart) = (vals[0].Parse<double>(0),vals[1].Parse<double>(0),vals[6].Parse<uint>(0)) ; var dt = TimeSpan.FromSeconds(time-accu.Time.TotalSeconds) ;
					++accu.Cycle ; if( dt==TimeSpan.Zero ) Data[Data.Count-1] = accu ; else { accu.Time += dt ; accu.Distance = dist ; accu.Work = 2.8*Math.Pow(dist/time,3)*time ; accu.Heart += heart*dt.TotalSeconds/60 ; Data.Add(accu) ; }
				}
			}
			public static implicit operator Path( Skierg work ) => new Path(DateTime.Now,work.Data.Select(p=>new Point(DateTime.Now+p.Time){ Time = p.Time , Dist = p.Distance , Ergy = p.Work , Heart = p.Heart , Cycle = p.Cycle }))
				.Set(p=>{ var l = work.Data.LastOrDefault() ; p.Dist = l.Distance ; p.Time = l.Time ; p.Ergy = l.Work ; p.Heart = l.Heart ; p.Cycle = l.Cycle ; }) ;
			public static explicit operator Skierg( Path data ) => throw new NotImplementedException() ;
			public static implicit operator string( Skierg data ) => throw new NotImplementedException() ;
		}
	}
}
