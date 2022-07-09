using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;

namespace EdjCase.JsonRpc.Router.Tests.Controllers
{
    public class MethodMatcherController : RpcController
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

        // https://github.com/edjCase/JsonRpc/issues/99
        public IRpcMethodResult CreateInfoHelperItem(string name, string language, string value, string description, string component, string locationIndex, string fontFamily = "Arial", int fontSize = 12, bool bold = false, bool italic = false, bool strikeout = false, bool underline = false)
		{
            return Ok((name, language, value, description, component, locationIndex, fontFamily, fontSize, bold, italic, strikeout, underline)); ;
		}
	}
}