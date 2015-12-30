using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;



using StackExchange.Redis;

namespace Regulus.Database.Redis
{
    /// <summary>
    /// redis expansion
    /// </summary>
    public class Client
    {
        /// <summary>
        /// 
        /// </summary>
        public interface ISerializeProvider
        {
            bool TryDeserialize(string value, Type type , out object result);

            string Serialize(object value, Type type);
        }

        private ISerializeProvider _Provider;
        

        private IDatabase _Database;

        private readonly string _ArrayLengthName = "Length";

        private readonly string _ValueName = "value:";

        

        public Client(IDatabase database, ISerializeProvider provider)
        {
            _Provider = provider;            
            _Database = database;            
        }


        /// <summary>
        /// To increase a data database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        public void Add<T>(T entry)
        {

            var entryType = typeof(T);

            _AddRoot(entry, entryType);
        }

        private string _Add(object entry, Type entryType)
        {
            var id = Guid.NewGuid().ToString();
            _Database.HashSet(id, _GetPropertys(entryType, entry));
            return id;
        }
        private string _AddRoot(object entry, Type entryType)
        {
            var id = Guid.NewGuid().ToString();
            var fullName = entryType.FullName;
            _Database.SetAdd(_GetTypeKey(fullName), id);
            _Database.HashSet(id, _GetPropertys(entryType, entry));
            return id;
        }

        private HashEntry[] _GetPropertys(Type type, object entry)
        {
            List<HashEntry> hashEntries = new List<HashEntry>();
            var propertys = type.GetProperties();
            foreach (var property in propertys)
            {
                if (property.CanRead)
                {
                    var value = _GetHashEntry(entry, property);
                    if (value.HasValue)
                        hashEntries.Add(value.Value);
                }
            }

            var fields = type.GetFields();
            foreach(var field in fields)
            {
                if(field.IsPublic && field.IsStatic == false)
                {
                    var value = _GetHashEntryField(entry, field);
                    if (value.HasValue)
                        hashEntries.Add(value.Value);
                }
                
            }
            return hashEntries.ToArray();
        }

        private HashEntry? _GetHashEntryField(object entry, FieldInfo field)
        {
            var val = field.GetValue(entry);
            if(val != null)
            {
                var name = field.Name;
                
                var value = _Serialization(field.FieldType, val);

                return new HashEntry(name, value);
            }
            return null;
        }

        private HashEntry? _GetHashEntry(object entry, PropertyInfo property)
        {
            var setMethod = property.GetGetMethod();
            var result = setMethod.Invoke(entry, null);
            if (result == null)
                return null;
            var name = property.Name;

            var returnType = setMethod.ReturnType;
            var value = _Serialization(returnType, result);

            return new HashEntry(name, value);
        }

        private string _Serialization(Type type, object result)
        {
            string value;
            if (_IsValueType(type))
            {
                value = _Serialize(type, result);
            }
            else if (type.IsArray)
            {
                
                value = _AddArray(type, result);
            }
            else
            {
                value = _Add(result, type);
            }
            return value;
        }

        private string _AttachPrefix(string value)
        {
            //return value;
            return _ValueName + value;
        }

        private string _Serialize(Type type, object result)
        {
            
            return _AttachPrefix(_Provider.Serialize(result, type));
        }

        private object _GetArray(Type type, RedisValue redis_value)
        {

            var entrys = _Database.HashGetAll(redis_value.ToString());
            int length = (int)entrys[0].Value;

            var instance = Activator.CreateInstance(type, new object[] { length });
            Array array = (Array)instance;
            if (array.Rank > 1)
                throw new Exception("array rank not more than 1.");

            foreach (var entry in entrys.Skip(1))
            {
                var index = (int)entry.Name;
                if (index < length)
                    array.SetValue(_GetValue(type.GetElementType(), entry.Value), index);
            }

            return instance;
        }
        private string _AddArray(Type type, object result)
        {
            var parent = Guid.NewGuid().ToString();
            Array vals = (Array)result;
            if (vals.Rank > 1)
                throw new Exception("array rank not more than 1.");

            var valIds = _SerializationSet(type, vals);
            _Database.HashSet(parent, valIds.ToArray());
            return parent;
        }

        private IEnumerable<HashEntry> _SerializationSet(Type type, Array vals)
        {
            var length = vals.GetLength(0);

            yield return new HashEntry(_ArrayLengthName, length);
            for (int i = 0; i < length; i++)
            {
                var elementType = type.GetElementType();
                var val = vals.GetValue(i);
                var id = _Serialization(elementType, val);
                yield return new HashEntry(i, id);
            }
        }

        private RedisKey _GetTypeKey(string full_name)
        {
            return full_name;
        }

