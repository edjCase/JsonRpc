using System;

namespace EdjCase.JsonRpc.Router.Tests.Controllers
{
    public class MethodMatcherDuplicatesController
    {
        public Guid GuidTypeMethod(Guid guid)
        {
            return guid;
        }
    }
}