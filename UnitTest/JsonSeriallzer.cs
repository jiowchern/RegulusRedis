using System;


using Newtonsoft.Json;


using Regulus.Database.Redis;

namespace RedisTest
{
    public class JsonSeriallzer : Client.ISerializeProvider
    {
        bool Client.ISerializeProvider.TryDeserialize(string db_value, Type type , out object result)
        {
            try
            {
                result = JsonConvert.DeserializeObject(db_value, type);
                return true;
            }
            catch(Exception e )
            {
                result = null;

            }
            return false;
        }

        string Client.ISerializeProvider.Serialize(object result, Type type)
        {
            return JsonConvert.SerializeObject(result);
        }
    }
}