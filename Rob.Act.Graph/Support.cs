using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Win32;
using Aid.Extension;
using System.Collections.Specialized;
using System.Globalization;
using Aid.IO;

namespace Rob.Act.Analyze
{
	public class QuantileSubversion : IMultiValueConverter
	{
		public object Convert( object[] values , Type targetType , object parameter , CultureInfo culture )
		{
			if( values.At(0) is Axe ax && values.At(1) is IEnumerable<Aspect> srcs ); else return null ;
#if true	// Parallel
			return new AxeQuantiles.Para(ax,values.At(2) as Axe,srcs.ToArray()) ;
#else
			//var srcf = (values.At(3) as IEnumerable)?.OfType<Aspect>() ; var srcs = src.Except(srcf).Where(a=>a[ax.Spec]!=null) ;
			var dis = (ax.Distribution??srcs.SelectMany(s=>s[ax.Spec].Distribution??Enumerable.Empty<double>()).Distinct().OrderBy(v=>v)).ToArray() ;
			var axon = values.At(2) as Axe ; var res = srcs.Select(a=>a[ax.Spec].Quantile[dis,a[axon?.Spec]].ToArray()).ToArray() ;
			try { return new AxeQuantiles{ Ax = ax , Axon = axon , Content = res.Length>0 && res[0].Length>0 ? res[0].Length.Steps().Select(i=>res.Length.Steps().Select(j=>res[j][i]).Prepend(dis[i+(dis.Length>res[0].Length?1:0)]).ToArray()) : Enumerable.Empty<double[]>() } ; }
			catch( System.Exception e ) { Trace.TraceWarning(e.Stringy()) ; return new AxeQuantiles{ Ax = ax , Axon = axon , Content = Enumerable.Empty<double[]>() } ; }
#endif
		}
		public object[] ConvertBack( object value , Type[] targetTypes , object parameter , CultureInfo culture ) => null ;
	}
	public class TraitConversion : IValueConverter
	{
		public static TraitConversion The = new TraitConversion() ; static readonly IDictionary<string,Filter.Entry.Binding> Binder = new Dictionary<string,Filter.Entry.Binding>() ;
		Filter.Entry.Binding Bind( string form ) => Binder.TryGetValue(form??string.Empty,out var v) ? v : Binder[form??string.Empty] = form ;
		public object Convert( object value , Type targetType , object parameter , CultureInfo culture ) => value is Aspect.Traits t ? t[(int)parameter].Get(r=>Bind(r.Bond).Of(r.Value)) : null ;
		public object ConvertBack( object value , Type targetType , object parameter , CultureInfo culture ) => null ;
	}
	interface Quantilable : Aid.Countable<double[]> { Axe Ax {get;} Axe Axon {get;} Task<int> Count() ; }
	struct AxeQuantiles : Quantilable
	{
		public Axe Ax {get;internal set;} public Axe Axon {get;internal set;} internal IEnumerable<double[]> Content ; public int Count => Content?.Count()??0 ;
		public IEnumerator<double[]> GetEnumerator() => Content?.GetEnumerator()??Enumerable.Empty<double[]>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		async Task<int> Quantilable.Count() => Count ;
		public class Para : Aid.Collections.ObservableList<double[]> , Quantilable
		{
			public bool Ready ; int ready ;
			public Axe Ax {get;} public Axe Axon {get;} IEnumerable<Aspect> Source ;
			public Para( Axe ax , Axe axon , params Aspect[] source ) { Ax = ax ; Axon = axon ; Source = source ; }
			void Insure()
			{
				if( System.Threading.Interlocked.CompareExchange(ref ready,1,0)==0 ) Task.Factory.StartNew(()=>{
					var dis = (Ax.Distribution??Source.SelectMany(s=>s[Ax.Spec].Distribution??Enumerable.Empty<double>()).Distinct().OrderBy(v=>v)).ToArray() ; var res = Source.Select(a=>a[Ax.Spec].Quantile[dis,a[Axon?.Spec]].ToArray()).ToArray() ;
					if( res.Length>0 && res[0].Length>0 ) res[0].Length.Steps().Select(i=>res.Length.Steps().Select(j=>res[j][i]).Prepend(dis[i+(dis.Length>res[0].Length?1:0)]).ToArray()).Each(Add) ;
					Ready = true ;
				}) ;
			}
			public override IEnumerator<double[]> GetEnumerator() { Insure() ; return base.GetEnumerator() ; }
			async Task<int> Quantilable.Count() { if( !Ready ) await Task.Factory.StartNew(()=>System.Threading.SpinWait.SpinUntil(()=>Ready)) ; return base.Count ; }
		}
	}
	public class Filter : Aid.Collections.ObservableList<Filter.Entry>.Filtered
	{
		public class Entry
		{
			const string Separator = " \x1 Filet \x2 " ;
			public bool Rex { get => rex && !Filter.Void() ; set { if( value==rex ) return ; rex = value ; Dirty = true ; } } bool rex ;
			public string Filter { get => filter ; set { if( (value=value.Null(v=>v.Void()))==filter ) return ; filter = value ; Dirty = true ; } } string filter ;
			public string Traits { get => traits ; set { if( (value=value.Null(v=>v.Void()))==traits ) return ; traits = value ; Dirty = true ; } } string traits ;
			public string Matrix { get => matrix ; set { if( (value=value.Null(v=>v.Void()))==matrix ) return ; matrix = value ; Dirty = true ; } } string matrix ;
			public string Associer { get => associer ; set { if( (value=value.Null(v=>v.Void()))==associer ) return ; associer = value ; Dirty = true ; } } string associer ;
			public string Matter { get => matter ; set { if( (value=value.Null(v=>v.Void()))==matter ) return ; matter = value ; Dirty = true ; } } string matter ;
			public string Query { get => query ; set { if( (value=value.Null(v=>v.Void()))==query ) return ; query = value ; Dirty = true ; } } string query ;
			public bool Empty => Filter.No() && Traits.No() && Matrix.No() && Associer.No() && Matter.No() && Query.No() ;
			public bool Dirty ;
			public static explicit operator string( Entry entry ) => entry.Get(e=>string.Join(Separator,e.Rex?" ":string.Empty,e.Filter,e.Traits,e.Matrix,e.Associer,e.Matter,e.Query)) ;
			public static implicit operator Entry( string entry ) => entry.Get(e=>{ var f = e.Separate(Separator) ; return f.Length<=1 ? null : new Entry{ rex = f[0]==" " , filter = f.At(1) , traits = f.At(2) , matrix = f.At(3) , associer = f.At(4) , matter = f.At(5) , query = f.At(6) } ; }) ;
			public Func<Objective,bool> ToFilter<Objective>() => Rex ? Filter.Matcherex<Objective>() : Filter.Compile<Func<Objective,bool>>() ;
			public Func<IEnumerable<Objective>,IEnumerable<Objective>> ToQuery<Objective>() => Query.Compile<Func<IEnumerable<Objective>,IEnumerable<Objective>>>() ;
			public Func<Objective,bool> ToAssocier<Objective>() => Associer.Compile<Func<Objective,bool>>() ;
			public (Func<Objective,bool> Filter,Func<Enhancer,bool> Associer,Func<IEnumerable<Objective>,IEnumerable<Objective>> Query) ToRefiner<Objective,Enhancer>() => (ToFilter<Objective>(),ToAssocier<Enhancer>(),ToQuery<Objective>()) ;
			public override string ToString() => Filter ;
			public struct Binding
			{
				static readonly string ThisKey = typeof(Aid.Converters.ObjectAccessible).GetProperties().One().Name ;
				public string Path , Name , Format , Align ; public IValueConverter Converter ;
				public string Form => Align.No() ? Format : Format.No() ? $"{{0,{Align}}}" : $"{{0,{Align}:{Format}}}" ;
				public string Reform => Align.No()&&!Format.No() ? $"{{0:{Format}}}" : Form ;
				public static implicit operator Binding( string value ) => new Binding(value) ;
				public Binding( string value )
				{
					if( value?.TrimStart().StartsBy("(")==true )
					{
						var cvt = value.LeftFromScoped(true,'/',',',':') ; value = value.RightFromFirst(cvt) ; Path = cvt.Contains(LambdaContext.Act.Accessor) ? ThisKey : null ; cvt = cvt.RightFromFirst('(').LeftFromLast(')') ;
						if( Path==null ) Converter = new Aid.Converters.LambdaConverter{Forward=cvt.LeftFrom(LambdaContext.Act.Lambda,all:true),Back=cvt.RightFromFirst(LambdaContext.Act.Lambda)} ;
						else Converter = new Aid.Converters.LambdaAccessor{Forward=cvt.LeftFrom(LambdaContext.Act.Accessor),Back=cvt.RightFrom(LambdaContext.Act.Accessor)} ;
					}
					else { Path = value.LeftFrom(true,':',',','/') ; Converter = null ; }
					Name = value.LeftFrom(true,':',',').RightFromFirst('/',true) ; Format = value.RightFromFirst(':') ; Align = value.LeftFrom(':').RightFrom(',') ;
					if( Format.RightFrom(LambdaContext.Act.Accessor) is string coformat ); else return ;
					Format = Format.LeftFromLast(LambdaContext.Act.Accessor) ; if( (Converter??=new Aid.Converters.LambdaConverter()) is Aid.Converters.LambdaConverter cv ) cv.Backward = coformat ;
				}
				public string Of( object value ) => Reform.Form( Converter is IValueConverter c ? c.Convert(value,null,null,null) : value ) ;
			}
		}
		const string Separator = " \x1 Filter \x2\n" ;
		public static explicit operator string( Filter filter ) => filter.Get(f=>string.Join(Separator,f.Entries.Where(e=>!e.Empty).Select(e=>(string)e))) ;
		public static implicit operator Filter( string filter ) => filter.Get(f=>new Filter{Sensible=true}.Set(t=>f.Separate(Separator).Each(e=>t.Add(e)))) ;
	}
	public struct Associable { public Pathable path ; public Aspect aspect ; public  static implicit operator Associable( (Pathable path,Aspect aspect) arg ) => new Associable{path=arg.path,aspect=arg.aspect} ; }
}
