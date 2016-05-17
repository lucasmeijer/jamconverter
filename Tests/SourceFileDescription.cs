using System.Collections.Generic;

namespace jamconverter.Tests
{
	internal class SourceFileDescription
	{
		public string FileName;
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