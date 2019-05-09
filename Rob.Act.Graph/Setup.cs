using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rob.Act.Analyze
{
	public class Settings
	{
		public string WorkoutsPaths , AspectsPaths , AspectsPath ;
		public Predicate<Path> WorkoutsFilter ;
		public Predicate<Aspect> AspectsFilter ;
	}
}