        public int Update<TClass>(
            Expression<Func<TClass, bool>> filter_expression,
            TClass value)
        {
            int count = 0;
            var type = typeof(TClass);
            var key = _GetTypeKey(type.FullName);
            var members = _Database.SetMembers(key);
            foreach (var member in members)
            {
                var id = member.ToString();
                if (_Check(id, filter_expression.Body))
                {
                    _Database.SetRemove(key, id);
                    _Delete(id, type);
                    _AddRoot(value, type);
                    count++;
                }
            }
            return count;
        }

        public int UpdateField<TClass, TValue>(
            Expression<Func<TClass, bool>> filter_expression,
            Expression<Func<TClass, TValue>> get_expression,
            TValue value)
        {
            int count = 0;
            var type = typeof(TClass);
            var members = _Database.SetMembers(_GetTypeKey(type.FullName));
            foreach (var member in members)
            {
                var id = member.ToString();
                if (_Check(id, filter_expression.Body))
                {
                    _SetField<TValue>(id, get_expression.Body, value);
                    count++;
                }
            }
            return count;
        }
        public IEnumerable<TValue> GetField<TClass, TValue>(Expression<Func<TClass, bool>> filter_expression, Expression<Func<TClass, TValue>> get_expression)
        {
            var type = typeof(TClass);
            var members = _Database.SetMembers(_GetTypeKey(type.FullName));
            foreach (var member in members)
            {
                var id = member.ToString();
                if (_Check(id, filter_expression.Body))
                {
                    yield return (TValue)_GetValue(typeof(TValue), _GetField(id, get_expression.Body));
                }
            }
        }

        public int Delete<T>(Expression<Func<T, bool>> expression)
        {
            int count = 0;
            var type = typeof(T);
            var key = _GetTypeKey(type.FullName);
            var members = _Database.SetMembers(key);
            foreach (var member in members)
            {
                var id = member.ToString();
                if (_Check(id, expression.Body))
                {
                    _Database.SetRemove(key, id);
                    _Delete(id, type);
                    count++;
                }
            }

            return count;
        }

        private void _Delete(string key, Type type)
        {
            if (_IsValueType(type))
                return;

            _DeleteOnlyKey(key);
            /*
            var entrys = _Database.HashGetAll(key);
            if (type.IsArray && !_IsValueType(type.GetElementType()))
            {
                _DeleteArray(type.GetElementType(), entrys);
            }
            else
            {
                _DeleteProperty(type, entrys);
            }

            _Database.KeyDelete(key);*/
        }

        private void _DeleteOnlyKey(string key)
        {
            var entrys = _Database.HashGetAll(key);
            foreach (var entry in entrys)
            {
                Guid result;
                if (Guid.TryParse(entry.Value , out result))
                {
                    _DeleteOnlyKey(entry.Value);
                }
            }
            _Database.KeyDelete(key);
        }

        private void _DeleteArray(Type type, HashEntry[] entrys)
        {
            foreach (var entry in entrys)
            {
                _Delete(entry.Value, type);
            }
        }

        

        private static bool _IsValueType(Type type)
        {            
            return type.IsValueType || type == typeof(string) ;
        }

        public int Exist<T>(Expression<Func<T, bool>> expression)
        {
            var type = typeof(T);
            var members = _Database.SetMembers(_GetTypeKey(type.FullName));
            int count = 0;
            foreach(var member in members)
            {
                var id = member.ToString();
                if(_Check(id, expression.Body))
                {
                    count++;
                }
            }
            return count;
        }
        public IEnumerable<T> Find<T>(Expression<Func<T, bool>> expression)
        {
            var type = typeof(T);
            var members = _Database.SetMembers(_GetTypeKey(type.FullName));

            foreach (var member in members)
            {
                var id = member.ToString();
                if (_Check(id, expression.Body))
                {
                    yield return (T)_GetValue(type, id);
                }
            }
        }

        private object _GetProperty(string id, Type type)
        {
            var instance = Activator.CreateInstance(type);
            var propertys = type.GetProperties();
            foreach (var property in propertys)
            {
                if (property.CanWrite )
                {
                    var dbValue = _Database.HashGet(id, property.Name);
                    if (dbValue.HasValue)
                    {
                        var propertyType = property.PropertyType;

                        var value = _GetValue(propertyType, dbValue);

                        var method = property.GetSetMethod();
                        method.Invoke(
                            instance,
                            new[]
                            {
                                value
                            });

                    }


                }
            }

            var fields = type.GetFields();
            foreach(var field in fields)
            {
                if(field.IsPublic && field.IsStatic == false)
                {
                    var dbValue = _Database.HashGet(id, field.Name);
                    if (dbValue.HasValue)
                    {
                        var fieldType = field.FieldType;

                        var value = _GetValue(fieldType, dbValue);
                        field.SetValue(instance, value);

                    }
                }
                
            }
            return instance;
        }

        private object _GetValue(Type type, RedisValue db_value)
        {
            if(db_value.IsNull)
                return null;
            object value = null;
            if (Client._IsValueType(type))
            {
                value = _Deserialize(type, db_value);
            }
            else if (type.IsArray)
            {
                value = _GetArray(type, db_value);
            }
            else
            {
                value = _GetProperty(db_value, type);
            }
            return value;
        }

