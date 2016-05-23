using System.Collections.Generic;
using NiceIO;

namespace jamconverter.Tests
{
	internal class SourceFileDescription
	{
		public NPath File;
		public string Contents;
	}

    class ProgramDescripton : List<SourceFileDescription>
    {
        public ProgramDescripton()
        {
        }

        public ProgramDescripton(IEnumerable<SourceFileDescription> sfds)
        {
            AddRange(sfds);
        }
    }
}