using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Regulus.Database.Redis
{
    public class Mapper<TClass>
    {
        private readonly Client _Cliet;

        private readonly Expression<Func<TClass, bool>> _FindExpression;

        public Mapper(Client cliet, Expression<Func<TClass, bool>> find_expression)
        {
            _Cliet = cliet;
            _FindExpression = find_expression;
        }

        

        public int Update<T>(Expression<Func<TClass, T>> field_expression, T value)
        {
            return _Cliet.UpdateField(_FindExpression, field_expression, value);
        }

        public IEnumerable<T> Get<T>(Expression<Func<TClass, T>> find_expression )
        {
            return _Cliet.GetField(_FindExpression, find_expression);
        }
    }
}
