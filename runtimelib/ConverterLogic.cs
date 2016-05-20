using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;

namespace runtimelib
{
	public class ConverterLogic
	{
		public static string ClassNameForJamFile(string fileName)
		{
			return "Gen_"+new NPath(fileName).FileNameWithoutExtension;
		}

		public static string CleanIllegalCharacters(string input)
		{
		    return input.Replace(".", "_").Replace("+", "Plus").Replace("*", "Star");
		}
	}
}
