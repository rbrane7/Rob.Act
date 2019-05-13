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
		public string StateFile ;
		public TimeSpan SavePeriod = new TimeSpan(0,0,10) ;
	}
	public class State
	{
		[Flags] enum Kind { No = 0 , Filter = 1 , Aspects = 2 }
		Kind Dirty ; System.Threading.Timer Saver ;
		void Save( object arg )
		{
			if( Dirty.HasFlag(Kind.Filter) ) Main.Setup.StateFile.WriteAll((string)this) ;
			Context.Aspects.Where(a=>a.Dirty).Each(a=>{(a.Origin??Main.Setup.AspectsPath.Get(p=>p.LeftFromLast('*').Path(a.Spec+p.RightFrom('*')))).WriteAll((string)a);a.Dirty=false;}) ;
			Dirty = Kind.No ;
		}
		internal Main Context { get => context ; set { context = value.Set(c=>c.PropertyChanged+=Change) ; Saver?.Dispose() ; Saver = new System.Threading.Timer(Save,null,Main.Setup.SavePeriod,Main.Setup.SavePeriod) ; } } Main context ;
		public string BookLex { set { Context.Book.Lex = bookLex = value ; Dirty |= Kind.Filter ; } } string bookLex ;
		public string BookRex { set { Context.Book.Rex = bookRex = value ; Dirty |= Kind.Filter ; } } string bookRex ;
		void Change( object subject , System.ComponentModel.PropertyChangedEventArgs arg ) {}
		public static explicit operator string( State state ) => string.Join("\x1 State \x2\n",state.bookLex,state.bookRex) ;
		public static implicit operator State( string state ) => state.Separate("\x1 State \x2\n",braces:null).Get(s=>new State{ bookLex = s.At(0) , bookRex = s.At(1) }) ;
	}
}
