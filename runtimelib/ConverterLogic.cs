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
		public static string ClassNameForJamFile(NPath fileName)
		{
			return "Gen_"+CleanIllegalCharacters(fileName.ToString(SlashMode.Forward));
		}

		public static string CleanIllegalCharacters(string input)
		{
		    return input.Replace(".", "_").Replace("+", "Plus").Replace("*", "Star").Replace("-", "_").Replace("/", "_");
		}
	}
}
