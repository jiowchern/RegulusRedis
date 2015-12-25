# Foreword  
RegulusRedis is a redis client for C#.  
It is based on StackExchange.Redis  
The main technology is that when the object is stored in database to be split into individual Hash.  
Library Link : https://www.nuget.org/packages/RegulusRedis/  

# Using Client  
### Build client object
```C#
// StackExchange.Redis initial.
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var client = new Regulus.Database.Redis.Client(redis.GetDatabase(), new JsonSeriallzer());
```
### Add a data to database
```C#
var testObject = new TestObject();//The class is your custom.
var id = 1;
testObject.Id = id;
testObject.Value = 1345;

client.Add(testObject);
```
### Find data
```c#
var testObjects = client.Find<TestObject>(t=> t.Id == 1);
```

### Updata a data
```C#
var testObject2 = new TestObject();//The class is your custom.
testObject2.Id = 1;
testObject2.Value = 999;
var updatedCount = client.Update(t=> t.Id == id, testObject2);
```

### Update by field
```C#
var updatedCount = client.UpdateField<TestObject , int>(t=> t.Id == 1, t=> t.Value , 12345);
```

### Delete a data
```c#
var deletedCount = client.Delete<TestObject>(t => t.Id == 1);
```

# Using Mapper
```c#
// create
var mapper = new Regulus.Database.Redis.Mapper<TestObject>(client, obj => obj.Id == 1);            
// update field
mapper.Update<int>(obj => obj.Value, 99);
// get field
var result = mapper.Get<int>(obj => obj.Value);
```
