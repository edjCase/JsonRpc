using System;

namespace EdjCase.JsonRpc.Router.Tests.Controllers
{
    public class MethodMatcherThreeController
	{
        public Guid GuidTypeMethod(Guid guid)
        {
            return guid;
        }
    }
}