        private string _DettachPrefix(string db_value)
        {
            //return db_value;
            return db_value.Remove(0, _ValueName.Length);
        }

        private object _Deserialize(Type type, RedisValue db_value)
        {
            
            object result;
            var value = _DettachPrefix(db_value);
            if(_Provider.TryDeserialize(value , type , out result))
            {
                return result;
            }            
            return Activator.CreateInstance(type);
        }

        private bool _Check(string id, Expression expression)
        {
            if(expression.NodeType == ExpressionType.Constant
                || expression.NodeType == ExpressionType.MemberAccess)
            {
                var lambda = Expression.Lambda(expression);
                return (bool)lambda.Compile().DynamicInvoke();
            }            
            
            if (Client._CheckBinaryNodeType(expression) == false)
                throw new Exception(string.Format("Unhandled expression type: '{0}'", expression.NodeType));

            var exp = (BinaryExpression)expression;

            var valueLeft = _GetField(id, exp.Left);
            var valueRight = _GetValue(exp.Right);
            return valueLeft == valueRight;
        }

        private string _GetField(string id, Expression exp)
        {
            var fieldTypes = _GetFieldTypes(exp);

            id = _GetFieldByFieldTypes(id, fieldTypes.Skip(1));

            return id;
        }

        private void _SetField<TValue>(string id, Expression exp, TValue value)
        {
            var fieldTypes = _GetFieldTypes(exp);

            var key = _GetFieldByFieldTypes(id, fieldTypes.Skip(1).Take(fieldTypes.Length - 2));

            if (exp.NodeType == ExpressionType.ArrayIndex)
            {
                var dbLength = _Database.HashLength(id);
                BinaryExpression be = (BinaryExpression)exp;
                var right = (ConstantExpression)be.Right;
                var index = right.Value.ToString();

                _UpdateField(value, key, index);
            }
            else if (exp.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression me = (MemberExpression)exp;
                var name = me.Member.Name;
                _UpdateField(value, key, name);
            }
            else if (exp.NodeType == ExpressionType.Parameter)
            {
                var type = typeof(TValue);
                var keyName = _GetTypeKey(type.FullName);

                _Database.SetRemove(keyName, id);
                _Delete(id, type);
                _AddRoot(value, type);
            }
        }

        private void _UpdateField<TValue>(TValue value, string key, string name)
        {
            Type type = typeof(TValue);
            var prevValue = _Database.HashGet(key, name);
            _Delete(prevValue, type);
            if(value != null)
            {
                _Database.HashSet(
                    key,
                    new[]
                    {
                        new HashEntry(name, _Serialization(type, value))
                    });
            }
            else
            {
                _Database.HashDelete(key, name);
            }
            
        }

        private string _GetFieldByFieldTypes(string id, IEnumerable<Expression> field_types)
        {
            foreach (var fieldType in field_types)
            {
                if (fieldType.NodeType == ExpressionType.Parameter)
                {
                    ParameterExpression pe = (ParameterExpression)fieldType;
                }
                else if (fieldType.NodeType == ExpressionType.MemberAccess)
                {
                    MemberExpression me = (MemberExpression)fieldType;
                    var name = me.Member.Name;
                    var value = _Database.HashGet(id, name).ToString();

                    id = value;
                }
                else if (fieldType.NodeType == ExpressionType.ArrayIndex)
                {
                    BinaryExpression be = (BinaryExpression)fieldType;
                    var ce = (ConstantExpression)be.Right;
                    var index = ce.Value.ToString();
                    var value = _Database.HashGet(id, index);

                    id = value;
                }
                else
                {
                    throw new Exception("不知道的node type " + fieldType.NodeType);
                }
            }
            return id;
        }

        private string _GetValue(Expression right)
        {

            var lambda = Expression.Lambda(right);
            var val = lambda.Compile().DynamicInvoke();
            
            return _Serialize( lambda.ReturnType , val);
        }

        private Expression[] _GetFieldTypes(Expression expression)
        {
            bool found = false;
            var infos = new Stack<Expression>();
            var current = expression;
            while (found == false)
            {

                if (current.NodeType == ExpressionType.Parameter)
                {
                    ParameterExpression pe = (ParameterExpression)current;

                    infos.Push(current);
                    found = true;
                }
                else if (current.NodeType == ExpressionType.MemberAccess)
                {
                    MemberExpression me = (MemberExpression)current;
                    infos.Push(current);
                    current = me.Expression;
                }
                else if (current.NodeType == ExpressionType.ArrayIndex)
                {
                    BinaryExpression be = (BinaryExpression)current;
                    infos.Push(current);
                    current = be.Left;
                }
                else
                {
                    throw new Exception("不知道的node type");
                }

            }

            return infos.ToArray();
        }        

        private static bool _CheckBinaryNodeType(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    {
                        return true;
                    }
                default:
                    return false;
            }
        }
    }
}