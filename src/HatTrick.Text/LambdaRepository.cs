using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
    public class LambdaRepository
    {
        #region internals
        Dictionary<string, Delegate> _lambdas;
        #endregion

        #region constructors
        public LambdaRepository()
        {
            _lambdas = new Dictionary<string, Delegate>();
        }
        #endregion

        #region register
        public void Register(string name, Delegate function)
        {
            if (_lambdas.ContainsKey(name))
            {
                throw new ArgumentException($"A function with the provided name: {name} has already been added");
            }
            _lambdas.Add(name, function);
        }

        public void Register(string name, Func<string> function)
        {
            if (_lambdas.ContainsKey(name))
            {
                throw new ArgumentException($"A function with the provided name: {name} has already been added");
            }
            _lambdas.Add(name, function);
        }
        #endregion

        #region deregister
        public void Deregister(string name)
        {
            if (!_lambdas.ContainsKey(name))
            {
                throw new ArgumentException($"No lambda exists for the provided name: {name}");
            }
            _lambdas.Remove(name);
        }
        #endregion

        #region invoke
        public object Invoke(string name, params object[] parms)
        {
            if (!_lambdas.ContainsKey(name))
            { throw new MergeException($"Encountered function that does not exist in lambda repo: {name}"); }

            System.Reflection.MethodInfo mi = _lambdas[name].Method;

            int paramsCount = mi.GetParameters().Length;

            if (paramsCount != parms.Length)
            {
                string msg = $"Attempted function invocation with invalid number of parameters. Lambda name: {name}  Expected count: {paramsCount} Provided count: {parms.Length}";
                throw new MergeException(msg);
            }

            return _lambdas[name].DynamicInvoke(parms);
        }
        #endregion
    }
}
