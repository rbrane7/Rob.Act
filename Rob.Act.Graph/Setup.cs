using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aid.Extension;

namespace Rob.Act.Analyze
{
	public class Settings
	{
		public string Doctee ;
		public string WorkoutsPaths , AspectsPaths , AspectsPath ;
		public Predicate<Path> WorkoutsFilter ;
		public Predicate<Aspect> AspectsFilter ;
		public string StatePath ;
		public TimeSpan SavePeriod = new TimeSpan(0,0,10) ;
		public string[] ActionTraits ;
		public string[] MatrixTraits ;
		public int PrimaryShape = 1 ;
	}
	public class State
	{
		System.Threading.Timer Saver ;
		IDictionary<string,string> Coordinater ; bool Recoordinate ;
		public string this[ string key ] { get => Coordinater.By(key) ; set { if( Coordinater.By(key)==value ) return ; Coordinater[key] = value ; Recoordinate = true ; } }
		public (double? Byte,uint? Count,bool Reverse)? Coordination( string axe ) => this[axe].ParseCoinfo() ;
		void Load()
		{
			Context.ActionFilter = Main.Setup.StatePath.Path("ActionFilter.stt").ReadAllText() ?? string.Empty ;
			Context.SourceFilter = Main.Setup.StatePath.Path("SourceFilter.stt").ReadAllText() ?? string.Empty ;
			Context.AspectFilter = Main.Setup.StatePath.Path("AspectFilter.stt").ReadAllText() ?? string.Empty ;
			Coordinater = Main.Setup.StatePath.Path("Coordinater.stt").ReadAllLines()?.Where(l=>l.Contains(':')).ToDictionary(c=>c.LeftFrom(':'),c=>c.RightFrom(':')) ?? new Dictionary<string,string>() ;
		}
		void Save( object arg )
		{
			Context.Aspects.Where(a=>a.Dirty).Each(a=>{(a.Origin??Main.Setup.AspectsPath.Get(p=>p.LeftFromLast('*').Path(a.Spec+p.RightFrom('*')))).WriteAll((string)a);a.Dirty=false;}) ;
			Context.Book.Where(a=>a.Spectrum.Dirty).Each(a=>{System.IO.Path.ChangeExtension(a.Origin,"spt").Set(p=>{if(((string)a.Spectrum).Null(s=>s.No()).Set(s=>p.WriteAll(s))==null)System.IO.File.Delete(p);});a.Spectrum.Dirty=false;}) ;
			if( Context.ActionFilter.Any(f=>f.Dirty) ) { Context.ActionFilter.Each(f=>f.Dirty=false) ; Main.Setup.StatePath.Set(p=>System.IO.Directory.CreateDirectory(p)).Path("ActionFilter.stt").WriteAll((string)Context.ActionFilter) ; }
			if( Context.SourceFilter.Any(f=>f.Dirty) ) { Context.SourceFilter.Each(f=>f.Dirty=false) ; Main.Setup.StatePath.Set(p=>System.IO.Directory.CreateDirectory(p)).Path("SourceFilter.stt").WriteAll((string)Context.SourceFilter) ; }
			if( Context.AspectFilter.Any(f=>f.Dirty) ) { Context.AspectFilter.Each(f=>f.Dirty=false) ; Main.Setup.StatePath.Set(p=>System.IO.Directory.CreateDirectory(p)).Path("AspectFilter.stt").WriteAll((string)Context.AspectFilter) ; }
			if( Recoordinate ) { Recoordinate = false ; Main.Setup.StatePath.Set(p=>System.IO.Directory.CreateDirectory(p)).Path("Coordinater.stt").WriteAll(Coordinater?.Select(e=>$"{e.Key}:{e.Value}").Stringy(Environment.NewLine)) ; }
		}
		internal Main Context { get => context ; set { context = value ; Saver?.Dispose() ; Saver = new System.Threading.Timer(Save,null,Main.Setup.SavePeriod,Main.Setup.SavePeriod) ; Load() ; } } Main context ;
	}
	internal static class Extension
	{
		public static double? Evaluate( this (double? Byte,uint? Count,bool Reverse)? v ) => v?.Byte ;
		public static double? Signate( this double? value , double? reverse ) => reverse==null ? value : reverse-value ;
		public static (double? Byte,uint? Count,bool Reverse)? ParseCoinfo( this string v ) => v.get(i=>(i.LeftFrom('*',true).TrimEnd('-').Parse<double>(),i.RightFrom('*')?.TrimEnd('-').Parse<uint>(),i.EndsWith("-"))) ;
		public static string StringCoinfo( this (double? Byte,uint? Count,bool Reverse)? v ) => v.Get(i=>$"{i.Byte}*{i.Count}{(i.Reverse?"-":null)}") ;
		public static double? Sqr( this double? val ) => val*val ;
	}
}
