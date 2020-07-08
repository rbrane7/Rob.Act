using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace Rob.Act.Analyze
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		DateTime LastException ;
		public TimeSpan ExceptionTimeout = new TimeSpan(0,0,1) ;
		public App() => DispatcherUnhandledException += (s,e)=>{ if( DateTime.Now-LastException>ExceptionTimeout ) Trace.TraceError($"Unhandled {e.Exception}") ; e.Handled = true ; if( e.Exception is ArgumentOutOfRangeException ) LastException = DateTime.Now ; } ;
	}
}
