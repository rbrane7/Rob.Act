﻿using System;
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
using Aid.Extension.Public;

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
		public static TraitConversion The = new() ; static readonly Dictionary<string,Filter.Entry.Binding> Binder = new() ;
		static Filter.Entry.Binding Bind( string form ) => Binder.TryGetValue(form??string.Empty,out var v) ? v : Binder[form??string.Empty] = form ;
		static string Evaluate( Aspect.Traitlet trait ) { try { return Bind(trait.Bond).Of(trait.Raw) ; } catch { return $"Failed evaluating Trait {trait.Spec} = {trait.Lex} !" ; } }
		public object Convert( object value , Type targetType , object parameter , CultureInfo culture ) => value is Aspect.Traits t ? t[(int)parameter].Get(Evaluate) : null ;
		public object ConvertBack( object value , Type targetType , object parameter , CultureInfo culture ) => null ;
	}
	interface Quantilable : Aid.Countable<double[]> { Axe Ax {get;} Axe Axon {get;} new Task<int> Count() ; string Spec {get;} double[] this[ double x , int by = 0 ] {get;} }
	struct AxeQuantiles : Quantilable
	{
		public Axe Ax {get;internal set;} public Axe Axon {get;internal set;} internal IEnumerable<double[]> Content ; public int Count => Content?.Count()??0 ;
		public IEnumerator<double[]> GetEnumerator() => Content?.GetEnumerator()??Enumerable.Empty<double[]>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
		Task<int> Quantilable.Count() => Task.FromResult(Count) ;
		public string Spec => $"Q({Ax.Spec}){Axon?.Spec}" ;
		public double[] this[ double x , int by = 0 ] { get { double[] pre = null ; foreach( var q in Content ) if( (pre??=q)[by]<=x && q[by]>=x || pre[by]>=x && q[by]<=x ) return pre ; else pre = q ; return null ; } }
		public class Para : Aid.Collections.ObservableList<double[]> , Quantilable
		{
			public bool Ready ; int ready ;
			public Axe Ax {get;} public Axe Axon {get;} readonly IEnumerable<Aspect> Source ;
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
			public string Spec => $"Q({Ax.Spec}){Axon?.Spec}" ;
			public double[] this[ double x , int by = 0 ] { get { double[] pre = null ; foreach( var q in this ) if( (pre??=q)[by]<=x && q[by]>=x || pre[by]>=x && q[by]<=x ) return pre ; else pre = q ; return null ; } }
		}
	}
	static class QuantilableExtension
	{
		public static bool IsQuantile( this string axe ) => axe.StartsBy("Q(") ;
		public static string QuantileAx( this string quantile ) => quantile.RightFromFirst("Q(").LeftFrom(')') ;
	}
	public class Filter : Aid.Collections.ObservableList<Filter.Entry>.Filtered
	{
		public bool Dirty { get => dirty || this.Any(e=>e.Dirty) ; set { dirty = value ; this.Each(e=>e.Dirty=value) ; } } bool dirty ;
		Filter() => CollectionChanged += (s,a)=>dirty=true ;
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
			public static implicit operator Entry( string entry ) => entry.Get(e=>{ var f = e.Separate(Separator,braces:null) ; return f.Length<=1 ? null : new Entry{ rex = f[0]==" " , filter = f.At(1) , traits = f.At(2) , matrix = f.At(3) , associer = f.At(4) , matter = f.At(5) , query = f.At(6) } ; }) ;
			public Func<Objective,bool> ToFilter<Objective>() => Rex ? Filter.Matcherex<Objective>() : Filter.Compile<Func<Objective,bool>>() ;
			public Func<Objective,IEnumerable<Objective>,bool> ToViciner<Objective>( string naming = null ) => Rex ? Filter.Matcherex<Objective>().Get(f=>new Func<Objective,IEnumerable<Objective>,bool>((p,c)=>f(p))) : Filter.Compile<Func<Objective,IEnumerable<Objective>,bool>>(naming??",context") ;
			public Func<IEnumerable<Objective>,IEnumerable<Objective>> ToQuery<Objective>() => Query.Compile<Func<IEnumerable<Objective>,IEnumerable<Objective>>>() ;
			public Func<Objective,bool> ToAssocier<Objective>() => Associer.Compile<Func<Objective,bool>>() ;
			public (Func<Objective,IEnumerable<Objective>,bool> Filter,Func<Enhancer,bool> Associer,Func<IEnumerable<Objective>,IEnumerable<Objective>> Query) ToRefiner<Objective,Enhancer>( string naming = null ) => (ToViciner<Objective>(naming),ToAssocier<Enhancer>(),ToQuery<Objective>()) ;
			public override string ToString() => Filter ;
			public struct Binding
			{
				static readonly string ThisKey = typeof(Aid.Converters.ObjectAccessible).GetProperties().One().Name ;
				public string Path , Name , Format , Align ; public IValueConverter Converter ; readonly Func<object,object> Pather ;
				public string Form => Align.No() ? Format : Format.No() ? $"{{0,{Align}}}" : $"{{0,{Align}:{Format}}}" ;
				public string Reform => Align.No()&&!Format.No() ? $"{{0:{Format}}}" : Form ;
				public static implicit operator Binding( string value ) => new(value) ;
				public Binding( string value )
				{
					if( value?.TrimStart().StartsBy("(")==true )
					{
						var cvt = value.LeftFromScoped(true,'/',',',':') ; value = value.RightFromFirst(cvt) ; Path = cvt.Contains(LambdaContext.Act.Accessor) ? ThisKey : null ; Pather = null ; cvt = cvt.RightFromFirst('(').LeftFromLast(')') ;
						if( Path==null ) Converter = new Aid.Converters.LambdaConverter{Forward=cvt.LeftFrom(LambdaContext.Act.Lambda,all:true),Back=cvt.RightFromFirst(LambdaContext.Act.Lambda)} ;
						else Converter = new Aid.Converters.LambdaAccessor{Forward=cvt.LeftFrom(LambdaContext.Act.Accessor),Back=cvt.RightFrom(LambdaContext.Act.Accessor)} ;
					}
					else { Path = value.LeftFrom(true,':',',','/') ; Converter = null ; Pather = $".{Path}".Compile<Func<object,object>>() ; }
					Name = value.LeftFrom(true,':',',').RightFromFirst('/',true) ; Format = value.RightFromFirst(':') ; Align = value.LeftFrom(':').RightFrom(',') ;
					if( Format.RightFrom(LambdaContext.Act.Accessor) is string unf ) { Format = Format.LeftFromLast(LambdaContext.Act.Accessor) ; if( (Converter??=new Aid.Converters.LambdaConverter()) is Aid.Converters.LambdaConverter cv ) cv.Backward = unf ; }
					if( Format.LeftFromScoped(LambdaContext.Act.Lambda) is string pre ) { Format = Format[(pre.Length+LambdaContext.Act.Lambda.Length)..] ; if( (Converter??=new Aid.Converters.LambdaConverter()) is Aid.Converters.LambdaConverter cv ) cv.Forwarding = pre ; }
				}
				public string Of( object value ) => View(Value(value)) ;
				object Value( object value ) => Converter is IValueConverter c ? c.Convert(value,null,null,null) : value ;
				public string View( object value ) => Reform.Form( value ) ;
				public object On( object value ) => Converter is IValueConverter c ? c.Convert(Pather.Of(value,value),null,null,null) : Pather.Of(value,value) ;
			}
		}
		const string Separator = " \x1 Filter \x2\n" ;
		public static explicit operator string( Filter filter ) => filter.Get(f=>string.Join(Separator,f.Entries.Where(e=>!e.Empty).Select(e=>(string)e))) ;
		public static implicit operator Filter( string filter ) => filter.Get(f=>new Filter{Sensible=true}.Set(t=>f.Separate(Separator,braces:null).Each(e=>t.Add(e)))) ;
	}
	public struct Associable { public Pathable path ; public Aspect aspect ; public  static implicit operator Associable( (Pathable path,Aspect aspect) arg ) => new(){ path = arg.path , aspect = arg.aspect } ; }
}
