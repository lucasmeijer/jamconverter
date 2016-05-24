using System;
using System.Collections.Generic;
using Jam;

namespace runtimelib
{
    public class VariableRestorer : IDisposable
    {
        Dictionary<string, string[]> _backups = new Dictionary<string, string[]>();
        public void RestoreAfterFunction(string variableName)
        {
            _backups[variableName] = Interop.GetVar(variableName);
        }

        public void Dispose()
        {
            foreach (var kvp in _backups)
                Interop.SetVar(kvp.Key, Operator.VAR_SET, kvp.Value);
        }
    }
}
