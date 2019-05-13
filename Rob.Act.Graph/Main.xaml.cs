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
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class Main : Window , INotifyPropertyChanged
	{
		public static Settings Setup => setup.Result ; static readonly Aid.Prog.Setup<Settings> setup = (null,e=>Trace.TraceError(e.ToString())) ;
		public State State { get => state ; private set { state = (value??new State()).Set(s=>s.Context=this) ; } } State state ; Aid.Prog.Doct Doct = (Setup.Doctee.Uri(),e=>Trace.TraceError(e.ToString())) ;
		public event PropertyChangedEventHandler PropertyChanged ;
		void PropertyChangedOn<Value>( string properties , Value value ) { PropertyChanged.On(this,properties,value) ; if( properties.Consists("Sources") && GraphTab.IsSelected ) Graph_Draw(this,null) ; }
		public Main() { InitializeComponent() ; DataContext = this ; Doct += (this,"Main") ; Axe.Aspecter = ()=>Book.Select(p=>p.Spectrum).Union(Aspects) ; SourcesGrid.ItemContainerGenerator.ItemsChanged += SourcesGrid_ItemsChanged ; Load() ; }
		void Load()
		{
			Setup.WorkoutsPaths.MatchingFiles().EachGuard(f=>NewAction(f,Setup?.WorkoutsFilter),(f,e)=>Trace.TraceError($"{f} faulted by {e}")) ;
			Setup.AspectsPaths.MatchingFiles().EachGuard(f=>NewAspect(f,Setup?.AspectsFilter),(f,e)=>Trace.TraceError($"{f} faulted by {e}")) ;
			State = Setup.StateFile?.ReadAllText() ;
		}
		protected override void OnClosing( CancelEventArgs e ) { Doct?.Dispose() ; base.OnClosing(e) ; }
		protected override void OnClosed( EventArgs e ) { base.OnClosed(e) ; Process.GetCurrentProcess().Kill() ; }
		void NewAction( string file , Predicate<Path> filter = null ) => file.Reconcile().Internalize().Set(p=> Book += filter is Predicate<Path> f ? f(p)?p:null : p ) ;
		void NewAspect( string file , Predicate<Aspect> filter = null ) => ((Aspect)file.ReadAllText()).Set(a=>a.Origin=file).Set(a=> Aspects += filter is Predicate<Aspect> f ? f(a)?a:null : a ) ;
		public Book Book { get ; private set ; } = new Book("Main") ;
		public Aid.Collections.ObservableList<Aspect> Aspects { get ; private set ; } = new Aid.Collections.ObservableList<Aspect>{Sensible=true} ;
		public Aid.Collections.ObservableList<Axe> Axes { get ; private set ; } = new Aid.Collections.ObservableList<Axe>() ;
		public Aspect Aspect { get => Respect ; protected set { if( value==Aspect ) return ; Aspect.Set(a=>{a.CollectionChanged-=OnAspectChanged;a.PropertyChanged-=OnAspectChanged;}) ; (Respect=value).Set(a=>{a.CollectionChanged+=OnAspectChanged;a.PropertyChanged+=OnAspectChanged;}) ; Sources = null ; } } Aspect Respect ;
		public IEnumerable<Aspect> Sources { get => sources ?? ( sources = Aspect==null ? Enumerable.Empty<Aspect>() : Aspect is Path.Aspect ? BookGrid.SelectedItems.OfType<Path>().Select(s=>s.Spectrum) : BookGrid.SelectedItems.OfType<Path>().Get(p=>AspectMultiToggle.IsChecked==true?p.Select(s=>s.Spectrum).ToArray().Get(s=>Math.Min(AspectMultiCount.Text.Parse(0),s.Length).Get(c=>c>0?c.Steps().Select(i=>new Aspect(Aspect,true){Sources=s.Skip(i).Concat(s.Take(i)).ToArray()}):new Aspect(Aspect,true){Sources=s}.Times())):p.Select(s=>new Aspect(Aspect,false){Source=s.Spectrum})) ) ; set => PropertyChangedOn("Aspect,Sources",sources=value) ; } IEnumerable<Aspect> sources ;
		void OnAspectChanged( object subject , NotifyCollectionChangedEventArgs arg=null ) { var sub = Aspect is Path.Aspect ? SpectrumTabs : AspectTabs ; Revoke : var six = sub.SelectedIndex ; if( sub==AspectTabs || sub==QuantileTabs ) Sources = null ; sub.SelectedIndex = -1 ; sub.SelectedIndex = six ; if( sub==AspectTabs ) { sub = QuantileTabs ; goto Revoke ; } }
		void OnAspectChanged( object subject , PropertyChangedEventArgs arg ) => OnAspectChanged(subject) ;
		void AddActionButton_Click( object sender , RoutedEventArgs e ) { var dlg = new OpenFileDialog{Multiselect=true} ; if( dlg.ShowDialog(this)==true ) dlg.FileNames.Each(f=>NewAction(f)) ; }
		void BookGrid_SelectionChanged( object sender , SelectionChangedEventArgs e ) { Sources = null ; if(!( sender is DataGrid bg )) return ; var sel = bg.SelectedItems.Cast<Path>() ; var i=0 ; foreach( var item in sel ) { if( bg.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row && row.Cell(0) is DataGridCell cell && cell.Foreground is SolidColorBrush b && b.Color!=Colos[i%Colos.Length] ) cell.Foreground = new SolidColorBrush(Colos[i%Colos.Length]) ; ++i ; } foreach( Path item in bg.Items.Cast<Path>().Except(sel) ) if( bg.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row ) if( row.Cell(0).Foreground is SolidColorBrush b && b.Color!=Colors.Black ) row.Cell(0).Foreground = Brushes.Black ; }
		void AspectGrid_AutoGeneratedColumns( object sender , EventArgs e ) { var grid = sender as DataGrid ; var asp = grid.ItemsSource is Aspect.Iterable a ? a.Context : null ; grid.Columns.Clear() ; uint i=0 ; foreach( var ax in asp ) grid.Columns.Add(new DataGridTextColumn{Header=ax.Spec,Binding=new Binding($"[{i++}]")}) ; }
		void QuantileGrid_AutoGeneratedColumns( object sender , EventArgs e ) { var grid = sender as DataGrid ; var src = Sources ; grid.Columns.Clear() ; if( grid.ItemsSource is AxedEnumerable axe ) { QuantileData[axe.Ax.Spec] = axe ; grid.Columns.Add(new DataGridTextColumn{Header=axe.Ax.Distribution?.FirstOrDefault(),Binding=new Binding("[0]")}) ; uint i=1 ; foreach( var asp in src ) grid.Columns.Add(new DataGridTextColumn{Header=asp.Spec,Binding=new Binding($"[{i++}]")}) ; }  }
		void SourcesGrid_AutoGeneratedColumns( object sender , EventArgs e ) { var grid = sender as DataGrid ; if(!( Aspect is Aspect asp )) return ; grid.Columns.Clear() ; grid.Columns.Add(new DataGridTextColumn{Header="Spec",Binding=new Binding("Spec")}) ; int i=0 ; foreach( var tr in asp.Trait ) grid.Columns.Add(new DataGridTextColumn{Header=tr.Spec,Binding=new Binding($"Trait"){ConverterParameter=i++,Converter=TraitConversion.The}}) ; }
		async void SourcesGrid_ItemsChanged( object sender , System.Windows.Controls.Primitives.ItemsChangedEventArgs e ) { var gen = sender as ItemContainerGenerator ; for( int i=0 , c=gen.Items.Count ; i<c ; ++i ) { Retry: if( gen.ContainerFromIndex(i) is DataGridRow row ) row.Cell(0).Foreground = new SolidColorBrush(Colos[i]) ; else { await Task.Delay(100) ; goto Retry ; } } }
		void AspectTabs_Selected( object sender , SelectionChangedEventArgs e ) { var asp = e.AddedItems.Count>0 ? e.AddedItems[0] : null ; switch( (DisplayTable.SelectedItem as TabItem)?.Header ) { case "Aspect" : (asp as Aspect).Set(a=>Aspect=a) ; break ; case "Spectrum" : (asp as Path)?.Spectrum.Set(a=>Aspect=a) ; break ; } }
		void AddAspectAxeButton_Click( object sender , RoutedEventArgs e ) => Aspect.Add(new Axe{Aspect=Aspect as Path.Aspect}.Set(Axes.Add)) ;
		void AddAspectTraitButton_Click( object sender , RoutedEventArgs e ) => Aspect.Trait.Add(new Aspect.Traitlet()) ;
		void AddAspectButton_Click( object sender , RoutedEventArgs e ) => Aspects.Add(new Aspect()) ;
		void AddAxeButton_Click( object sender , RoutedEventArgs e ) => Axes.Add(new Axe()) ;
		void SaveAspectsButton_Click( object sender , RoutedEventArgs e ) => Setup.AspectsPath.Set(p=>Aspects.Each(a=>p.LeftFromLast('*',true).Path(a.Spec+p.RightFrom('*')).WriteAll((string)a))) ;
		void DisplayTable_SelectionChanged( object sender , SelectionChangedEventArgs e ) { var tab = e.AddedItems.Count>0 ? e.AddedItems[0] as TabItem : null ; switch( tab?.Header as string ) { case "Aspect" : Aspect = AspectsGrid.SelectedItem as Aspect ; GraphType = "Aspect" ; break ; case "Spectrum" : Aspect = (((SpectrumTabs.SelectedItem as TabItem)?.Content as DataGrid)?.ItemsSource as Path ?? SpectrumTabs.ItemsSource.OfType<Path>().One())?.Spectrum ; GraphType = "Spectrum" ; break ; case "Quantile" : GraphType = "Quantile" ; break ; } }
		void DataGridCommandBinding_Executed( object sender , ExecutedRoutedEventArgs e ) => ((sender as DataGrid)?.ItemsSource as IList).Remove((sender as DataGrid)?.SelectedItem) ;
		#region Graphing
		void Graph_Draw( object sender , RoutedEventArgs e ) { GraphPanel.Children.Clear() ; switch( GraphType ) { case "Aspect" : case "Spectrum" : GraphDrawAspect() ; return ; case "Quantile" : GraphDrawQuantile() ; return ; } }
		string GraphType ; Dictionary<string,AxedEnumerable> QuantileData = new Dictionary<string,AxedEnumerable>() ;
		(double Width,double Height) MainFrameSize => (MainFrame.ColumnDefinitions[1].ActualWidth-GraphScreenBorder.Width,MainFrame.RowDefinitions[1].ActualHeight-GraphScreenBorder.Height) ;
		void GraphDrawAspect()
		{
			var xaxes = AspectAxisGrid.SelectedItems.OfType<Axe>().Select(a=>a.Spec).ToArray() ; if(!( AspectAxisGrid.SelectedItem is Axe xaxe )) return ;
			(var width,var height) = GraphFrame = MainFrameSize ;
			{
				var brush = new SolidColorBrush(new Color{A=127,R=200,G=200,B=200}) ; var dash = new DoubleCollection{4} ;
				for( var m=0 ; m<=width ; m+=50 ) GraphPanel.Children.Add( new Line{ X1 = m , Y1 = 0 , X2 = m , Y2 = height , Stroke = brush , StrokeDashArray = dash } ) ;
				for( var m=height ; m>=0 ; m-=50 ) GraphPanel.Children.Add( new Line{ X1 = 0 , Y1 = m , X2 = width , Y2 = m , Stroke = brush , StrokeDashArray = dash } ) ;
				brush = new SolidColorBrush(new Color{A=63,R=191,G=191,B=191}) ; dash = new DoubleCollection{8} ;
				for( var m=0 ; m<=width ; m+=10 ) GraphPanel.Children.Add( new Line{ X1 = m , Y1 = 0 , X2 = m , Y2 = height , Stroke = brush , StrokeDashArray = dash } ) ;
				for( var m=height ; m>=0 ; m-=10 ) GraphPanel.Children.Add( new Line{ X1 = 0 , Y1 = m , X2 = width , Y2 = m , Stroke = brush , StrokeDashArray = dash } ) ;
			}
			var rng = new Dictionary<string,(double Min,double Max)>() ; Sources.SelectMany(s=>s).Each(a=>{if(a.Any(q=>q!=null))if(!rng.ContainsKey(a.Spec))rng[a.Spec]=(a.Min().Value,a.Max().Value);else{rng[a.Spec]=(Math.Min(rng[a.Spec].Min,a.Min().Value),Math.Max(rng[a.Spec].Max,a.Max().Value));}}) ; if( !rng.ContainsKey(xaxe.Spec) ) return ;
			{
				var x = rng[xaxe.Spec] ;
				for( var m=0 ; m<=width ; m+=100 ) GraphPanel.Children.Add( new Label{ Content=Format(ScreenX(x.Min+m*(x.Max-x.Min)/width,x)) , Foreground=Brushes.Gray }.Set(l=>{Canvas.SetLeft(l,m-5);Canvas.SetTop(l,height-20);}) ) ;
				for( var m=50 ; m<=width ; m+=100 ) GraphPanel.Children.Add( new Label{ Content=Format(ScreenX(x.Min+m*(x.Max-x.Min)/width,x)) , Foreground=Brushes.Gray }.Set(l=>{Canvas.SetLeft(l,m-5);Canvas.SetTop(l,-10);}) ) ;
				if( x.Min<0 && x.Max>0 ) { var xZero = ScreenX(-x.Min/(x.Max-x.Min)*width) ; GraphPanel.Children.Add( new Line{ X1 = xZero , Y1 = 0 , X2 = xZero , Y2 = height , Stroke = Brushes.Gray } ) ; }
				var n=0 ; foreach( var ax in rng.Keys.Except(xaxes) )
				{
					var y = rng[ax] ; for( var m=height-50 ; m>=0 ; m-=50 ) GraphPanel.Children.Add( new Label{ Content=Format(ScreenY(y.Min+(height-m)*(y.Max-y.Min)/height,y)) , Foreground=Brushes.Gray }.Set(l=>{Canvas.SetTop(l,m-20);Canvas.SetLeft(l,n*50-4);}) ) ; ++n ;
					if( y.Min<0 && y.Max>0 ) { var yZero = ScreenY(y.Max/(y.Max-y.Min)*height) ; GraphPanel.Children.Add( new Line{ X1 = 0 , Y1 = yZero , X2 = width , Y2 = yZero , Stroke = Brushes.Gray } ) ; }
				}
			}
			var k = 0 ; foreach( var asp in Sources )
			{
				var xax = asp[xaxe.Spec] ; ++k ; var j = 0 ; foreach( var ax in asp ) if( !xaxes.Contains(ax.Spec) ) try { GraphPanel.Children.Add( new Polyline{
					Stroke = new SolidColorBrush(Colos[(k-1)%Colos.Length]) , StrokeDashArray = j++==0?null:new DoubleCollection{j-1} ,
					Points = new PointCollection(ax.Count.Steps().Where(i=>xax[i]!=null&&ax[i]!=null).Select(i=>ScreenPoint((xax[i].Value-rng[xax.Spec].Min)/(rng[xax.Spec].Max-rng[xax.Spec].Min)*width,height-(ax[i].Value-rng[ax.Spec].Min)/(rng[ax.Spec].Max-rng[ax.Spec].Min)*height)))
				} ) ; } catch( System.Exception ex ) { Trace.TraceWarning(ex.Stringy()) ; }
			}
			Hypercube = rng.Where(a=>xaxe.Spec==a.Key||!xaxes.Contains(a.Key)).ToArray() ;
		}
		void GraphDrawQuantile()
		{
			var xaxes = AspectAxisGrid.SelectedItems.OfType<Axe>().Select(a=>a.Spec).ToArray() ;
			(var width,var height) = GraphFrame = MainFrameSize ;
			{
				var brush = new SolidColorBrush(new Color{A=127,R=127,G=127,B=127}) ; var dash = new DoubleCollection{4} ;
				for( var m=0 ; m<=width ; m+=50 ) GraphPanel.Children.Add( new Line{ X1 = m , Y1 = 0 , X2 = m , Y2 = height , Stroke = brush , StrokeDashArray = dash } ) ;
				for( var m=height ; m>=0 ; m-=50 ) GraphPanel.Children.Add( new Line{ X1 = 0 , Y1 = m , X2 = width , Y2 = m , Stroke = brush , StrokeDashArray = dash } ) ;
				brush = new SolidColorBrush(new Color{A=63,R=191,G=191,B=191}) ; dash = new DoubleCollection{8} ;
				for( var m=0 ; m<=width ; m+=10 ) GraphPanel.Children.Add( new Line{ X1 = m , Y1 = 0 , X2 = m , Y2 = height , Stroke = brush , StrokeDashArray = dash } ) ;
				for( var m=height ; m>=0 ; m-=10 ) GraphPanel.Children.Add( new Line{ X1 = 0 , Y1 = m , X2 = width , Y2 = m , Stroke = brush , StrokeDashArray = dash } ) ;
			}
			var rng = new List<KeyValuePair<string,(double Min,double Max)>>() ;
			var k = 0 ; if( Aspect!=null ) foreach( var axe in Aspect ) if( axe.Spec!=null && QuantileData.At(axe.Spec) is AxedEnumerable ax && ax.Count()>0 && !xaxes.Contains(axe.Spec) )
			{
				var val = ax.SelectMany(v=>v.Skip(1)) ; ((double Min,double Max) x,(double Min,double Max) y) = ((ax.Min(a=>a[0]),ax.Max(a=>a[0])),(val.Min(),val.Max())) ;
				rng.Add(new KeyValuePair<string,(double Min,double Max)>(ax.Ax.Spec,x)) ; rng.Add(new KeyValuePair<string,(double Min,double Max)>($"Q({ax.Ax.Spec})",y)) ;
				{
					for( var m=0 ; m<=width ; m+=100 ) GraphPanel.Children.Add( new Label{ Content=Format(ScreenX(x.Min+m*(x.Max-x.Min)/width,x)) , Foreground=Brushes.Gray }.Set(l=>{Canvas.SetLeft(l,m-5);Canvas.SetTop(l,height-20-10*k);}) ) ;
					for( var m=50 ; m<=width ; m+=100 ) GraphPanel.Children.Add( new Label{ Content=Format(ScreenX(x.Min+m*(x.Max-x.Min)/width,x)) , Foreground=Brushes.Gray }.Set(l=>{Canvas.SetLeft(l,m-5);Canvas.SetTop(l,-10+10*k);}) ) ;
					for( var m=height-50 ; m>=0 ; m-=50 ) GraphPanel.Children.Add( new Label{ Content=Format(ScreenY(y.Min+(height-m)*(y.Max-y.Min)/height,y)) , Foreground=Brushes.Gray }.Set(l=>{Canvas.SetTop(l,m-20);Canvas.SetLeft(l,-4);}) ) ;
					if( x.Min<0 && x.Max>0 ) { var xZero = ScreenX(-x.Min/(x.Max-x.Min)*width) ; GraphPanel.Children.Add( new Line{ X1 = xZero , Y1 = 0 , X2 = xZero , Y2 = height , Stroke = Brushes.Gray } ) ; }
					if( y.Min<0 && y.Max>0 ) { var yZero = ScreenY(y.Max/(y.Max-y.Min)*height) ; GraphPanel.Children.Add( new Line{ X1 = 0 , Y1 = yZero , X2 = width , Y2 = yZero , Stroke = Brushes.Gray } ) ; }
				}
				for( int j = 1 , cnt = ax.FirstOrDefault()?.Length??0 ; j<cnt ; ++j ) GraphPanel.Children.Add( new Polyline{
					Stroke = new SolidColorBrush(Colos[(j-1)%Colos.Length]) , StrokeDashArray = k==0?null:new DoubleCollection{k} ,
					Points = new PointCollection(ax.Select(a=>ScreenPoint((a[0]-x.Min)/(x.Max-x.Min)*width,height-(a[j]-y.Min)/(y.Max-y.Min)*height)))
				} ) ; ++k ;
			}
			Hypercube = rng ;
		}
		#endregion
		static int DecDigits( double value ) => value==0 ? 0 : (int)Math.Max(0,3-Math.Log10(Math.Abs(value))) ;
		static string Format( double value ) => value.ToString("#."+new string('#',DecDigits(value))) ;
		IEnumerable<KeyValuePair<string,(double Min,double Max)>> Hypercube ; (double Width,double Height) GraphFrame ; (Line X,Line Y) MouseCross , ScreenCross ;
		(double Width,double Height) GraphScreenBorder => (DisplayTable.Margin.Left+DisplayTable.Margin.Right+4,30) ;
		static readonly Color[] Colos = new[]{ new Color{A=255,R=255,G=0,B=0} , new Color{A=255,R=0,G=255,B=0} , new Color{A=255,R=0,G=0,B=255} , new Color{A=255,R=191,G=191,B=0} , new Color{A=255,R=0,G=191,B=191} , new Color{A=255,R=191,G=0,B=191} , new Color{A=255,R=223,G=0,B=159} , new Color{A=255,R=159,G=223,B=0} , new Color{A=255,R=0,G=159,B=223} , new Color{A=255,R=159,G=191,B=223} , new Color{A=255,R=223,G=159,B=0} , new Color{A=255,R=0,G=223,B=159} } ;
		void AspectAxisGrid_SelectionChanged( object sender, SelectionChangedEventArgs e ) { if( GraphTab.IsSelected ) Graph_Draw(this,null) ; }
		void AspectMultiToggle_Changed( object sender, RoutedEventArgs e ) { if( sender==AspectMultiToggle || AspectMultiToggle.IsChecked==true ) Sources = null ; }
		#region Coordinates
		void GraphPanel_MouseMove( object sender, MouseEventArgs e ) => MousePoint = Mouse.GetPosition(GraphPanel).nil() ;
		public string Coordinates { get => coordinates ; set => PropertyChanged.On(this,"Coordinates",coordinates=value); } string coordinates ;
		public System.Windows.Point? MousePoint
		{ get => mousePoint ; set
			{
				MouseCross.X.Set(GraphPanel.Children.Remove) ; MouseCross.Y.Set(GraphPanel.Children.Remove) ; var asp = Hypercube is Array ;
				PropertyChanged.On( this, "MousePoint", Coordinates = (mousePoint=value).Get(m=>Hypercube?.Select(a=>$"{a.Key}={( ( asp ? AspectAxisGrid.SelectedItem is Axe x && a.Key==x.Spec : !a.Key.StartsBy("Q(") ) ? ScreenX(m.X/GraphFrame.Width*(a.Value.Max-a.Value.Min)+a.Value.Min,a.Value) : ScreenY((GraphFrame.Height-m.Y)/GraphFrame.Height*(a.Value.Max-a.Value.Min)+a.Value.Min,a.Value) )}").Stringy('\n')) ) ;
				if( value==null || value?.X<0 || value?.Y<0 || value?.X>GraphFrame.Width || value?.Y>GraphFrame.Height ) return ;
				GraphPanel.Children.Add( MouseCross.X = new Line{ Stroke=Brushes.Gray , X1=0 , X2=GraphFrame.Width , Y1=value.Value.Y , Y2=value.Value.Y } ) ;
				GraphPanel.Children.Add( MouseCross.Y = new Line{ Stroke=Brushes.Gray , Y1=0 , Y2=GraphFrame.Height , X1=value.Value.X , X2=value.Value.X } ) ;
			}
		}
		#endregion
		#region Focusing
		System.Windows.Point? mousePoint , screenPoint ; System.Windows.Rect? ScreenRect ;
		System.Windows.Point? ScreenOrigin
		{ get => screenPoint ; set
			{
				
				ScreenCross.X.Set(GraphPanel.Children.Remove) ; ScreenCross.Y.Set(GraphPanel.Children.Remove) ;
				var p = ScreenMouse ; screenPoint = value.Get(v=>p) ; if( value==null ) return ;
				GraphPanel.Children.Add( ScreenCross.X = new Line{ Stroke=Brushes.Orange , X1=0 , X2=GraphFrame.Width , Y1=value.Value.Y , Y2=value.Value.Y } ) ;
				GraphPanel.Children.Add( ScreenCross.Y = new Line{ Stroke=Brushes.Orange , Y1=0 , Y2=GraphFrame.Height , X1=value.Value.X , X2=value.Value.X } ) ;
			}
		}
		System.Windows.Point? ScreenMouse { get { if( ScreenRect==null || MousePoint==null ) return MousePoint ; (var width,var height) = MainFrameSize ; var p = MousePoint.Value ; var r = ScreenRect.Value ; return new System.Windows.Point(p.X*r.Size.Width/width+r.Location.X,p.Y*r.Size.Height/height+r.Location.Y) ; } }
		void GraphPanel_MouseEnter( object sender, MouseEventArgs e ) => Panel.SetZIndex(AddAspectButton,Coordinates!=null?0:2) ;
		void GraphPanel_MouseLeave( object sender, MouseEventArgs e ) => Panel.SetZIndex(AddAspectButton,2) ;
		void GraphPanel_MouseDown( object sender, MouseButtonEventArgs e ) => ScreenOrigin = MousePoint ;
		void DisplayTable_MouseUp( object sender, MouseButtonEventArgs e ) { if( ScreenMouse==ScreenOrigin ) return ; var scr = ScreenOrigin.Get(s=>ScreenMouse.use(p=>new Rect(s,p))) ; if( scr==ScreenRect ) return ; ScreenRect = scr ; Graph_Draw(sender,null) ; }
		void DisplayTable_MouseDoubleClick( object sender, MouseButtonEventArgs e ) { var draw = ScreenRect!=null ; ScreenOrigin = null ; ScreenRect = null ; if( draw ) Graph_Draw(sender,e) ; }
		System.Windows.Point ScreenPoint( double x , double y ) => new System.Windows.Point(ScreenX(x),ScreenY(y)) ;
		double ScreenX( double x ) { if( ScreenRect==null ) return x ; var r = ScreenRect.Value ; return (x-r.Location.X)*GraphFrame.Width/r.Size.Width ; }
		double ScreenY( double y ) { if( ScreenRect==null ) return y ; var r = ScreenRect.Value ; return (y-r.Location.Y)*GraphFrame.Height/r.Size.Height ; }
		double ScreenX( double x , (double Min,double Max) e ) { if( ScreenRect==null ) return x ; var r = ScreenRect.Value ; var q = (e.Max-e.Min)/GraphFrame.Width ; var fx = (x-e.Min)/q ; return e.Min+r.Location.X*q+fx*r.Width/GraphFrame.Width*q ; }
		double ScreenY( double y , (double Min,double Max) e ) { if( ScreenRect==null ) return y ; var r = ScreenRect.Value ; var q = (e.Max-e.Min)/GraphFrame.Height ; var fy = (e.Max-y)/q ; return e.Max-r.Location.Y*q-fy*r.Height/GraphFrame.Height*q ; }
		#endregion
	}
	public class QuantileSubversion : IMultiValueConverter
	{
		public object Convert( object[] values , Type targetType , object parameter , CultureInfo culture ) { if(!( values.At(0) is Axe ax && values.At(1) is IEnumerable<Aspect> src )) return null ; var dis = ax.Distribution.ToArray() ; var res = src.Where(a=>a[ax.Spec]!=null).Select(a=>a[ax.Spec].Quantile[dis].ToArray()).ToArray() ; return new AxedEnumerable{ Ax = ax , Content = res.Length>0 && res[0].Length>0 ? res[0].Length.Steps().Select(i=>res.Length.Steps().Select(j=>res[j][i]).Prepend(dis[i+(dis.Length>res[0].Length?1:0)]).ToArray()) : Enumerable.Empty<double[]>() } ; }
		public object[] ConvertBack( object value , Type[] targetTypes , object parameter , CultureInfo culture ) => null ;
	}
	public class TraitConversion : IValueConverter
	{
		public static TraitConversion The = new TraitConversion() ;
		public object Convert( object value , Type targetType , object parameter , CultureInfo culture ) => value is Aspect.Traits t ? t[(int)parameter]?.Valunit : null ;
		public object ConvertBack( object value , Type targetType , object parameter , CultureInfo culture ) => null ;
	}
	struct AxedEnumerable : IEnumerable<double[]> { public Axe Ax ; internal IEnumerable<double[]> Content ; public IEnumerator<double[]> GetEnumerator() => Content?.GetEnumerator()??Enumerable.Empty<double[]>().GetEnumerator() ; IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ; }
}
