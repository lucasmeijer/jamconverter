using System.Collections.Generic;

using ListStorage = System.Collections.Generic.Dictionary<string, LocalJamList>;

namespace runtimelib
{
	public class MockJamStorage
	{
		public static MockJamStorage Instance { get; } = new MockJamStorage();

		readonly Dictionary<string,ListStorage> _perTargetStorage = new Dictionary<string, ListStorage>();
		readonly ListStorage _globalStorage = new ListStorage();

		public void InteropAssign(string variableName , string onTarget, string @operator , JamListBase[] values )
		{
			var storage = onTarget == null ? _globalStorage : GetOrCreateStorageForTarget(onTarget);

			var variable = GetVariableFromStorage(variableName, storage);

			ApplyOperator(@operator, values, variable);
		}

		private ListStorage GetOrCreateStorageForTarget(string onTarget)
		{
			ListStorage result;
			if (_perTargetStorage.TryGetValue(onTarget, out result))
				return result;

			result = new ListStorage();
			_perTargetStorage[onTarget] = result;
			return result;
		}

		private LocalJamList GetVariableFromStorage(string variableName, ListStorage storage)
		{
			LocalJamList variable;
			if (!storage.TryGetValue(variableName, out variable))
			{
				variable = new LocalJamList();
				storage.Add(variableName, variable);
			}
			return variable;
		}

		private static void ApplyOperator(string @operator, JamListBase[] values, LocalJamList variable)
		{
			switch (@operator)
			{
				case "assign":
					variable.Assign(values);
					break;

				case "assignifempty":
					variable.AssignIfEmpty(values);
					break;

				case "substract":
					variable.Subtract(values);
					break;

				case "append":
					variable.Append(values);
					break;
			}
		}

		public string[] GetValue(string variableName)
		{
			LocalJamList result;
			if (!_globalStorage.TryGetValue(variableName, out result))
				return new string[0];
			return result.Elements;
		}
	}
}