using Elastic.Apm.Api;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observability.Core
{
    public static class TransactionExtensions
    {
        public static ITransaction WithLabel(this ITransaction transaction, string key, string value)
        {
            transaction.SetLabel(key, value);
            return transaction;
        }
    }
}
