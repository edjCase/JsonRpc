using System;
using System.Collections.Generic;

namespace EdjCase.JsonRpc.Router.Tests.Controllers
{
    public class MethodMatcherController
    {
        public Guid GuidTypeMethod(Guid guid)
        {
            return guid;
        }

        public (int, bool, string, object, int?) SimpleMulitParam(int a, bool b, string c, object d, int? e = null)
        {
            return (a, b, c, d, e);
        }

        public List<string> List(List<string> values)
        {
            return values;
        }
			
        public string SnakeCaseParams(string parameterOne)
        {
            return parameterOne;
        }

        public bool IsLunchTime()
        {
            return true;
        }

        public (string, string?) Optional(string required, string? optional = null)
		{
            return (required, optional);
		}
    }
}